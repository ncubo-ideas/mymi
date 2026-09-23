using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

// EL SPIKE DE LA FILA I (nota 49): un sustrato de Gnothi compilado para el motor web y
// RECORTADO, que recibe despues los modulos de las instituciones -ensamblados que nunca
// referencio- y los corre a todos en UN SOLO runtime.
//
// Lo que mide: si el sustrato se comparte de verdad. El numero que decide es cuanto crece
// la memoria lineal del wasm con cada modulo que se suma.

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
WebAssemblyHost host = builder.Build();

IJSRuntime js = host.Services.GetRequiredService<IJSRuntime>();
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

async Task Decir(string linea)
{
	Console.WriteLine(linea);
	await js.InvokeVoidAsync("registrar", linea);
}

async Task<double> Lineal()
{
	long b = await js.InvokeAsync<long>("memoriaWasm");
	return b < 0 ? -1 : b / 1048576.0;
}

const BindingFlags Todo = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
var resumen = new List<string>();
var vivos = new List<(string modulo, object instancia)>();

// DOS MODOS, UN SOLO BINARIO.
//
// HOSPEDADA: Gnothi registro su puente, asi que la sala no hace nada por su cuenta -- se queda
// esperando a que le pidan cargar un modulo o contestar una consulta (ver Sala.cs).
//
// SUELTA: no hay nadie del otro lado, asi que corre la barrida de medicion de la nota 49. Es el
// laboratorio, y es lo que produjo los numeros del LEEME.
Sala.Sustrato.Sala.ConfigurarDireccion(new Uri(builder.HostEnvironment.BaseAddress));

if (await js.InvokeAsync<bool>("salaHospedada"))
{
	await Decir("la sala esta lista, hospedada por Gnothi. Esperando pedidos.");
	await js.InvokeVoidAsync("salaLista");
	await host.RunAsync();
	return;
}

try
{
	await Decir("=== 0 · el sustrato, antes de conocer a ningun modulo ===");
	await Decir($"    runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
	await Decir($"    ensamblados cargados: {AppDomain.CurrentDomain.GetAssemblies().Length}");
	double base0 = await Lineal();
	await Decir($"    memoria lineal del wasm: {base0:0.0} MB   ·   heap administrado: {GC.GetTotalMemory(false) / 1048576.0:0.0} MB");
	await Decir("");

	Ficha[] fichas = await http.GetFromJsonAsync<Ficha[]>("modulos/indice.json");
	double anterior = base0;

	foreach (Ficha f in fichas)
	{
		await Decir($"=== modulo: {f.modulo} ===");

		// 1 · los bytes que entrega la institucion
		int bytes = 0;
		Assembly entrada = null;
		foreach (string nombre in f.archivos.Split(','))
		{
			byte[] crudo = await http.GetByteArrayAsync($"modulos/{f.modulo}/dominio/{nombre}.dll");
			bytes += crudo.Length;
			Assembly a = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(crudo));
			if (nombre == f.entrada) entrada = a;
		}

		// 2 · la superficie, por NOMBRE de atributo, igual que InProcessShell
		Type superficie = entrada.GetTypes().FirstOrDefault(t => t.GetCustomAttributes(false)
			.Any(a => a.GetType().Name == "ModuleSurfaceAttribute"));
		if (superficie is null) { await Decir("    sin [ModuleSurface]"); continue; }

		var consultas = new List<(string nombre, MethodInfo m)>();
		foreach (MethodInfo m in superficie.GetMethods(Todo))
		{
			object marca = m.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == "QueryAttribute");
			if (marca is null) continue;
			consultas.Add(((string)marca.GetType().GetProperty("Name")?.GetValue(marca) ?? m.Name, m));
		}
		int vistas = superficie.GetMethods(Todo).Count(m => m.GetCustomAttributes(false).Any(a => a.GetType().Name == "ViewAttribute"));
		await Decir($"    {bytes / 1024} KB de .dll   ·   {superficie.Name}   ·   {consultas.Count} [Query], {vistas} [View]");

		// 3 · su carpeta, que solo existe en memoria y es SUYA
		string actor = (superficie.GetField("ActorName", BindingFlags.NonPublic | BindingFlags.Static)
			?.GetRawConstantValue() as string) ?? f.modulo;
		string concedida = "/journal/" + f.modulo;
		Directory.CreateDirectory($"{concedida}/{actor}/journal");

		// 4 · instanciar e invocar
		ConstructorInfo ctor = superficie.GetConstructors().OrderBy(c => c.GetParameters().Length).First();
		object instancia = ctor.Invoke(ctor.GetParameters()
			.Select(p => p.ParameterType == typeof(string) ? (object)concedida : null).ToArray());

		var reloj = System.Diagnostics.Stopwatch.StartNew();
		(string nombre, MethodInfo m) primera = consultas.First(c => c.m.GetParameters().Length == 0);
		object crudo2 = primera.m.Invoke(instancia, []);
		long frio = reloj.ElapsedMilliseconds;
		reloj.Restart();
		primera.m.Invoke(instancia, []);
		long tibio = reloj.ElapsedMilliseconds;

		await Decir($"    {primera.nombre}()  frio {frio} ms · tibio {tibio} ms  ->  {Aplanar(crudo2)}");

		double ahora = await Lineal();
		await Decir($"    memoria lineal: {ahora:0.0} MB   (+{ahora - anterior:0.0} desde el modulo anterior)   ·   heap administrado: {GC.GetTotalMemory(false) / 1048576.0:0.0} MB");
		await Decir("");
		vivos.Add((f.modulo, instancia));
		resumen.Add($"{f.modulo,-14} {bytes / 1024,5} KB   frio {frio,5} ms   tibio {tibio,4} ms   lineal {ahora,6:0.0} MB (+{ahora - anterior:0.0})");
		anterior = ahora;
	}

	await Decir("=== EL SUSTRATO COMPARTIDO, MEDIDO ===");
	foreach (string l in resumen) await Decir("    " + l);
	double fin = await Lineal();
	await Decir("");
	await Decir($"    sustrato solo, antes de cualquier modulo: {base0:0.0} MB");
	await Decir($"    con los {resumen.Count} modulos cargados y contestando: {fin:0.0} MB");
	await Decir($"    costo de los {resumen.Count} modulos juntos: {fin - base0:0.0} MB   ·   promedio por modulo: {(fin - base0) / Math.Max(1, resumen.Count):0.0} MB");
	await Decir($"    ensamblados en el runtime: {AppDomain.CurrentDomain.GetAssemblies().Length}");
	await Decir($"    heap administrado tras recolectar: {GC.GetTotalMemory(true) / 1048576.0:0.0} MB");

	// =====================================================================================
	// EL AISLAMIENTO, PROBADO EN VEZ DE AFIRMADO.
	// Este codigo corre EN EL MISMO LUGAR que el dominio de una institucion: mismo heap,
	// mismo sistema de archivos en memoria. Asi que lo que yo alcance desde aca, lo alcanza
	// un modulo hostil. Tres preguntas, en orden de lo que mas importa.
	// =====================================================================================
	await Decir("");
	await Decir("=== 6 · el aislamiento, probado desde adentro del sandbox ===");

	await Decir("");
	await Decir("  a · contra el APARATO: lo que un modulo puede alcanzar del telefono");
	foreach (string ruta in new[] { "/sdcard", "/data", "/data/data", "/storage/emulated/0", "/system", "C:\\", "/etc/hosts" })
		await Decir($"      {ruta,-24} existe: {Directory.Exists(ruta) || File.Exists(ruta)}");
	await Decir($"      raices visibles: {string.Join(" ", Directory.GetDirectories("/"))}");
	await Decir("      -> lo que se ve es el sistema de archivos EN MEMORIA, no el del telefono");

	await Decir("");
	await Decir("  b · contra OTRO MODULO, con las cinco carpetas montadas a la vez");
	string ajeno = "/journal/compras";
	string[] deOtro = Directory.Exists(ajeno) ? Directory.GetFiles(ajeno, "*", SearchOption.AllDirectories) : [];
	if (deOtro.Length > 0)
	{
		byte[] robado = File.ReadAllBytes(deOtro[0]);
		await Decir($"      leer {deOtro[0]} desde el contexto de otro modulo: LO LEE, {robado.Length} bytes");
		await Decir("      -> NO hay frontera entre modulos: un solo heap y un solo MEMFS");
	}
	else await Decir($"      {ajeno} no tiene archivos para intentar");

	await Decir("");
	await Decir("  c · y si Gnothi DESALOJA el modulo y desmonta su carpeta");
	// MEDIDO, y es una restriccion de diseno: borrar la carpeta con el actor todavia vivo
	// mata el runtime. El finalizador de JournalWriter intenta escribir, no encuentra el
	// archivo, y una excepcion en un finalizador es FATAL UNHANDLED. Primero se desaloja.
	int desalojados = 0;
	foreach ((string modulo, object instancia) v in vivos.Where(v => v.modulo != "examenes"))
	{
		if (v.instancia is IDisposable d) { d.Dispose(); desalojados++; }
	}
	GC.Collect();
	GC.WaitForPendingFinalizers();
	await Decir($"      desalojados: {desalojados} de {vivos.Count - 1}");
	foreach (string otro in Directory.GetDirectories("/journal").Where(d => !d.EndsWith("examenes")))
		Directory.Delete(otro, true);
	await Decir($"      montadas ahora: {string.Join(" ", Directory.GetDirectories("/journal"))}");
	try
	{
		File.ReadAllBytes(deOtro.Length > 0 ? deOtro[0] : "/journal/compras/compras/meta.bin");
		await Decir("      leer el journal ajeno: LO LEE (no deberia)");
	}
	catch (Exception ex)
	{
		await Decir($"      leer el journal ajeno: {ex.GetType().Name}");
		await Decir("      -> la frontera existe si Gnothi desaloja y monta de a uno. Es decision suya, no del modulo");
	}
	await Decir($"      memoria lineal tras desalojar cuatro: {await Lineal():0.0} MB   ·   heap: {GC.GetTotalMemory(true) / 1048576.0:0.0} MB");

	// La tercera capa: los objetos. Montar de a uno no la arregla -- dos dominios cargados
	// comparten el heap--. La unica salida barata es DESCARGAR el modulo entre llamadas, y eso
	// exige un AssemblyLoadContext coleccionable. En .NET nativo anda; en wasm hay que medirlo.
	await Decir("");
	await Decir("  d · ¿se puede DESCARGAR un modulo? (decide si el aislamiento se puede graduar)");
	try
	{
		var aparte = new AssemblyLoadContext("modulo:prueba", isCollectible: true);
		await Decir($"      AssemblyLoadContext(isCollectible: true): creado, IsCollectible={aparte.IsCollectible}");
		byte[] unos = await http.GetByteArrayAsync("modulos/municipalidad/Municipalidad.Dominio.dll");
		Assembly suelta = aparte.LoadFromStream(new MemoryStream(unos));
		await Decir($"      cargado aparte: {suelta.GetName().Name}, {suelta.GetTypes().Length} tipos");
		var testigo = new WeakReference(suelta, true);
		aparte.Unload();
		suelta = null;
		for (int i = 0; i < 3 && testigo.IsAlive; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
		await Decir($"      despues de Unload y recolectar, sigue vivo: {testigo.IsAlive}");
		await Decir(testigo.IsAlive
			? "      -> se descarga el contexto pero el ensamblado no se soltó en este intento"
			: "      -> SE PUEDE: un modulo por llamada es implementable, y con el motor tibio");
	}
	catch (Exception ex)
	{
		await Decir($"      NO se puede: {ex.GetType().Name}: {ex.Message}");
		await Decir("      -> sin descarga, el aislamiento entre modulos pide la caja de Jonathan");
	}
}
catch (Exception e)
{
	Exception raiz = e is TargetInvocationException t && t.InnerException is not null ? t.InnerException : e;
	await Decir("");
	await Decir($"!!! FALLO: {raiz.GetType().FullName}: {raiz.Message}");
	foreach (string l in (raiz.StackTrace ?? "").Split('\n').Take(10)) await Decir("    " + l.TrimEnd());
}

await host.RunAsync();

static string Aplanar(object v)
{
	if (v is null) return "null";
	Type t = v.GetType();
	if (t.IsPrimitive || v is string || v is decimal || v is DateTime) return v.ToString();
	if (v is System.Collections.IEnumerable e && v is not string)
		return "[" + string.Join(", ", e.Cast<object>().Take(3).Select(Aplanar)) + "]";
	string texto = "{" + string.Join(", ", t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
		.Take(5).Select(p => p.Name + "=" + Aplanar(SinRomper(p, v)))) + "}";
	return texto.Length > 150 ? texto.Substring(0, 150) + "..." : texto;
}

static object SinRomper(PropertyInfo p, object v)
{
	try { return p.GetValue(v); } catch (Exception ex) { return "<" + ex.GetType().Name + ">"; }
}

public sealed class Ficha
{
	public string modulo { get; set; }
	public string entrada { get; set; }
	public string archivos { get; set; }
}
