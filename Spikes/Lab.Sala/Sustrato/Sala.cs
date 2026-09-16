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
			});
		}
		catch (Exception e) { return Json(new { ok = false, motivo = Motivo(e) }); }
		finally { UnaPorVez.Release(); }
	}

	// CONSULTAR: invoca una [Query] y devuelve sus hechos aplanados, que es lo que el Host ya
	// hace hoy en InProcessShell.Project antes de que nadie serialice.
	[JSInvokable]
	public static async Task<string> Consultar(string modulo, string tool, string argumentosJson)
	{
		await UnaPorVez.WaitAsync();
		try
		{
			if (!Cargados.TryGetValue(modulo, out Modulo? m))
				return Json(new { ok = false, motivo = $"el modulo '{modulo}' no esta cargado" });

			if (!m.Consultas.TryGetValue(tool, out MethodInfo? metodo)
				&& !m.Vistas.TryGetValue(tool, out metodo))
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
			if (Cargados.Remove(modulo, out Modulo? m) && m.Instancia is IDisposable d) d.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();

			string suya = $"{Raiz}/{modulo}";
			if (Directory.Exists(suya)) Directory.Delete(suya, true);

			return Json(new { ok = true, montadas = Directory.Exists(Raiz) ? Directory.GetDirectories(Raiz) : [] });
		}
		catch (Exception e) { return Json(new { ok = false, motivo = Motivo(e) }); }
		finally { UnaPorVez.Release(); }
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
