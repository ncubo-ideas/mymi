using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cargas.App.Eventos;
using Cargas.App.Vistas;
using Choreography.Theater;
using Gnothi.Contract;
using Puppeteer;
using Puppets.Portador;

namespace Cargas.App
{
	// LA SUPERFICIE DE CARGAS, para las formas cargo e hibrida. El mismo dominio, expuesto al
	// Host SIN el SDK de MCP: lleva [ModuleSurface]/[Query] de Gnothi.Contract -una etiqueta sin
	// dependencias que el Host presta- en vez de [McpServerTool] (~1,3 MB). Eso es lo que deja
	// que el modulo viaje como dos dll chicos y pida prestados Puppeteer, Choreography y el
	// contrato.
	//
	// TRES PUBLICOS, TRES MARCADORES, y ninguno entra por la puerta del otro:
	//
	//   [Query]    lo que el ASISTENTE puede preguntar -- dos, gruesas, en el manifiesto, y la
	//              persona las apaga una por una
	//   [View]     lo que la PANTALLA necesita para pintar -- no estan en el manifiesto y el
	//              modelo no las ve nunca
	//   [Deliver]  lo que el CARTERO deja -- tampoco esta en el manifiesto, y es lo unico que
	//              escribe
	//
	// Es la misma reparticion que ExamenesSurface, y la razon es la misma: una pantalla no puede
	// pintar un tablero con un resumen, y al asistente no le sirven los tramos de una chispa.
	//
	// UN SOLO ACTOR, VIVO, compartido por los tres. Ver `app` al final: el split de lector y
	// escritor compraba menos de lo que parecia y costaba dos actores sobre un journal.
	//
	// El Host construye esto pasandole la carpeta concedida al constructor de string -- la
	// convencion de InProcessShell. El modulo nunca abre el journal por su cuenta ni elige la
	// carpeta.
	[ModuleSurface]
	public sealed class CargasSurface : IDisposable
	{
		private const string ActorName = "cargas";

		private readonly string folder;

		public CargasSurface(string grantedFolder)
		{
			if (string.IsNullOrWhiteSpace(grantedFolder))
				throw new ArgumentException("the granted folder is required", nameof(grantedFolder));
			folder = Path.GetFullPath(grantedFolder);
			app = new Lazy<CargasApp>(Arrancar);
		}

		private bool HasJournal => Directory.Exists(Path.Combine(folder, ActorName, "journal"));

		// ------------------------------------------------------------ lo que contesta al asistente

		[Query(ConsultasDelModulo.Resumen)]
		public object Resumen()
		{
			if (!HasJournal) return NoData();
			return Read(ConsultasDelModulo.HacerResumen);
		}

		[Query(ConsultasDelModulo.ListaDeVehiculos)]
		public object ListaDeVehiculos()
		{
			if (!HasJournal) return NoData();
			return Read(ConsultasDelModulo.HacerListaDeVehiculos);
		}

		// ------------------------------------------------------------ lo que pinta la cara

		// LA CABECERA: los vehiculos y las unidades con las que ella mira todo. La placa la elige
		// la cara, y todas las vistas de abajo la piden -- el dominio reparte por vehiculo y
		// quedarse con el primero escondia los demas sin decirlo.
		[View("cabecera")]
		public object Cabecera() => !HasJournal ? NoData() : Read(api => (object)new
		{
			hayDatos = true,
			vehiculos = api.Vehiculos(),
			unidades = api.Unidades(),
			cargas = api.CantidadDeCargas(),
			ultimoEvento = api.UltimoEventoProcesado(),
		});

		// EL TABLERO DE UN VEHICULO, de una sola vez. Son seis lecturas del dominio y la pantalla
		// las dibuja juntas: partirlas en seis preguntas solo agrega saltos del puente.
		//
		// `hoy` lo pone esta superficie y no la cara, y no es un detalle: la autonomia y la
		// proxima carga se calculan CONTRA UNA FECHA, y dejar que la pagina la mande seria dejar
		// que la pantalla mueva el resultado.
		[View("tablero")]
		public object Tablero(string placa)
		{
			if (string.IsNullOrWhiteSpace(placa)) throw new ArgumentException("falta la placa", nameof(placa));
			if (!HasJournal) return NoData();

			return Read(api =>
			{
				DateTime hoy = DateTime.Now.Date;
				return (object)new
				{
					placa,
					rendimiento = api.Rendimiento(placa),
					autonomia = api.Autonomia(placa),
					proxima = api.ProximaCarga(placa, hoy),
					tramos = api.Tramos(placa),
					costos = api.Costos(placa),
					odometro = api.Odometro(placa, hoy),
				};
			});
		}

		// LO QUE FALTA PARA PODER CALCULAR. El tablero apagado no explica por que esta apagado, y
		// el dominio sabe la respuesta: le falta un tramo cerrado entre dos tanques llenos, o el
		// kilometraje anotado. Es lo que convierte un guion en una instruccion.
		[View("faltantes")]
		public object Faltantes(string placa)
		{
			if (string.IsNullOrWhiteSpace(placa)) throw new ArgumentException("falta la placa", nameof(placa));
			if (!HasJournal) return NoData();

			return Read(api => (object)new
			{
				placa,
				faltantes = api.Faltantes(placa),
				completitud = api.Completitud(placa),
			});
		}

		// LA LISTA DE CARGAS de un vehiculo: el historial, que es lo que el tablero no da.
		[View("cargas")]
		public object Cargas(string placa)
		{
			if (string.IsNullOrWhiteSpace(placa)) throw new ArgumentException("falta la placa", nameof(placa));
			if (!HasJournal) return NoData();

			return Read(api => (object)new { placa, cargas = api.Cargas(placa) });
		}

		// ------------------------------------------------------------ lo que el cartero deja

		// Gnothi hizo la red -con SU credencial, contra el endpoint que EL fijo al instalar- y
		// entrega un sobre ya leido. Esta superficie lo asienta.
		//
		// ES EL MISMO ARMADO QUE HACE LA APP SUELTA: la misma ruta, la misma guarda Check y el
		// mismo actor, y de ahi sale gratis que entregar dos veces el mismo sobre no duplique
		// nada. Las dos diferencias con EstadoDeLaApp son que aca el sobre YA llego -no hay a
		// quien pedirselo- y que no hay flush: el journal de esta forma vive en la carpeta que
		// Gnothi concedio, no en un MEMFS que haya que bajar a disco.
		//
		// EL CURSOR NO VIAJA EN EL SOBRE y no hace falta: lo pone el actor, saliendo del journal.
		[Deliver]
		public string Entregar(string sobreJson)
		{
			ArgumentNullException.ThrowIfNull(sobreJson);

			var unaVez = new BucleDeRefresh(
				new CargasAPI(app.Value),
				new FuenteDeEventosPorGnothi((_, _) => Task.FromResult(sobreJson), "gnothi.deliver"),
				RutaDeCargas.Rutear);

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

		// UN SOLO ACTOR, VIVO. Medido en Examenes el 2026-09-11 y traido tal cual, porque el
		// problema no era del laboratorio sino de la forma.
		//
		// Antes de eso habia dos -uno de solo lectura por consulta, otro escribible para la
		// entrega- y ademas se REHIDRATABA en cada pregunta. Esa decision venia con su razon
		// escrita: la app suelta de la institucion escribe la carpeta mientras Gnothi lee. Es
		// cierta en A2/A3; en la forma HIBRIDA no hay app externa -- Gnothi es el unico dueno del
		// journal, que era el punto entero de la forma.
		//
		// Y DOS ACTORES SOBRE UN JOURNAL ERA EL PROBLEMA, no la solucion: el lector abria
		// mientras el escritor todavia cerraba su operacion atomica, y eso se veia como
		// `FileNotFoundException: Temp file not found`.
		//
		// LA SEMILLA HACE FALTA. Un actor con OnHydrated vacio -puesto para no sembrar en cada
		// entrega- deja al DSL sin sus globales, y el bucle muere al pedir el cursor. La guarda
		// de upgrade es una-vez-por-actor, asi que no habia nada que evitar.
		//
		// Y LAS ESCRITURAS NO SON ALCANZABLES DESDE AFUERA, que es lo que de verdad las cierra:
		// [Deliver] lo busca la cascara por su propio marcador, no esta en el manifiesto, no lo
		// ve el asistente, y `consultar` solo llega a [View].
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
		private readonly Lazy<CargasApp> app;

		private CargasApp Arrancar()
		{
			var arrancando = new CargasApp(ActorName);
			arrancando.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			arrancando.Start();
			return arrancando;
		}

		private T Read<T>(Func<CargasAPI, T> question) => question(new CargasAPI(app.Value));

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
