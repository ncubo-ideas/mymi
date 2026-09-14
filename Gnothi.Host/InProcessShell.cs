using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// IN-PROCESS SHELL. The module is a LIBRARY, not an executable: it is loaded into its own
// context and called by function. No child process, no stdio, and the module does not need
// to bring a transport.
//
// EXPERIMENTAL. It coexists with StdioShell and is chosen per module from installation.json
// ("shell": { "type": "in-process" }). Measured: identical results to stdio on all three POC
// modules. Also measured: it does NOT contain the failures stdio does -- see
// NotasGnothiHost/10.
//
// IT DOES NOT REFERENCE THE MCP SDK. The attributes are read BY NAME, so a module can carry a
// Gnothi-owned label instead of an MCP one and this file does not change -- that day arrived:
// both are recognized now. A cargo module wears [ModuleSurface]/[Query] from Gnothi.Contract
// (tiny, no SDK) and an MCP-style module wears [McpServerToolType]/[McpServerTool]; the shell
// matches either, so the SAME loader serves both without knowing which it is.
internal sealed class InProcessShell : IShell
{
	private static readonly string[] ClassMarks = ["McpServerToolTypeAttribute", "ModuleSurfaceAttribute"];
	private static readonly string[] ToolMarks = ["McpServerToolAttribute", "QueryAttribute"];

	// Y EL OTRO PUBLICO. Lo que pide la pantalla de la institucion, que no es lo mismo que lo
	// que se publica hacia el asistente: medido sobre examenes, 2 tools declaradas contra 21
	// lecturas del dominio. Ver ViewAttribute en Gnothi.Contract.
	private static readonly string[] ViewMarks = ["ViewAttribute"];

	private readonly string name;
	private readonly string path;
	private readonly string grantedFolder;
	private readonly SemaphoreSlim gate = new(1, 1);

	private AssemblyLoadContext? context;
	private Type? marked;

	// LA SUPERFICIE, UNA SOLA, VIVA MIENTRAS EL ENSAMBLADO LO ESTE -- y esto es una correccion,
	// no una optimizacion. Antes se hacia `Build(t, 0)` EN CADA LLAMADA: la clase de la
	// institucion se reconstruia para contestar cada pregunta.
	//
	// Y ESO LE COSTABA UN CANDADO A CADA INSTITUCION. Si la instancia no sobrevive entre
	// llamadas, lo que si tiene que sobrevivir -- el actor, porque arrancarlo es rehidratar el
	// journal entero -- esta obligado a ser `static`. Un `static` construido perezosamente desde
	// varios hilos obliga a un doble chequeo con candado, y las CINCO superficies terminaron
	// copiando el mismo parche a mano. Que se copiara cinco veces es la prueba de que no era
	// decision de nadie: era un hueco de esta cascara.
	//
	// Con la instancia aca, una superficie vuelve a ser un objeto normal con campos normales, y
	// no escribe una sola linea de concurrencia. Lo que queda repartido queda donde corresponde:
	// esta cascara decide que hay UNA superficie, y el actor se defiende de sus llamadores con su
	// propio ReaderWriterLockSlim, que ya existe (Puppeteer, ActorHandler).
	private object? live;

	// QUEDARSE CARGADO MIENTRAS LA CARA ESTA ABIERTA, y solo la forma hibrida lo pide.
	//
	// Por omision este shell evicta despues de contestar, igual que stdio: un modulo vivo tiene
	// la carpeta del journal abierta, y el asistente pregunta de a ratos. Pero una cara hibrida
	// pregunta VARIAS VECES SEGUIDAS mientras la persona mira la pantalla, y ademas recibe
	// entregas en el medio. Cargar y descargar entre cada pintada es caro y, peor, pone dos
	// actores sobre el mismo journal: el Unload es COOPERATIVO -- no completa si el anterior dejo
	// referencias vivas --, asi que el lector nuevo abre mientras el viejo todavia no solto.
	//
	// Sintoma medido el 2026-09-11 en el telefono: `FileNotFoundException: Temp file not found`
	// justo despues de una entrega, en la consulta siguiente.
	private readonly bool keepLoaded;

	public InProcessShell(string name, Shell shell, string grantedFolder, bool keepLoaded = false)
	{
		this.name = name;
		this.grantedFolder = grantedFolder;
		this.keepLoaded = keepLoaded;
		// The shell's first argument is the dll. Same field stdio uses, different meaning:
		// there it is what gets executed, here it is what gets loaded.
		path = Path.GetFullPath(shell.Arguments.Count > 0 ? shell.Arguments[0] : "");
	}

	public bool IsRunning => context is not null;

	private Type Start()
	{
		if (marked is not null) return marked;
		gate.Wait();
		try
		{
			if (marked is not null) return marked;

			var ctx = new AssemblyLoadContext($"module:{name}", isCollectible: true);

			// AssemblyDependencyResolver reads the module's .deps.json, and IT DOES NOT EXIST
			// ON ANDROID: it throws PlatformNotSupportedException because it depends on
			// hostpolicy, which mobile does not have. MEASURED on a Galaxy A54, SDK 36, with a
			// step-by-step probe:
			//
			//   1) AssemblyLoadContext(isCollectible: true) .... OK
			//   2) AssemblyDependencyResolver .................. FAILS PlatformNotSupported
			//   3) LoadFromAssemblyPath ........................ OK
			//   4) GetTypes .................................... FAILS: 'Choreography' missing
			//
			// Step 4 failed AS A CONSEQUENCE of step 2: with no resolver, nothing found the
			// dependencies. So the resolver becomes OPTIONAL and the fallback is to look next
			// door: every dependency of the module travels in its own folder.
			AssemblyDependencyResolver? resolver = null;
			try { resolver = new AssemblyDependencyResolver(path); }
			catch (PlatformNotSupportedException) { /* mobile: resolved by adjacency */ }

			string moduleFolder = Path.GetDirectoryName(path)!;

			ctx.Resolving += (c, n) =>
			{
				// If the Host ALREADY has this assembly, share it. Filtering by the "System."
				// prefix does NOT work: there are System.* packages the module brings and the
				// Host does not have (System.IO.Hashing, measured).
				try { _ = AssemblyLoadContext.Default.LoadFromAssemblyName(n); return null; }
				catch { /* private to the module */ }

				string? p = resolver?.ResolveAssemblyToPath(n);

				if (p is null)
				{
					string neighbour = Path.Combine(moduleFolder, (n.Name ?? "") + ".dll");
					if (File.Exists(neighbour)) p = neighbour;
				}

				return p is null ? null : c.LoadFromAssemblyPath(p);
			};

			Assembly asm = ctx.LoadFromAssemblyPath(path);

			// GetTypes and not GetExportedTypes: the modules' tool classes are internal.
			Type? found = asm.GetTypes().FirstOrDefault(t =>
				t.GetCustomAttributes(false).Any(a => ClassMarks.Contains(a.GetType().Name)));

			if (found is null)
				throw new InvalidOperationException(
					$"'{Path.GetFileName(path)}' has no class marked with [{string.Join("] or [", ClassMarks)}]");

			context = ctx;
			marked = found;
			return marked;
		}
		finally { gate.Release(); }
	}

	// LA INSTANCIA SE PUBLICA UNA SOLA VEZ, y esa es la parte que hay que no hacer mal: si se
	// construyera perezosamente SIN el candado, la carrera del primer uso -- dos llamadas
	// simultaneas sobre un modulo recien abierto, construyendo dos superficies y con ellas dos
	// actores sobre un journal -- no desapareceria del sistema: se mudaria del modulo a esta
	// clase, que es peor, porque aca ya nadie la esta mirando.
	//
	// Mismo doble chequeo que `Start()` y el MISMO candado, sin anidarlos: `Start()` ya lo solto
	// cuando esto corre.
	//
	// NO SE CONSTRUYE ADENTRO DE `Start()`, a proposito. `ActualTools` tambien llama a `Start()`,
	// y es lo que corre la comprobacion de modulos al instalar y al arrancar: construir ahi haria
	// que LISTAR LOS NOMBRES DE LAS TOOLS rehidratara el journal de cada modulo hibrido. La
	// instancia nace cuando alguien pregunta de verdad.
	private object Instance(Type t)
	{
		if (live is not null) return live;
		gate.Wait();
		try { return live ??= Build(t, 0); }
		finally { gate.Release(); }
	}

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
	{
		try
		{
			Type t = Start();
			return Task.FromResult<IReadOnlyList<string>>([.. t
				.GetMethods(Everything)
				.Select(DeclaredName)
				.Where(n => n is not null)
				.Select(n => n!)]);
		}
		finally { if (!keepLoaded) Evict(); }
	}

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> Ask(tool, arguments, accepts, ToolMarks);

	// LO QUE PIDE LA CARA, y es OTRO CONJUNTO: los metodos marcados [View].
	//
	// Mismo camino, distinto marcador. No se mezcla con Invoke a proposito: si las dos puertas
	// buscaran en el mismo conjunto, una consulta de pantalla podria llegar por la puerta del
	// asistente -- y al reves, que es peor -- sin que nada lo impidiera.
	public Task<object> View(string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts)
		=> Ask(name, arguments, accepts, ViewMarks);

	private Task<object> Ask(
		string asked, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, string[] marks)
	{
		try
		{
			Type t = Start();

			MethodInfo? method = t.GetMethods(Everything)
				.FirstOrDefault(m => string.Equals(NameUnder(m, marks), asked, StringComparison.Ordinal));

			if (method is null) throw new InvalidOperationException($"module '{name}' does not expose '{asked}'");

			object? instance = method.IsStatic ? null : Instance(t);
			object?[] values = [.. method.GetParameters().Select(p => Bind(p, arguments, accepts))];

			object? raw;
			try { raw = method.Invoke(instance, values); }
			catch (TargetInvocationException e) when (e.InnerException is not null)
			{
				// With no protocol in between, the module's REAL exception arrives intact.
				// Over stdio the module's own server translates it into a generic message and
				// the detail is lost.
				throw e.InnerException;
			}

			return Task.FromResult<object>(Project(raw) ?? new Dictionary<string, object?>());
		}
		finally { if (!keepLoaded) Evict(); }
	}

	// LA ENTREGA, POR EL MISMO CAMINO QUE UNA CONSULTA: se busca el metodo marcado, se arma la
	// instancia con la carpeta concedida, se invoca. Lo unico distinto es el marcador y que la
	// firma es fija -- (string) -> string -- asi que no hay nada que ligar.
	//
	// Y NO SE EVICTA AL TERMINAR, a diferencia de Invoke y ActualTools. Ahi descargar el
	// contexto despues de contestar es correcto: la pregunta termino. Una entrega deja el
	// journal escrito y lo mas probable es que enseguida venga una consulta a leerlo; descargar
	// el ensamblado en el medio haria pagar la carga dos veces por una sola interaccion.
	public Task<string> Deliver(string envelopeJson, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(envelopeJson);

		Type t = Start();

		MethodInfo? method = t.GetMethods(Everything).FirstOrDefault(m =>
			m.GetCustomAttributes(false).Any(a => a.GetType().Name == "DeliverAttribute"));

		if (method is null)
			throw new NotSupportedException(
				$"module '{name}' has no method marked with [Deliver]: it is a surface that only reads");

		object? instance = method.IsStatic ? null : Instance(t);

		object? raw;
		try { raw = method.Invoke(instance, [envelopeJson]); }
		catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; }

		return Task.FromResult(raw as string ?? "");
	}

	// HASTA DONDE YA LEYO, por el mismo camino que la entrega: se busca el metodo marcado, se arma
	// la instancia con la carpeta concedida, se invoca. La firma es fija -- () -> long -- asi que
	// tampoco hay nada que ligar.
	//
	// CERO CUANDO NO HAY MARCADOR, y no es un error: un modulo que no declara su cursor recibe el
	// historial entero, que es exactamente lo que pasaba antes de que esto existiera. Lo mismo
	// cuando el journal esta vacio y el propio modulo contesta cero.
	//
	// Y TAMPOCO EVICTA, por la misma razon que Deliver: preguntar el cursor es el primer paso de
	// un refresco, y lo que sigue es la entrega. Descargar el ensamblado en el medio haria pagar
	// la carga dos veces por una sola interaccion.
	public Task<long> Cursor(CancellationToken ct)
	{
		Type t = Start();

		MethodInfo? method = t.GetMethods(Everything).FirstOrDefault(m =>
			m.GetCustomAttributes(false).Any(a => a.GetType().Name == "CursorAttribute"));

		if (method is null) return Task.FromResult(0L);

		object? instance = method.IsStatic ? null : Instance(t);

		object? raw;
		try { raw = method.Invoke(instance, []); }
		catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; }

		return Task.FromResult(raw is long ya && ya > 0 ? ya : 0L);
	}

	private const BindingFlags Everything =
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	// EL NOMBRE BAJO EL QUE VIAJA, para el conjunto de marcadores que se le pase.
	//
	// Que reciba los marcadores en vez de mirarlos todos es lo que mantiene separados los dos
	// publicos: ActualTools y el asistente preguntan por ToolMarks, la cara por ViewMarks, y una
	// consulta de pantalla NO puede llegar por la puerta del asistente ni al reves.
	private static string? NameUnder(MethodInfo m, string[] marks)
	{
		object? mark = m.GetCustomAttributes(false).FirstOrDefault(a => marks.Contains(a.GetType().Name));
		if (mark is null) return null;
		// Every mark carries a 'Name'. McpServerTool may leave it null (then the method name is
		// used); Query and View always have one.
		return mark.GetType().GetProperty("Name")?.GetValue(mark) as string ?? m.Name;
	}

	private static string? DeclaredName(MethodInfo m) => NameUnder(m, ToolMarks);

	// THE CONVENTION, and it has to be said out loud because it is the weakest part of this
	// shell: any string a constructor asks for receives the granted folder. It works on all
	// three POC modules, but it is NOT declared in any label -- the Host is guessing. With
	// stdio the problem did not exist, because the folder went as a command-line argument.
	// This is the exact point where the convention has to become a label.
	private object Build(Type t, int depth)
	{
		if (depth > 4) throw new InvalidOperationException($"constructor chain too deep at {t.Name}");

		ConstructorInfo[] ctors = [.. t
			.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.OrderBy(c => c.GetParameters().Length)];

		if (ctors.Length == 0) throw new InvalidOperationException($"{t.Name} has no accessible constructor");

		ConstructorInfo c = ctors[0];
		object?[] args = [.. c.GetParameters().Select(p =>
			p.ParameterType == typeof(string) ? grantedFolder : Build(p.ParameterType, depth + 1))];

		return c.Invoke(args)!;
	}

	// The enum is resolved FROM THE TYPE. That is what the manifest cannot express.
	private static object? Bind(ParameterInfo p, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts)
	{
		// THE RESERVED PARAMETER. A module surface that wants to choose its output format
		// declares a parameter called 'accepts' and the HOST fills it -- never the assistant.
		//
		// Checked BEFORE reading `arguments`, and that order is the whole point: if the
		// assistant's dictionary were read first, a question carrying accepts="toon" would
		// pick its own wrapper, and the format is not the question's to choose. A module that
		// does not declare this parameter never learns it exists, which is the same
		// indistinguishability the rest of the design relies on.
		if (string.Equals(p.Name, Accepts.ParameterName, StringComparison.Ordinal))
			return p.ParameterType == typeof(string)
				? accepts.ToString()
				: p.ParameterType.IsAssignableFrom(typeof(string[]))
					? accepts.Formats.ToArray()
					: accepts;

		object? raw = null;
		arguments?.TryGetValue(p.Name ?? "", out raw);

		if (raw is null)
			return p.HasDefaultValue ? p.DefaultValue : throw new ArgumentException($"missing parameter '{p.Name}'");

		string text = raw is JsonElement je
			? je.ValueKind == JsonValueKind.String ? je.GetString() ?? "" : je.GetRawText()
			: raw.ToString() ?? "";

		Type t = p.ParameterType;
		return t.IsEnum ? Enum.Parse(t, text, ignoreCase: true) : Convert.ChangeType(text, t);
	}

	// PROJECT BY READING THE LABEL BY NAME, and finding this cost a bug: crossing a load
	// context boundary, the module's [JsonPropertyName] may NOT be the same type the Host's
	// serializer knows. When it does not match, the json names are ignored IN SILENCE and the
	// response comes out with the wrong shape.
	private static object? Project(object? v)
	{
		if (v is null) return null;
		Type t = v.GetType();
		if (v is Enum) return v.ToString();
		if (t.IsPrimitive || v is string || v is decimal || v is DateTime || v is DateTimeOffset) return v;
		if (v is System.Collections.IEnumerable list) return list.Cast<object?>().Select(Project).ToList();

		Dictionary<string, object?> d = [];
		foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			object? mark = p.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == "JsonPropertyNameAttribute");
			string? declared = mark?.GetType().GetProperty("Name")?.GetValue(mark) as string;

			// WITHOUT a label, camelCase -- and this cost a bug that only showed up with the
			// THIRD module. A return type with no [JsonPropertyName] came out PascalCase by
			// reflection and camelCase over stdio, so the SAME tool returned different field
			// names depending on the shell. The SDK applies that policy in the SERIALIZER,
			// which is not an attribute, so reflection cannot see it: it has to be replicated.
			//
			// Facturas and examenes hid it because they label every field. Compras returns
			// domain types with no labels, and there it surfaced.
			string label = declared ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name);
			d[label] = Project(p.GetValue(v));
		}
		return d;
	}

	// Evicted the same way stdio does, for the same reason: a live module holds the journal
	// folder open. BUT Unload is COOPERATIVE -- measured: it does not complete if the actor
	// left live references. With stdio, evicting meant killing a process and it was a
	// guarantee; here it is a request.
	private void Evict()
	{
		gate.Wait();
		try
		{
			// Y LA SUPERFICIE SE CIERRA ANTES, porque esta cascara es su duena: la construyo, la
			// guardo entre llamadas, y soltarla es parte de desalojar. Sin esto, «desalojar» no
			// descargaba nada -- el Unload es cooperativo y el ACTOR de la institucion es justo
			// quien deja las referencias vivas.
			//
			// Por IDisposable y no por un marcador nuevo del contrato: una institucion que tiene
			// algo que cerrar ya sabe como decirlo en C#, y la que no, no implementa nada. El
			// contrato de Gnothi no crece por esto.
			//
			// Y NO TIRA: se esta yendo igual. Un Dispose que falla no puede impedir la descarga.
			if (live is IDisposable cerrable)
			{
				try { cerrable.Dispose(); } catch { /* ya se estaba yendo */ }
			}
			live = null;

			marked = null;
			try { context?.Unload(); } catch { /* already going away */ }
			context = null;
		}
		finally { gate.Release(); }
	}

	// Y SE PUEDE LLAMAR DOS VECES, que ahora pasa de verdad: una actualizacion cierra el modulo
	// ANTES de mover su carpeta, y despues el catalogo relee y suelta lo que salio de la lista --
	// el mismo objeto. Sin esta guarda el segundo `Evict()` haria `gate.Wait()` sobre un semaforo
	// ya dispuesto y saldria un ObjectDisposedException por una llamada que no hace nada.
	private bool disposed;

	public ValueTask DisposeAsync()
	{
		if (disposed) return ValueTask.CompletedTask;
		disposed = true;

		Evict();
		gate.Dispose();
		return ValueTask.CompletedTask;
	}
}
