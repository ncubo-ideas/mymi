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

	// `Montado` es la FOTO de lo que Gnothi puso: ruta relativa y bytes, tal como llegaron. Sirve
	// para una sola cosa, y es la que faltaba: saber que escribio el modulo mientras estuvo aca.
	private sealed record Modulo(
		Type Superficie,
		object Instancia,
		IReadOnlyDictionary<string, MethodInfo> Consultas,
		IReadOnlyDictionary<string, MethodInfo> Vistas,
		Dictionary<string, byte[]> Montado,

		// DONDE QUEDO MONTADO, y ya no se puede deducir del nombre: ver `Neutral`. Se guarda aca
		// porque el desalojo y la recoleccion de escrituras tienen que ir a la MISMA carpeta a la
		// que se monto, y no a una que se recalcule.
		string Carpeta);

	// EL NOMBRE DE LA CARPETA NO ES EL DEL MODULO, Y ESA ES LA CORRECCION.
	//
	// Antes se montaba en `/journal/<modulo>`. Como el desmontaje VACIA en vez de borrar -- borrar
	// mata el runtime desde un finalizador, esta medido y explicado en `Desmontar` --, las
	// carpetas de los modulos anteriores quedaban ahi, vacias pero CON SU NOMBRE. Un modulo que
	// entraba despues listaba `/journal` y se enteraba de que la persona tiene compras, examenes,
	// facturas. Cero bytes de contenido y toda la lista de instituciones.
	//
	// Lo habia dado por perdido -- "que el motor libere sus escritores de forma determinista es un
	// pedido al equipo del motor" --, y Jonathan devolvio que no: eso es la mitad del invariante
	// de la sala, no un problema ajeno. Tiene razon, y no hacia falta el motor: hacia falta que la
	// carpeta no se llamara como el modulo.
	//
	// Ahora cada carga monta en una carpeta NUEVA y neutra. Lo que queda de las anteriores es
	// `c1`, `c2`, `c3`: dice cuantas veces se cargo algo en esta sesion, y nada mas. Que hubo
	// alguien antes es inevitable en un sistema de archivos compartido; QUIEN era, no.
	//
	// Y de yapa arregla algo que el nombre por modulo escondia: una carpeta nueva no puede traer
	// restos de la anterior, asi que el modulo que entra no encuentra segmentos de journal vacios
	// que su motor no escribio.
	private static int cargas;

	private static string Neutral() => $"{Raiz}/c{++cargas}";

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

			// UN MODULO POR VEZ, Y LO SOSTIENE EL SUSTRATO, NO LA CASCARA.
			//
			// Hasta aca "uno por vez" vivia entero en un `static string` de `SalaShell`: si esa
			// variable se equivocaba, la cascara se saltaba el desalojo y ESTE metodo cargaba el
			// segundo modulo encima, con los dos journals montados. Justo la unica situacion que
			// la sala no puede permitir, y sin una linea de log.
			//
			// Jonathan lo puso en una frase: toda la contencion de la sala colgaba de una
			// variable. Ahora el que decide es el que tiene los journals a la vista, y ante la
			// duda no carga: una consulta que falla es recuperable, dos journals montados no.
			if (Cargados.Count > 0)
				return Json(new
				{
					ok = false,
					motivo = $"ya hay un modulo cargado ('{Cargados.Keys.First()}') y la sala carga uno por vez: "
						+ $"hay que desalojarlo antes de montar '{modulo}'",
				});

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

			// 3 · SE BARRE AL ENTRAR, NO SOLO AL SALIR. Y esta es la mitad que faltaba.
			//
			// MEDIDO EN EL EMULADOR, y es la falla «corre tarde» que Jonathan nombro sin verla:
			// al desalojar, la carpeta queda en 0 bytes; dos cargas despues tiene 30. Los escribe
			// el FINALIZADOR del JournalWriter del motor, que se despierta cuando le toca -- ya
			// vaciada la carpeta -- y vuelve a poner el encabezado del archivo. El propio §2⑤ de
			// la nota decia que eso pasaba; lo que no decia es que esos bytes quedan a la vista
			// del modulo que entre despues.
			//
			// Un vaciado al salir no puede cerrarlo, porque el escritor llega tarde por
			// definicion. Barrer al ENTRAR si: sea cuando sea que el finalizador haya escrito,
			// fue antes de este momento.
			//
			// Queda una ventana angosta -- un finalizador que se despierte con el modulo nuevo ya
			// adentro -- y no se cierra desde aca: pide que el motor libere sus escritores de
			// forma determinista. La diferencia con antes es que ahora esta ACOTADA y MEDIDA, y
			// que `vecinos` la reporta en bytes para que la cascara falle si alguna vez no es
			// cero.
			object barrido = Barrer();

			// 4 · el journal, que Gnothi deja en memoria antes de que nadie lo abra
			string concedida = Neutral();
			Dictionary<string, byte[]> montado = await TraerElJournal(modulo, concedida);

			// 5 · la instancia. La convencion es la misma del Host: el string del constructor
			//     recibe la carpeta concedida, que aca es una ruta de MEMFS y no un disco.
			object instancia = Construir(superficie, concedida, 0);

			Cargados[modulo] = new Modulo(superficie, instancia, consultas, vistas, montado, concedida);
			reloj.Stop();

			return Json(new
			{
				ok = true,
				ms = reloj.ElapsedMilliseconds,
				superficie = superficie.Name,
				consultas = consultas.Keys,
				vistas = vistas.Keys,

				// CUANTO LE LLEGO DE SU PROPIO REGISTRO. Es lo que prueba que el viaje de vuelta
				// funciono la vez anterior: un modulo que sembro y se fue tiene que encontrar sus
				// archivos al volver.
				montado = new { archivos = montado.Count, bytes = montado.Values.Sum(b => (long)b.Length) },

				// LA CONFORMIDAD, NO UNA PROMESA. Se reporta que queda de los OTROS modulos en el
				// momento en que este entra. Si algun dia una de estas lineas dice mas de cero
				// bytes, la frontera entre journals se rompio y el log lo grita.
				//
				// Mismo espiritu que la sonda de CSP de Lab.WebViewWasm: la contencion se
				// afirma intentandola, no declarandola.
				// QUE HABIA QUE BARRER AL ENTRAR. No es un detalle de limpieza: es la medida de
				// la ventana del finalizador, y la unica forma de saber si se ensancha.
				barrido,

				vecinos = Vecinos(concedida),
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

			// UNA CONSULTA PUEDE SER ASINCRONA, y hasta aca el sustrato devolvia el `Task`
			// aplanado -- o sea, basura con forma de objeto -- en vez de su resultado.
			//
			// Y EN EL MOTOR WEB NO ES UN LUJO. El runtime es de UN SOLO HILO: un modulo que
			// intente esperar el suyo con `.GetAwaiter().GetResult()` no se cuelga, tira
			// «PlatformNotSupportedException: Cannot wait on monitors on this runtime». O sea que
			// devolver la tarea y que la espere el sustrato no es una comodidad: es el unico
			// camino que tiene un modulo para hacer algo asincrono aca adentro.
			//
			// Salio de correr la sonda de fuga: sus intentos de red daban «no pudo» sin haber
			// tocado la red.
			if (crudo is Task tarea)
			{
				await tarea;
				crudo = tarea.GetType().IsGenericType
					? tarea.GetType().GetProperty("Result")?.GetValue(tarea)
					: null;
			}

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

			// Y LO QUE ASENTO SE VA CON LA RESPUESTA. Sin esto el refresco parecia funcionar y se
			// evaporaba al desalojar, que es la peor clase de falla.
			return Json(new { ok = true, contenido = r as string ?? "", escrituras = Escrituras(modulo, m) });
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
	// BARRER LO QUE DEJARON LAS CARGAS ANTERIORES, y devolver cuanto habia. Corre al ENTRAR, que
	// es el unico momento desde el que se puede afirmar algo sobre un escritor que llega tarde.
	//
	// Vacia TODO lo que haya bajo la raiz. En este punto no hay ningun modulo cargado -- lo
	// garantiza el rechazo de mas arriba -- asi que no hay nada de nadie vivo que se pueda romper.
	private static object Barrer()
	{
		if (!Directory.Exists(Raiz)) return new { carpetas = 0, bytes = 0L, vaciadas = true };

		string[] carpetas = Directory.GetDirectories(Raiz);
		long bytes = 0;
		bool todas = true;

		foreach (string carpeta in carpetas)
		{
			foreach (string archivo in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
				try { bytes += new FileInfo(archivo).Length; } catch { }

			(bool vaciada, string _) = Desmontar(carpeta);
			todas &= vaciada;
		}

		return new { carpetas = carpetas.Length, bytes, vaciadas = todas };
	}

	// Y AHORA LO QUE INFORMA ES OTRA COSA, mejor: antes decia «quedo la carpeta de compras»; ahora
	// dice «quedaron 3 carpetas de cargas anteriores, 0 bytes». El nombre ya no esta -- ver
	// `Neutral` --, asi que lo unico que se puede afirmar es lo unico que importa: cuanto pesa lo
	// que quedo. Si algun dia eso no es cero, la frontera se rompio.
	private static object[] Vecinos(string propia)
	{
		if (!Directory.Exists(Raiz)) return [];

		var salida = new List<object>();
		foreach (string carpeta in Directory.GetDirectories(Raiz))
		{
			string nombre = Path.GetFileName(carpeta);
			if (string.Equals(carpeta, propia, StringComparison.Ordinal)) continue;

			long bytes = 0;
			int archivos = 0;
			foreach (string f in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
			{
				archivos++;
				try { bytes += new FileInfo(f).Length; } catch { }
			}

			salida.Add(new { carga = nombre, archivos, bytes });
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
			object[] escrituras = [];
			(bool vaciada, string detalle) desmontaje;

			// SU CARPETA, SI SE SABE CUAL ES. Ya no se puede deducir del nombre del modulo, asi
			// que la fuente es el propio `Modulo`. Y si no esta cargado -- porque un desalojo
			// anterior fallo a la mitad, o porque la cascara se equivoco de nombre -- se vacia
			// TODO lo que haya bajo la raiz.
			//
			// Vaciar de mas es seguro: lo que esta montado es de un solo modulo y ya se le
			// recogieron las escrituras, o no hay nadie. Vaciar de menos es dejar la frontera
			// abierta, que es lo unico que no se puede.
			Cargados.Remove(modulo, out Modulo? m);
			string suya = m?.Carpeta ?? Raiz;

			// EL DESMONTAJE VA EN UN `finally`, Y NO ES UN DETALLE DE ESTILO.
			//
			// Antes iba despues de cerrar y de recoger, dentro del mismo `try`. Si cualquiera de
			// esos dos tiraba, el `catch` de abajo devolvia un error y LA CARPETA NUNCA SE VACIABA.
			// Como la frontera de la sala ES el vaciado, eso la dejaba abierta sin que nada lo
			// dijera: el modulo siguiente entraba y encontraba el journal del anterior.
			//
			// Es el modo de falla «que no corra» que Jonathan nombro, y estaba vivo en el codigo.
			// Cerrar y recoger pueden fallar; vaciar, no.
			try
			{
				if (m is not null)
				{
					cerrado = Cerrar(m.Instancia);

					// El orden importa: primero cerrar la superficie -- para que el motor haya
					// volcado lo suyo --, despues recoger lo que escribio, y recien vaciar.
					escrituras = Escrituras(modulo, m);
				}

				// NUNCA GC.WaitForPendingFinalizers() ACA. MEDIDO: el runtime se muere con 255 al
				// cambiar de modulo. En el motor web no hay hilo finalizador -- es de un solo
				// hilo -- asi que esperar a que otro hilo corra los finalizadores es esperar a
				// nadie. Un GC.Collect() suelto si es seguro: encola, no espera.
				GC.Collect();
			}
			finally
			{
				desmontaje = Desmontar(suya);
			}

			return Json(new
			{
				ok = true,

				// `vaciada` ES LA AFIRMACION QUE LA CASCARA MIRA. Sin un booleano, el Host tenia
				// que adivinar leyendo una frase, y no lo hacia: seguia adelante igual.
				vaciada = desmontaje.vaciada,

				cerrado,
				desmontada = desmontaje.detalle,
				escrituras,
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
	private static (bool vaciada, string detalle) Desmontar(string carpeta)
	{
		if (!Directory.Exists(carpeta)) return (true, "no estaba");

		int vaciados = 0, fallados = 0;
		long bytes = 0;

		try
		{
			foreach (string archivo in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
			{
				try
				{
					bytes += new FileInfo(archivo).Length;
					File.WriteAllBytes(archivo, []);
					vaciados++;
				}
				catch { fallados++; }
			}
		}
		catch (Exception e)
		{
			return (false, $"NO SE PUDO RECORRER la carpeta: {e.GetType().Name}");
		}

		// UN SOLO ARCHIVO QUE NO SE PUDO VACIAR ES UNA FRONTERA ROTA, no un detalle. Se dice que
		// no, y la cascara decide -- que es fallar la consulta, no seguir como si nada.
		return fallados == 0
			? (true, $"vaciada: {vaciados} archivos, {bytes} bytes que el proximo modulo ya no ve")
			: (false, $"QUEDARON {fallados} archivos sin vaciar de {vaciados + fallados}");
	}

	// ---------------------------------------------------------------- adentro

	// EL JOURNAL LLEGA, NO SE ABRE. El modulo nunca pide un archivo: Gnothi sirve el indice y los
	// bytes por el mismo origen de la sala, y aca aterrizan en memoria. Lo que el modulo escriba
	// despues se queda en memoria y no toca la carpeta de la persona.
	private static async Task<Dictionary<string, byte[]>> TraerElJournal(string modulo, string concedida)
	{
		Directory.CreateDirectory(concedida);

		var montado = new Dictionary<string, byte[]>(StringComparer.Ordinal);
		string listado = await Http.GetStringAsync($"modulo/{modulo}/journal/indice.json");
		using JsonDocument doc = JsonDocument.Parse(listado);

		foreach (JsonElement ruta in doc.RootElement.GetProperty("rutas").EnumerateArray())
		{
			string relativa = ruta.GetString()!;
			byte[] bytes = await Http.GetByteArrayAsync($"modulo/{modulo}/journal/{relativa}");
			string destino = Path.Combine(concedida, relativa.Replace('/', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
			File.WriteAllBytes(destino, bytes);
			montado[relativa] = bytes;
		}

		return montado;
	}

	// EL VIAJE DE VUELTA, que es lo que faltaba para que la sala sirviera para algo mas que
	// consultar.
	//
	// El modulo escribe en memoria y no sabe que hay un disco del otro lado. Asi que despues de
	// cada operacion que pudo escribir, se compara lo que hay contra la foto de lo que Gnothi
	// puso, y lo que cambio viaja de vuelta. Gnothi lo guarda en la carpeta de la persona, con su
	// propio cerco de ruta: el modulo no elige donde aterriza.
	//
	// Se devuelve en base64 porque la respuesta es JSON. Un journal pesa kilobytes -- medido, los
	// cuatro del telefono suman 174 KB -- asi que no hay nada que optimizar todavia.
	private static object[] Escrituras(string modulo, Modulo m)
	{
		// LA CARPETA SALE DEL MODULO, NO DE SU NOMBRE. Recalcularla desde el nombre era posible
		// cuando la carpeta SE LLAMABA como el modulo; ahora es `c7` y la unica fuente es esta.
		string concedida = m.Carpeta;
		if (!Directory.Exists(concedida)) return [];

		var cambios = new List<object>();

		foreach (string archivo in Directory.EnumerateFiles(concedida, "*", SearchOption.AllDirectories))
		{
			string relativa = Path.GetRelativePath(concedida, archivo).Replace(Path.DirectorySeparatorChar, '/');

			byte[] ahora;
			try { ahora = File.ReadAllBytes(archivo); }
			catch { continue; }

			if (m.Montado.TryGetValue(relativa, out byte[]? antes) && antes.AsSpan().SequenceEqual(ahora)) continue;

			cambios.Add(new { ruta = relativa, contenido = Convert.ToBase64String(ahora) });
			m.Montado[relativa] = ahora;
		}

		return [.. cambios];
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
