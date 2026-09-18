using System;
using System.IO;
using System.Threading.Tasks;
using Choreography.Theater;
using Compras.App.Eventos;
using Compras.App.Vistas;
using Gnothi.Contract;
using Puppeteer;
using Puppets.Portador;

namespace Compras.App
{
	// THE CARGO SURFACE OF COMPRAS. The same domain, exposed to the Host WITHOUT the MCP SDK:
	// it wears [ModuleSurface]/[Query] from Gnothi.Contract -a tiny, zero-dependency label the
	// Host lends- instead of [McpServerTool]. That is what lets the module ship as two small
	// DLLs (this + Compras.Dominio) and borrow Puppeteer, Choreography and this contract from
	// the Host.
	//
	// ANSWERING NEVER WRITES, and since the hybrid form that is a property of the METHODS, not of
	// a second actor: every [Query] and [View] goes through ComprasAPI's PerformQuery path, and
	// the only thing that writes is [Deliver].
	//
	// It used to keep a separate read-only actor -same name, same domain, OnHydrated empty- so a
	// query could never journal the seed. That bought less than it looked and cost two actors over
	// one journal. See `app` at the end.
	//
	// The Host builds this by passing the granted folder to the string constructor (the
	// InProcessShell convention). It does NOT touch the app's own branding: this is a separate
	// cargo artifact for the assistant path.
	[ModuleSurface]
	public sealed class ComprasSurface : IDisposable
	{
		private const string ActorName = "compras";

		/// <summary>
		/// EN QUE IDIOMA SE DICE UNA VISTA, y lo dice QUIEN LA PIDE.
		/// <para>
		/// Esta clase no tiene navegador: el Host le entrega una carpeta concedida y nada mas, y
		/// Gnothi.Contract no lleva idioma. Pero la cara que la consulta SI lo es, y ya leyo la
		/// eleccion del aparato con <c>navigator.languages</c>, asi que la manda como un parametro mas
		/// de la vista.
		/// </para>
		/// <para>
		/// Una etiqueta vacia o que este modulo no habla cae al idioma del registro del supermercado,
		/// que es la misma caida que hace la app suelta cuando no puede leer la eleccion. Por eso el
		/// parametro es opcional: un llamador que no diga nada sigue recibiendo lo de antes.
		/// </para>
		/// <para>
		/// LAS DOS <c>[Query]</c> NO LO TOMAN, y esa asimetria es la regla y no un olvido: lo que el
		/// asistente pregunta lo contesta la institucion en SU idioma, no en el de quien pregunta.
		/// </para>
		/// </summary>
		private static string Elegido(string idioma) =>
			Frases.Frases.EsUnaEtiquetaConocida(idioma) ? idioma : ComprasAPI.IdiomaDelRegistro;

		private readonly string folder;

		public ComprasSurface(string grantedFolder)
		{
			if (string.IsNullOrWhiteSpace(grantedFolder))
				throw new ArgumentException("the granted folder is required", nameof(grantedFolder));
			folder = Path.GetFullPath(grantedFolder);
			app = new Lazy<ComprasApp>(Arrancar);
		}

		private bool HasJournal => Directory.Exists(Path.Combine(folder, ActorName, "journal"));

		[Query(ConsultasDelModulo.Resumen)]
		public object Resumen()
		{
			if (!HasJournal) return NoData();
			return Read(ConsultasDelModulo.HacerResumen);
		}

		[Query(ConsultasDelModulo.ListaDeCompras)]
		public object ListaDeCompras()
		{
			if (!HasJournal) return NoData();
			return Read(ConsultasDelModulo.HacerListaDeCompras);
		}

		// ------------------------------------------------------------ lo que pinta la cara
		//
		// ESTAS NO SON TOOLS, y la diferencia es el punto entero: [Query] es lo que el ASISTENTE
		// puede preguntar -dos, gruesas, declaradas en el manifiesto y apagables una por una-,
		// mientras que una PANTALLA necesita la canasta del mes, los precios que se movieron y el
		// ritmo de cada producto. Declararlas todas como tools haria que el alta reportara un
		// monton de 'undeclared' contra un manifiesto que nombra dos.
		//
		// Todas usan el MISMO actor que las tools y que la entrega: uno solo, vivo. Preguntar
		// tampoco escribe, porque todas pasan por el camino de PerformQuery de ComprasAPI.

		// LA PORTADA, QUE ES LA PANTALLA ENTERA DE ARRIBA: el carrusel de senales, la linea de
		// «87 compras tuyas desde enero de 2025», y el renglon de estado con cuantos cambios hay.
		//
		// VA JUNTA Y ARMADA, y eso es a proposito. Las tres senales del carrusel -- novedad,
		// oportunidad, promo -- no son «los datos»: son una ELECCION entre datos, con reglas que
		// el producto fijo (una novedad vieja no es noticia; una promo de algo que ella no compra
		// no se fuerza). Devolver los hechos crudos y dejar que la pantalla elija habria puesto
		// esas reglas del lado que se puede reemplazar sin tocar el dominio, y habria dejado que
		// la app suelta y esta cara contesten distinto sobre los mismos hechos.
		//
		// Es el mismo reparto que en las otras cuatro caras: la superficie decide QUE se dice, la
		// cara decide COMO se ve.
		[View("portada")]
		public object Portada(string idioma = "") => !HasJournal ? NoData() : Read(api =>
		{
			string dicho = Elegido(idioma);
			DateTime ahora = DateTime.Now;
			RangoDelHistorialVista rango = api.Rango();
			CambioDeLaCanasta cambio = api.CambioDelQuiebre(dicho);
			var ritmos = api.RitmosDelQuiebre(dicho);
			var precios = api.Precios(dicho);
			int cantidad = api.CantidadDeCompras();
			PrecioQueCambio habitual = LaPortadaDeLaCara.Oportunidad(precios);

			return (object)new
			{
				hayDatos = true,
				cantidad,
				total = api.TotalDeCompras(),
				desdeCuando = rango is null ? "" : rango.PrimerMes,
				banners = LaPortadaDeLaCara.Banners(
					cambio, precios, api.Promociones(ahora, dicho), api.Firma(dicho), ritmos,
					hayCompras: cantidad > 0, alDia: ahora, idioma: dicho),
				cambios = ritmos.Count,

				// «TU HABITUAL»: el mismo hecho que ya protagoniza un slide, pero suelto, porque la
				// app lo repite abajo como mini-afiche con el ahorro en plata. Es UNA lectura del
				// dominio usada dos veces, no dos lecturas que podrian desincronizarse.
				habitual = habitual is null ? null : new
				{
					descripcion = habitual.Descripcion,
					antes = habitual.Antes,
					ahora = habitual.Ahora,
					ahorro = habitual.Antes - habitual.Ahora,
					foto = Fotos.De(habitual.Sku),
				},

				// «EN QUE SE LLENA TU CARRITO»: la canasta del ultimo mes con sus departamentos ya
				// ordenados del que mas pesa al que menos -- el orden lo da el dominio.
				canasta = ElUltimoMes(api, rango, dicho),

				// «UN CAMBIO EN TU RUTINA»: el primero, que ya viene ordenado del dominio, con su
				// foto y su «cambio desde enero». El resto se abre detras del enlace.
				rutina = ritmos.Count == 0 ? null : new
				{
					descripcion = ritmos[0].Descripcion,
					antes = ritmos[0].Antes,
					ahora = ritmos[0].Ahora,
					masSeguido = ritmos[0].EsMasSeguido,
					foto = Fotos.De(ritmos[0].Sku),
					desde = cambio is null ? "" : cambio.Corte,
				},
			};
		});

		// EL ULTIMO MES CON COMPRAS, y cual es lo decide el RANGO -- que es del dominio -- y no esta
		// clase mirando la ultima fila de una tabla. Nulo cuando todavia no hay ningun mes cerrado.
		private static object ElUltimoMes(ComprasAPI api, RangoDelHistorialVista rango, string dicho)
		{
			if (rango is null || rango.MesesConCompras == 0) return null;

			string[] partes = rango.UltimoMes.Split('-');
			if (partes.Length < 2) return null;

			return api.Canasta(int.Parse(partes[0]), int.Parse(partes[1]), dicho);
		}

		// LO DE ARRIBA: cuanto lleva gastado, desde cuando, y la firma de la semana -- a que hora
		// y que dia hace el super, que es lo que hace que la pantalla la reconozca a ella.
		[View("cabecera")]
		public object Cabecera(string idioma = "") => !HasJournal ? NoData() : Read(api => (object)new
		{
			hayDatos = true,
			cantidad = api.CantidadDeCompras(),
			total = api.TotalDeCompras(),
			rango = api.Rango(),
			firma = api.Firma(Elegido(idioma)),
			ultimoEvento = api.UltimoEventoProcesado(),
		});

		// LA CANASTA DEL ULTIMO MES, con sus dos comparaciones. Van juntas porque la canasta sola
		// no dice nada: «₡92 400» es un numero; «₡92 400, 8% mas que el mes pasado» es una
		// lectura. El mes lo elige el DOMINIO -- el ultimo con compras, que sale de Rango().
		[View("canasta")]
		public object Canasta(string idioma = "") => !HasJournal ? NoData() : Read(api =>
		{
			string dicho = Elegido(idioma);
			RangoDelHistorialVista rango = api.Rango();
			if (rango.MesesConCompras == 0) return (object)new { hayDatos = false };

			// El rango entrega el mes como `aaaa-mm`, que es la forma en que viaja: se parte aca,
			// igual que en la app suelta.
			string[] partes = rango.UltimoMes.Split('-');
			if (partes.Length < 2) return (object)new { hayDatos = false };

			return (object)new
			{
				hayDatos = true,
				mes = api.Canasta(int.Parse(partes[0]), int.Parse(partes[1]), dicho),
				contraElMesAnterior = api.ComparacionConElMesAnterior(dicho),
				contraElAnioPasado = api.CambioDelQuiebre(dicho),
			};
		});

		// LO QUE SE MOVIO: los precios que cambiaron y los ritmos que se rompieron. Es la vista
		// que no tiene equivalente en una pantalla de supermercado -- un super entrega facturas,
		// no cambios de precio.
		[View("movimientos")]
		public object Movimientos(string idioma = "") => !HasJournal ? NoData() : Read(api => (object)new
		{
			precios = api.Precios(Elegido(idioma)),
			ritmos = api.RitmosDelQuiebre(Elegido(idioma)),
			ritmo = api.Ritmo(Elegido(idioma)),
			recorrido = api.Recorrido(Elegido(idioma)),
		});

		// LO QUE VIENE: la lista de la proxima compra, las promociones vigentes, lo que falta
		// reponer y las garantias que todavia valen.
		//
		// `DateTime.Now` lo pone esta superficie y no la cara, y no es un detalle: las tres
		// dependen de CONTRA QUE FECHA se preguntan, y dejar que la pagina la mande seria dejar
		// que la pantalla mueva el resultado.
		[View("proxima")]
		public object Proxima(string idioma = "") => !HasJournal ? NoData() : Read(api =>
		{
			string dicho = Elegido(idioma);
			DateTime ahora = DateTime.Now;
			return (object)new
			{
				lista = api.Lista(ahora, dicho),
				promociones = api.Promociones(ahora, dicho),
				abastecimientos = api.AbastecimientosPendientes(ahora, dicho),
				garantias = api.GarantiasVigentes(ahora, dicho),
			};
		});

		// La lista: la misma que ya contesta la tool, pero por la puerta de la cara.
		[View("compras")]
		public object Compras() => !HasJournal ? NoData() : Read(ConsultasDelModulo.HacerListaDeCompras);

		// ------------------------------------------------------------ lo que el cartero deja

		// Gnothi hizo la red -con SU credencial, contra el endpoint que EL fijo al instalar- y
		// entrega un sobre ya leido. Esta superficie lo asienta.
		//
		// ES EL MISMO ARMADO QUE HACE LA APP SUELTA: la misma ruta, la misma guarda Check y el
		// mismo actor, y de ahi sale gratis que entregar dos veces el mismo sobre no duplique
		// nada. Las dos diferencias con EstadoDeLaApp son que aca el sobre YA llego -no hay a
		// quien pedirselo- y que no hay flush: el journal de esta forma vive en la carpeta que
		// Gnothi concedio, no en un MEMFS que haya que bajar a disco.
		[Deliver]
		public string Entregar(string sobreJson)
		{
			ArgumentNullException.ThrowIfNull(sobreJson);

			var unaVez = new BucleDeRefresh(
				new ComprasAPI(app.Value),
				new FuenteDeEventosPorGnothi((_, _) => Task.FromResult(sobreJson), "gnothi.deliver"),
				RutaDeCompras.Rutear);

			ResultadoDeRefresh resultado = unaVez.Ejecutar().GetAwaiter().GetResult();
			return resultado.ToString();
		}

		// HASTA DONDE YA LEYO, para que el cartero pida de ahi en adelante y no el historial entero.
		// Sale del MISMO actor que asienta, que es lo que lo hace confiable: es el cursor real del
		// journal, no una cuenta que lleve la pantalla.
		//
		// Cero cuando todavia no hay journal, y cero significa «traeme todo»: la primera vez es
		// exactamente lo que hay que hacer.
		[Cursor]
		public long HastaDondeLei() => !HasJournal ? 0L : Read(api => api.UltimoEventoProcesado());

		private object NoData() => ConsultasDelModulo.SinDatos(folder);

		// ------------------------------------------------------------ el actor

		// UN SOLO ACTOR, VIVO, Y ESE ES EL CAMBIO QUE IMPORTA. Medido en Examenes el 2026-09-11 y
		// traido tal cual, porque el problema no era del laboratorio sino de la forma.
		//
		// Antes habia dos -un `ReadOnly` con OnHydrated vacio por consulta, y ninguno para
		// escribir- y ademas se REHIDRATABA en cada pregunta. Esa decision venia con su razon
		// escrita, y era cierta en A2/A3: la app suelta de la institucion escribe la carpeta
		// mientras Gnothi lee. En la forma HIBRIDA no hay app externa -- Gnothi es el unico dueno
		// del journal, que era el punto entero de la forma.
		//
		// Y DOS ACTORES SOBRE UN JOURNAL ERA EL PROBLEMA, no la solucion: el lector abria mientras
		// el escritor todavia cerraba su operacion atomica, y eso se veia como
		// `FileNotFoundException: Temp file not found`.
		//
		// LO QUE EL SPLIT COMPRABA ERA MENOS DE LO QUE PARECIA: `ReadOnly` y `ComprasApp` son el
		// mismo PerformanceV2 sobre el mismo dominio, asi que «preguntar nunca escribe» nunca fue
		// estructural, era disciplina en los dos casos. Lo unico estructural era no emitir la
		// semilla -- Y LA SEMILLA HACE FALTA, porque es lo que CREA los globales que el DSL
		// direcciona. Sin ella el bucle muere al pedir el cursor. La guarda de `upgrade` es
		// una-vez-por-actor, asi que no habia nada que evitar.
		//
		// Y LAS ESCRITURAS NO SON ALCANZABLES DESDE AFUERA, que es lo que de verdad las cierra:
		// [Deliver] lo busca la cascara por su propio marcador, no esta en el manifiesto, no lo ve
		// el asistente, y `consultar` solo llega a [View].
		//
		// UN CAMPO NORMAL: la cascara guarda la instancia, asi que la vida del actor es la vida de
		// este objeto. Esto fue un `static` con candado -- el actor tenia que sobrevivir a una
		// clase que se reconstruia en cada llamada -- y las cinco instituciones copiaron el mismo
		// parche, que es como se supo que el hueco era de la cascara y no de ellas.
		//
		// Perezoso con `Lazy<T>` porque `ActualTools` construye esta clase sin preguntarle nada:
		// listar dos nombres no tiene que rehidratar el journal. Publica una sola vez por omision.
		//
		// Sin candado: dos preguntas simultaneas sobre el mismo actor las ordena el motor, con el
		// ReaderWriterLockSlim de ActorHandler.
		private readonly Lazy<ComprasApp> app;

		private ComprasApp Arrancar()
		{
			var arrancando = new ComprasApp(ActorName);
			arrancando.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			arrancando.Start();
			return arrancando;
		}

		private T Read<T>(Func<ComprasAPI, T> question) => question(new ComprasAPI(app.Value));

		// Y SE CIERRA CUANDO LA CASCARA DESALOJA: el `Unload` del contexto es cooperativo y no
		// completa si el actor dejo referencias vivas. Por IDisposable, que es lo que
		// `InProcessShell.Evict` pregunta.
		public void Dispose()
		{
			if (!app.IsValueCreated) return;

			try { app.Value.Actor.GracefulExit(); } catch { /* already going away */ }
		}
	}
}
