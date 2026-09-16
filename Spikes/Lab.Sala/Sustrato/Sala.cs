using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Sala.Sustrato;

// LA SALA, COMO SERVICIO. Lo mismo que el laboratorio hace de corrido, pero esperando a que
// Gnothi pida: cargar un modulo, y contestar una consulta.
//
// QUIEN DICE DE QUE MODULO SE HABLA. El nombre llega como parametro de estos metodos, y eso
// parece contradecir la regla del puente -- "no nombra un modulo, a proposito". No la
// contradice, porque QUIEN LLAMA ES GNOTHI, no el modulo: estas funciones se invocan desde el
// lado nativo, dentro del marco de una llamada que el Host abrio. El modulo no puede alcanzarlas
// -- no tiene como llamar a DotNet.invokeMethodAsync desde adentro de su propio dominio, que es
// codigo .NET corriendo bajo este runtime y no JavaScript de la pagina.
//
// Lo que SI hay que sostener del lado nativo, y es la pieza que Jonathan marco como critica:
// una sola llamada en vuelo, y el journal de un solo modulo montado por vez. Esto de aca no
// puede garantizarlo solo; ver SalaShell.
public static class Sala
{
	private static readonly SemaphoreSlim UnaPorVez = new(1, 1);
	private static readonly Dictionary<string, Modulo> Cargados = new(StringComparer.Ordinal);
	private static readonly HttpClient Http = new();

	// La raiz en MEMFS. No existe ninguna carpeta real detras: Gnothi pone los bytes aca antes
	// de preguntar, y lo que el modulo escriba se queda en memoria.
	private const string Raiz = "/journal";

	public static void ConfigurarDireccion(Uri baseAddress) => Http.BaseAddress = baseAddress;

	private sealed record Modulo(Type Superficie, object Instancia, IReadOnlyDictionary<string, MethodInfo> Consultas, IReadOnlyDictionary<string, MethodInfo> Vistas);

	// ---------------------------------------------------------------- lo que Gnothi llama

	// CARGAR: trae los .dll del modulo y su journal, y deja la superficie instanciada. Gnothi lo
	// llama una vez por modulo; despues las consultas encuentran todo tibio.
	[JSInvokable]
	public static async Task<string> Cargar(string modulo)
	{
		await UnaPorVez.WaitAsync();
		try
		{
			if (Cargados.ContainsKey(modulo))
				return Json(new { ok = true, yaEstaba = true, consultas = Cargados[modulo].Consultas.Keys });

			var reloj = System.Diagnostics.Stopwatch.StartNew();

			// 1 · los ensamblados, que el sustrato nunca referencio
			string indice = await Http.GetStringAsync($"modulo/{modulo}/indice.json");
			using JsonDocument doc = JsonDocument.Parse(indice);
			string entrada = doc.RootElement.GetProperty("entrada").GetString()!;

			Assembly? app = null;
			foreach (JsonElement a in doc.RootElement.GetProperty("archivos").EnumerateArray())
			{
				string nombre = a.GetString()!;
				byte[] bytes = await Http.GetByteArrayAsync($"modulo/{modulo}/dominio/{nombre}.dll");
				Assembly cargada = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(bytes));
				if (string.Equals(nombre, entrada, StringComparison.Ordinal)) app = cargada;
			}
			if (app is null) return Json(new { ok = false, motivo = $"no llego la entrada '{entrada}'" });

			// 2 · la superficie, por NOMBRE de atributo, igual que InProcessShell
			// LOS MISMOS MARCADORES QUE InProcessShell, y por nombre de tipo. Un modulo viejo
			// lleva los de MCP y uno nuevo los del contrato; la sala no es quien decide cual.
			Type? superficie = app.GetTypes().FirstOrDefault(t => t.GetCustomAttributes(false)
				.Any(x => x.GetType().Name is "ModuleSurfaceAttribute" or "McpServerToolTypeAttribute"));
			if (superficie is null) return Json(new { ok = false, motivo = "el ensamblado no trae [ModuleSurface]" });

			Dictionary<string, MethodInfo> consultas = PorMarcador(superficie, "QueryAttribute", "McpServerToolAttribute");
			Dictionary<string, MethodInfo> vistas = PorMarcador(superficie, "ViewAttribute");

			// 3 · el journal, que Gnothi deja en memoria antes de que nadie lo abra
			string concedida = $"{Raiz}/{modulo}";
			await TraerElJournal(modulo, concedida);

			// 4 · la instancia. La convencion es la misma del Host: el string del constructor
			//     recibe la carpeta concedida, que aca es una ruta de MEMFS y no un disco.
			object instancia = Construir(superficie, concedida, 0);

			Cargados[modulo] = new Modulo(superficie, instancia, consultas, vistas);
			reloj.Stop();

			return Json(new
			{
				ok = true,
				ms = reloj.ElapsedMilliseconds,
				superficie = superficie.Name,
				consultas = consultas.Keys,
				vistas = vistas.Keys,

				// LA CONFORMIDAD, NO UNA PROMESA. Se reporta que queda de los OTROS modulos en el
				// momento en que este entra. Si algun dia una de estas lineas dice mas de cero
				// bytes, la frontera entre journals se rompio y el log lo grita.
				//
				// Mismo espiritu que la sonda de CSP de Lab.WebViewWasm: la contencion se
				// afirma intentandola, no declarandola.
				vecinos = Vecinos(modulo),
			});
		}
		catch (Exception e) { return Json(new { ok = false, motivo = Motivo(e) }); }
		finally { UnaPorVez.Release(); }
	}

	// CONSULTAR: invoca una [Query] y devuelve sus hechos aplanados, que es lo que el Host ya
	// hace hoy en InProcessShell.Project antes de que nadie serialice.
	[JSInvokable]
	public static Task<string> Consultar(string modulo, string tool, string argumentosJson) =>
		Invocar(modulo, tool, argumentosJson, soloVistas: false);

	private static async Task<string> Invocar(string modulo, string tool, string argumentosJson, bool soloVistas)
	{
		await UnaPorVez.WaitAsync();
		try
		{
			if (!Cargados.TryGetValue(modulo, out Modulo? m))
				return Json(new { ok = false, motivo = $"el modulo '{modulo}' no esta cargado" });

			// LAS DOS PUERTAS NO SE MEZCLAN. Una vista se busca solo entre las vistas; una
			// consulta puede caer en cualquiera de las dos porque el asistente pregunta por lo
			// declarado y eso ya esta filtrado antes de llegar aca.
			MethodInfo? metodo;
			bool hay = soloVistas
				? m.Vistas.TryGetValue(tool, out metodo)
				: m.Consultas.TryGetValue(tool, out metodo) || m.Vistas.TryGetValue(tool, out metodo);

			if (!hay || metodo is null)
				return Json(new { ok = false, motivo = $"el modulo '{modulo}' no expone '{tool}'" });

			Dictionary<string, JsonElement> argumentos = string.IsNullOrWhiteSpace(argumentosJson)
				? []
				: JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentosJson) ?? [];

			object?[] valores = [.. metodo.GetParameters().Select(p => Atar(p, argumentos))];

			var reloj = System.Diagnostics.Stopwatch.StartNew();
			object? crudo = metodo.Invoke(metodo.IsStatic ? null : m.Instancia, valores);
			reloj.Stop();

			return Json(new { ok = true, ms = reloj.ElapsedMilliseconds, contenido = Aplanar(crudo) });
		}
		catch (Exception e) { return Json(new { ok = false, motivo = Motivo(e) }); }
		finally { UnaPorVez.Release(); }
	}

	// LO QUE PIDE LA PANTALLA, que es otro conjunto que el del asistente: series, puntos, lineas
	// de referencia. Ni se declara en el manifiesto ni le sirve al modelo. Son dos caminos y no
	// un parametro, igual que en el contrato.
	[JSInvokable]
	public static Task<string> Vista(string modulo, string vista, string argumentosJson) =>
		Invocar(modulo, vista, argumentosJson, soloVistas: true);

	// EL SOBRE QUE DEJA EL CARTERO. Gnothi hizo la red; el modulo decide que asienta.
	[JSInvokable]
	public static async Task<string> Entregar(string modulo, string sobreJson)
	{
		await UnaPorVez.WaitAsync();
		try
		{
			if (!Cargados.TryGetValue(modulo, out Modulo? m)) return Json(new { ok = false, motivo = $"'{modulo}' no esta cargado" });
			MethodInfo? metodo = PorMarcadorSuelto(m.Superficie, "DeliverAttribute");
			if (metodo is null) return Json(new { ok = true, contenido = "" });

			object? r = metodo.Invoke(m.Instancia, [sobreJson]);
			return Json(new { ok = true, contenido = r as string ?? "" });
		}
		catch (Exception e) { return Json(new { ok = false, motivo = Motivo(e) }); }
		finally { UnaPorVez.Release(); }
	}

	// HASTA DONDE YA LEYO. Cero por omision, y cero no es un error: quiere decir «traeme todo».
	[JSInvokable]
	public static async Task<string> Cursor(string modulo)
	{
		await UnaPorVez.WaitAsync();
		try
		{
			if (!Cargados.TryGetValue(modulo, out Modulo? m)) return Json(new { ok = true, contenido = 0L });
			MethodInfo? metodo = PorMarcadorSuelto(m.Superficie, "CursorAttribute");
			if (metodo is null) return Json(new { ok = true, contenido = 0L });

			return Json(new { ok = true, contenido = Convert.ToInt64(metodo.Invoke(m.Instancia, null) ?? 0L) });
		}
		catch (Exception e) { return Json(new { ok = false, motivo = Motivo(e) }); }
		finally { UnaPorVez.Release(); }
	}

	// QUE QUEDA DE LOS DEMAS. Lo que este metodo ve, lo ve tambien el dominio de la institucion:
	// corre en el mismo heap y sobre el mismo sistema de archivos en memoria. Por eso sirve como
	// prueba y no como comentario.
	private static object[] Vecinos(string propio)
	{
		if (!Directory.Exists(Raiz)) return [];

		var salida = new List<object>();
		foreach (string carpeta in Directory.GetDirectories(Raiz))
		{
			string nombre = Path.GetFileName(carpeta);
			if (string.Equals(nombre, propio, StringComparison.Ordinal)) continue;

			long bytes = 0;
			int archivos = 0;
			foreach (string f in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
			{
				archivos++;
				try { bytes += new FileInfo(f).Length; } catch { }
			}

			salida.Add(new { modulo = nombre, archivos, bytes });
		}

		return [.. salida];
	}

	private static MethodInfo? PorMarcadorSuelto(Type superficie, string marcador) =>
		superficie.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
			.FirstOrDefault(m => m.GetCustomAttributes(false).Any(a => a.GetType().Name == marcador));

	// LO QUE EL MODULO EXPONE DE VERDAD, para el contraste declarado-contra-real que el Host ya
	// hace con las otras cascaras.
	[JSInvokable]
	public static Task<string> Repertorio(string modulo) =>
		Task.FromResult(Cargados.TryGetValue(modulo, out Modulo? m)
			? Json(new { ok = true, consultas = m.Consultas.Keys, vistas = m.Vistas.Keys })
			: Json(new { ok = false, motivo = $"el modulo '{modulo}' no esta cargado" }));

	// DESALOJAR: cierra la superficie y borra su carpeta de memoria. EL ORDEN IMPORTA y esta
	// medido: borrar la carpeta con el actor todavia vivo mata el runtime, porque el finalizador
	// del motor intenta escribir y una excepcion en un finalizador es fatal.
	[JSInvokable]
	public static async Task<string> Desalojar(string modulo)
	{
		await UnaPorVez.WaitAsync();
		try
		{
			string cerrado = "";
			if (Cargados.Remove(modulo, out Modulo? m)) cerrado = Cerrar(m.Instancia);

			// NUNCA GC.WaitForPendingFinalizers() ACA. MEDIDO: el runtime se muere con 255 al
			// cambiar de modulo. En el motor web no hay hilo finalizador -- el runtime es de un
			// solo hilo -- asi que esperar a que otro hilo corra los finalizadores es esperar a
			// nadie. Un GC.Collect() suelto si es seguro: encola, no espera.
			GC.Collect();

			string suya = $"{Raiz}/{modulo}";
			string desmontada = Desmontar(suya);

			return Json(new
			{
				ok = true,
				cerrado,
				desmontada,
				montadas = Directory.Exists(Raiz) ? Directory.GetDirectories(Raiz) : [],
			});
		}
		catch (Exception e) { return Json(new { ok = false, motivo = Motivo(e) }); }
		finally { UnaPorVez.Release(); }
	}

	// CERRAR LA SUPERFICIE, SIN EXIGIRLE QUE SEA IDisposable. Medido: `FacturasSurface` no lo es
	// -abre y cierra un actor por consulta- y `ExamenesSurface` si. Si se le exigiera, la mitad
	// de los modulos quedarian con el motor vivo mientras se les desmonta la carpeta, que es
	// justamente lo que mata el runtime.
	private static string Cerrar(object instancia)
	{
		if (instancia is IDisposable d)
		{
			try { d.Dispose(); return "IDisposable"; }
			catch (Exception e) { return $"IDisposable fallo: {e.GetType().Name}"; }
		}

		foreach (string nombre in new[] { "Dispose", "Cerrar", "Close", "GracefulExit" })
		{
			MethodInfo? m = instancia.GetType().GetMethod(nombre,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null, Type.EmptyTypes, null);

			if (m is null) continue;

			try { m.Invoke(instancia, null); return nombre; }
			catch (Exception e) { return $"{nombre} fallo: {Motivo(e)}"; }
		}

		return "nada que cerrar";
	}

	// DESMONTAR ES VACIAR, NO BORRAR. Y esto no es una concesion: es lo unico que se puede hacer.
	//
	// MEDIDO, y el rastro lo dice entero:
	//
	//     JournalWriter.Finalize() -> Dispose() -> UpdateFileHeader() -> Seek()
	//     DirectoryNotFoundException: /journal/facturas/facturas/journal/journal_000001.bin
	//     [ERROR] FATAL UNHANDLED EXCEPTION
	//
	// El motor deja un JournalWriter con finalizador pendiente aunque la superficie cierre su
	// actor. En el motor web NO HAY HILO FINALIZADOR: los finalizadores corren en una
	// recoleccion posterior, cuando les toca, y no hay forma soportada de forzarlos. Asi que
	// entre el desalojo y el finalizador hay una ventana que no se puede cerrar -- y si en esa
	// ventana el archivo desaparecio, la excepcion nace ADENTRO del finalizador, que es fatal y
	// no se puede atrapar. Se lleva el runtime entero con 255.
	//
	// Vaciar no tiene ese problema: el finalizador encuentra el archivo, escribe su encabezado
	// sobre un archivo de cero bytes y no tira. Y la propiedad que importa se conserva igual: el
	// modulo que entre despues no tiene nada que leer del anterior.
	//
	// LO QUE SI QUEDA VISIBLE, y hay que decirlo: los NOMBRES de las carpetas. Un modulo puede
	// enterarse de que la persona tiene otros modulos instalados, aunque no de que dicen. Cerrar
	// eso pide que el motor libere sus escritores de forma determinista, y eso es un pedido al
	// equipo del motor, no algo que la sala pueda resolver sola.
	private static string Desmontar(string carpeta)
	{
		if (!Directory.Exists(carpeta)) return "no estaba";

		int vaciados = 0;
		long bytes = 0;
		foreach (string archivo in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
		{
			try
			{
				bytes += new FileInfo(archivo).Length;
				File.WriteAllBytes(archivo, []);
				vaciados++;
			}
			catch { /* si no se puede escribir, tampoco se va a poder leer nada util */ }
		}

		return $"vaciada: {vaciados} archivos, {bytes} bytes que el proximo modulo ya no ve";
	}

	// ---------------------------------------------------------------- adentro

	// EL JOURNAL LLEGA, NO SE ABRE. El modulo nunca pide un archivo: Gnothi sirve el indice y los
	// bytes por el mismo origen de la sala, y aca aterrizan en memoria. Lo que el modulo escriba
	// despues se queda en memoria y no toca la carpeta de la persona.
	private static async Task TraerElJournal(string modulo, string concedida)
	{
		Directory.CreateDirectory(concedida);

		string listado = await Http.GetStringAsync($"modulo/{modulo}/journal/indice.json");
		using JsonDocument doc = JsonDocument.Parse(listado);

		foreach (JsonElement ruta in doc.RootElement.GetProperty("rutas").EnumerateArray())
		{
			string relativa = ruta.GetString()!;
			byte[] bytes = await Http.GetByteArrayAsync($"modulo/{modulo}/journal/{relativa}");
			string destino = Path.Combine(concedida, relativa.Replace('/', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
			File.WriteAllBytes(destino, bytes);
		}
	}

	// LA MISMA CONVENCION QUE InProcessShell.Build, y se copia a proposito en vez de inventar otra:
	// cualquier string que un constructor pida recibe la carpeta concedida, y cualquier otro tipo
	// se construye recursivamente. El Host la llama "la parte mas debil de esta cascara" porque
	// esta adivinando, y tiene razon -- pero si la sala adivinara distinto, un modulo se
	// comportaria de dos maneras segun donde corre, que es peor que adivinar igual.
	private static object Construir(Type t, string concedida, int profundidad)
	{
		if (profundidad > 4)
			throw new InvalidOperationException($"cadena de constructores demasiado honda en {t.Name}");

		ConstructorInfo[] ctors = [.. t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.OrderBy(c => c.GetParameters().Length)];

		if (ctors.Length == 0)
			throw new InvalidOperationException($"{t.Name} no tiene constructor alcanzable");

		ConstructorInfo elegido = ctors[0];
		object?[] valores = [.. elegido.GetParameters().Select(p =>
			p.ParameterType == typeof(string) ? (object?)concedida : Construir(p.ParameterType, concedida, profundidad + 1))];

		return elegido.Invoke(valores);
	}

	private static Dictionary<string, MethodInfo> PorMarcador(Type superficie, params string[] marcadores)
	{
		const BindingFlags Todo = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
		var encontradas = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);

		foreach (MethodInfo m in superficie.GetMethods(Todo))
		{
			object? marca = m.GetCustomAttributes(false).FirstOrDefault(a => marcadores.Contains(a.GetType().Name));
			if (marca is null) continue;

			// El nombre publico sale del marcador cuando lo trae. MCP lo deja en `Name`; si
			// viniera vacio, el metodo se llama como se llama.
			string nombre = (string?)marca.GetType().GetProperty("Name")?.GetValue(marca) is { Length: > 0 } n
				? n
				: m.Name;

			encontradas[nombre] = m;
		}

		return encontradas;
	}

	private static object? Atar(ParameterInfo p, Dictionary<string, JsonElement> argumentos)
	{
		if (!argumentos.TryGetValue(p.Name ?? "", out JsonElement v))
			return p.HasDefaultValue ? p.DefaultValue : (p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null);

		if (p.ParameterType == typeof(string)) return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
		if (p.ParameterType == typeof(int)) return v.ValueKind == JsonValueKind.Number ? v.GetInt32() : int.Parse(v.ToString());
		if (p.ParameterType == typeof(long)) return v.ValueKind == JsonValueKind.Number ? v.GetInt64() : long.Parse(v.ToString());
		if (p.ParameterType == typeof(bool)) return v.ValueKind == JsonValueKind.True || (v.ValueKind != JsonValueKind.False && bool.Parse(v.ToString()));
		if (p.ParameterType == typeof(decimal)) return decimal.Parse(v.ToString());

		return v.ToString();
	}

	// El mismo aplanado que InProcessShell.Project: el grafo .NET a dict/lista/primitivo, porque
	// el [JsonPropertyName] del modulo no es el mismo Type que conoce el serializador del Host.
	private static object? Aplanar(object? v)
	{
		if (v is null) return null;
		Type t = v.GetType();

		if (t.IsPrimitive || v is string || v is decimal || v is DateTime || v is DateTimeOffset) return v;
		if (v is System.Collections.IEnumerable lista) return lista.Cast<object?>().Select(Aplanar).ToList();

		var salida = new Dictionary<string, object?>(StringComparer.Ordinal);
		foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			string declarado = p.GetCustomAttributes(false)
				.FirstOrDefault(a => a.GetType().Name == "JsonPropertyNameAttribute")
				?.GetType().GetProperty("Name")?.GetValue(p.GetCustomAttributes(false)
					.First(a => a.GetType().Name == "JsonPropertyNameAttribute")) as string ?? "";

			string etiqueta = declarado.Length > 0 ? declarado : JsonNamingPolicy.CamelCase.ConvertName(p.Name);
			try { salida[etiqueta] = Aplanar(p.GetValue(v)); }
			catch (Exception e) { salida[etiqueta] = $"<{e.GetType().Name}>"; }
		}

		return salida;
	}

	private static string Motivo(Exception e)
	{
		Exception raiz = e is TargetInvocationException t && t.InnerException is not null ? t.InnerException : e;
		return $"{raiz.GetType().Name}: {raiz.Message}";
	}

	private static string Json(object o) => JsonSerializer.Serialize(o);
}
