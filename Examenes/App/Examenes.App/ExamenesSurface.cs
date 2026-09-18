using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Choreography.Theater;
using Examenes.App.Eventos;
using Examenes.App.Vistas;
using Gnothi.Contract;
using Puppeteer;
using Puppets.Portador;

namespace Examenes.App
{
	// THE CARGO SURFACE OF EXAMENES. The same domain, exposed to the Host WITHOUT the MCP SDK.
	//
	// It wears [ModuleSurface]/[Query] from Gnothi.Contract -a tiny, zero-dependency label the
	// Host lends- instead of [McpServerTool] from the MCP SDK (~1.3 MB). That is what lets the
	// module ship as two small DLLs (this + Examenes.Dominio) and borrow Puppeteer, Choreography
	// and this contract from the Host. It is the reference model's SuperficieDeFacturas, here.
	//
	// ANSWERING NEVER WRITES, and since 2026-09-11 that is a property of the METHODS, not of a
	// second actor: every [Query] and [View] goes through ExamenesAPI's PerformQuery path, and
	// the only thing that writes is [Deliver].
	//
	// It used to keep a separate read-only actor -OnHydrated empty- and rehydrate it on every
	// question. That bought less than it looked: both are the same PerformanceV2 over the same
	// domain, so "a query cannot write" was discipline either way. The only structural part was
	// not emitting the seed -- and the seed turned out to be REQUIRED, because it creates the
	// globals the DSL addresses. See `app`.
	//
	// What really closes the writes is the doors: [Deliver] is found by the shell through its own
	// marker, is not in the manifest, the assistant never sees it, and `consultar` only reaches
	// [View].
	//
	// The Host builds this by passing the granted folder to the string constructor -the
	// InProcessShell convention. The module never opens the journal on its own nor chooses the
	// folder.
	[ModuleSurface]
	public sealed class ExamenesSurface : IDisposable
	{
		private const string ActorName = "examenes";

		private readonly string folder;

		public ExamenesSurface(string grantedFolder)
		{
			if (string.IsNullOrWhiteSpace(grantedFolder))
				throw new ArgumentException("the granted folder is required", nameof(grantedFolder));
			folder = Path.GetFullPath(grantedFolder);
			app = new Lazy<ExamenesApp>(Arrancar);
		}

		private bool HasJournal => Directory.Exists(Path.Combine(folder, ActorName, "journal"));

		// A SUMMARY LINE. The smallest useful block, and the one a screen shows at the top.
		[Query(ConsultasDelModulo.Resumen)]
		public object Resumen()
		{
			if (!HasJournal) return NoData();
			return Read(ConsultasDelModulo.HacerResumen);
		}

		// THE LIST OF EXAMS. An array of flat rows -exactly what the window renders as a table.
		[Query(ConsultasDelModulo.ListaDeExamenes)]
		public object ListaDeExamenes()
		{
			if (!HasJournal) return NoData();
			return Read(ConsultasDelModulo.HacerListaDeExamenes);
		}

		// ------------------------------------------------------------------ lo que pinta la cara
		//
		// ESTAS NO SON TOOLS, y la diferencia es el punto entero: [Query] es lo que el ASISTENTE
		// puede preguntar -dos, gruesas, declaradas en el manifiesto y apagables una por una-,
		// mientras que una PANTALLA necesita series, puntos y lineas de referencia. El dominio
		// tiene 21 lecturas; declararlas todas como tools haria que el alta reportara 19
		// 'undeclared' contra un manifiesto que nombra dos.
		//
		// Todas usan el MISMO actor que las tools y que la entrega: uno solo, vivo. Pintar tampoco
		// escribe, porque todas pasan por el camino de PerformQuery de ExamenesAPI.

		// El bloque de arriba de la pantalla, y de paso lo que el modulo sabe de la persona.
		[View("cabecera")]
		public object Cabecera() => !HasJournal ? NoData() : Read(api => (object)new
		{
			hayDatos = true,
			cantidad = api.CantidadDeExamenes(),
			fueraDeRango = api.AnalitosFueraDeRango(),
			edad = api.EdadDelPaciente(),
			sexo = api.SexoDelPaciente(),
			sexoDeclarado = api.SexoDeclaradoPorLaPersona(),
			sexoPublicado = api.SexoPublicadoPorLaInstitucion(),
			ultimoEvento = api.UltimoEventoProcesado(),
		});

		// La tabla: la misma que ya contesta la tool, pero por la puerta de la cara.
		[View("examenes")]
		public object Examenes() => !HasJournal ? NoData() : Read(ConsultasDelModulo.HacerListaDeExamenes);

		// LO QUE UN GRAFICO NECESITA Y EL CUADRO NO DA: los puntos de una serie con su numero, y
		// las lineas que los acompanan. Es exactamente lo que la tool no puede contestar.
		[View("serie")]
		public object Serie(string analito)
		{
			if (string.IsNullOrWhiteSpace(analito)) throw new ArgumentException("falta el analito", nameof(analito));
			if (!HasJournal) return NoData();

			return Read(api => (object)new
			{
				analito,
				puntos = api.Puntos(analito),
				referencias = api.Referencias(analito),
			});
		}

		// LOS ANALITOS DE LOS QUE HAY HISTORIA. Es lo que la pantalla recorre para mostrar una
		// lectura por serie sin conocer las clases del dominio -- exactamente lo mismo que hace
		// la app suelta antes de dibujar sus graficos.
		[View("analitos")]
		public object Analitos() => !HasJournal ? NoData() : Read(api => (object)new
		{
			hayDatos = true,
			analitos = api.Analitos(),
		});

		// LA LECTURA DE LA SERIE, que es la vista que NO tiene equivalente en una pantalla de
		// resultados: un laboratorio entrega valores, no tendencias.
		//
		// Y SE PREGUNTA POR LA FORMA ANTES QUE POR LOS HECHOS, que es lo mismo que hace la app
		// suelta antes de dibujar. Un binario -sangre en la orina- no tiene tendencia ni puntos
		// con numero: pedirselos devuelve ceros, y la pantalla los dibujaba como una serie plana
		// en cero. Cual puerta usar lo decide el dominio; la cara lee la forma, no la inventa.
		[View("tendencia")]
		public object Tendencia(string analito)
		{
			if (string.IsNullOrWhiteSpace(analito)) throw new ArgumentException("falta el analito", nameof(analito));
			if (!HasJournal) return NoData();

			return Read(api =>
			{
				InterpretacionDeAnalito lectura = api.Interpretacion(analito);

				// UNA SOLA VUELTA POR ANALITO. La pantalla dibuja la tarjeta entera de un saque
				// -- cabecera, chip, relato, valor, referencia, forma y lectura -- y partirlo en
				// varias preguntas solo agrega saltos del puente sin agregar nada.
				if (lectura.Forma == FormaDePresencia)
					return (object)new
					{
						analito,
						lectura,
						presencia = api.Presencia(analito),
						pruebas = api.Pruebas(analito),
					};

				if (lectura.Forma == FormaDeCategoria)
					return (object)new
					{
						analito,
						lectura,
						constancia = api.Constancia(analito),
						observaciones = api.Observaciones(analito),
					};

				return (object)new
				{
					analito,
					lectura,
					hechos = api.Tendencia(analito),
					puntos = api.Puntos(analito),
					referencias = api.Referencias(analito),
				};
			});
		}

		// Los nombres de las dos formas que NO son una serie continua. Son del dominio, y estan
		// escritos igual en la app suelta: quien los cambie tiene que cambiar los dos lados.
		private const string FormaDePresencia = "presencia en el tiempo";
		private const string FormaDeCategoria = "categoria en el tiempo";

		// EL HISTORIAL COMO CRONOLOGIA DE VISITAS, que es como se recuerda una ida al
		// laboratorio: varios analitos el mismo dia, no una grilla. El dominio manda las celdas
		// ya colapsadas por dia; agruparlas es desarmado, no decision, y lo hace la cara.
		[View("historial")]
		public object Historial() => !HasJournal ? NoData() : Read(api => (object)new
		{
			hayDatos = true,
			celdas = api.Cuadro(),
		});

		// Lo que exige atencion, lo que esta bien, y el dato que el modulo le pide a la persona.
		//
		// `piden` son las series que piden LO MISMO que el expediente entero. Es una igualdad y
		// no un orden: ordenar severidades es del dominio, y la mayor ya vino resuelta en
		// `urgente`. La portada las nombra para que «consultalo con tu medico» diga CUALES.
		[View("alertas")]
		public object Alertas() => !HasJournal ? NoData() : Read(api =>
		{
			LoMasUrgente urgente = api.Urgente();

			var piden = new List<InterpretacionDeAnalito>();
			if (urgente.PideAlgo)
				foreach (string analito in api.Analitos())
				{
					InterpretacionDeAnalito lectura = api.Interpretacion(analito);
					if (lectura.Severidad == urgente.Severidad) piden.Add(lectura);
				}

			return (object)new
			{
				urgente,
				piden,
				enRango = api.LoQueEstaEnRango(),
				faltaElSexo = api.PedidoDeSexo(),
			};
		});

		// LO QUE EL CARTERO DEJA. Gnothi hizo la red -con SU credencial, contra el endpoint que
		// EL fijo al instalar- y entrega un sobre ya leido. Esta superficie lo asienta.
		//
		// ES EL MISMO ARMADO QUE HACE LA APP SUELTA, y eso no es copiar por comodidad: es la
		// misma ruta, la misma guarda Check y el mismo flush, y de ahi sale gratis que entregar
		// dos veces el mismo sobre no duplique nada. La unica diferencia con EstadoDeLaApp es
		// la fuente -aca el sobre ya llego, no hay a quien pedirselo- y que no hay flush: el
		// journal de esta forma YA vive en la carpeta que Gnothi concedio, no en un MEMFS que
		// haya que bajar a disco.
		//
		// EL CURSOR NO VIAJA EN EL SOBRE y no hace falta que viaje: lo pone el actor, saliendo
		// del journal. Quien entrega no tiene que saber por donde iba.
		[Deliver]
		public string Entregar(string sobreJson)
		{
			ArgumentNullException.ThrowIfNull(sobreJson);

			// EL MISMO ACTOR QUE CONTESTA LAS VISTAS. No hay uno para leer y otro para escribir:
			// es uno solo, vivo, y por eso no hay dos abriendo el journal a la vez. Ver `app`.
			//
			// Y ES EL PUPPET DE VERDAD, con su semilla. La semilla no es solo una entrada en el
			// journal: es lo que CREA los globales -`expediente` y `bitacora`- que el DSL
			// direcciona. Con un actor de OnHydrated vacio, la primera cosa que hace el bucle
			// -preguntar el cursor por `bitacora.UltimoEvento`- muere con "Unknown property or
			// method 'UltimoEvento' on type 'Object'". Medido el 2026-09-11 en el telefono, y ya
			// estaba advertido en ExamenesApp: la vista de solo lectura del MCP "rehidrataba y no
			// encontraba el global".
			//
			// No duplica nada: la guarda de `upgrade('semilla')` es una-vez-por-actor.
			var unaVez = new BucleDeRefresh(
				new ExamenesAPI(app.Value),
				new FuenteDeEventosPorGnothi((_, _) => Task.FromResult(sobreJson), "gnothi.deliver"),
				RutaDeExamenes.Rutear);

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

		// UN SOLO ACTOR, VIVO, Y ESE ES EL CAMBIO QUE IMPORTA.
		//
		// Antes habia dos -uno de solo lectura por consulta, otro escribible para la entrega- y
		// ademas se REHIDRATABA en cada pregunta. Esa decision venia con su razon escrita: «the
		// installed app writes to this folder while this process lives, so a cached actor would
		// answer with the state of the first question».
		//
		// Es cierta en A2/A3, donde la app suelta de la institucion escribe la carpeta mientras
		// Gnothi lee. En la forma HIBRIDA no hay app externa: Gnothi es el unico dueno del
		// journal, que era el punto entero de la forma. Nadie mas escribe, asi que no hay nada
		// que volver a leer.
		//
		// Y DOS ACTORES SOBRE UN JOURNAL ERA EL PROBLEMA, no la solucion: el lector abria
		// mientras el escritor todavia cerraba su operacion atomica, y eso se veia como
		// `FileNotFoundException: Temp file not found` (medido en el telefono, 2026-09-11).
		//
		// LO QUE EL SPLIT COMPRABA ERA MENOS DE LO QUE PARECIA. ReadOnly y ExamenesApp son el
		// mismo PerformanceV2 sobre el mismo dominio: «preguntar nunca escribe» nunca fue
		// estructural, era disciplina en los dos casos. Lo unico estructural era no emitir la
		// semilla -- y la semilla hace falta, porque CREA los globales que el DSL direcciona.
		//
		// Y LAS ESCRITURAS NO SON ALCANZABLES DESDE AFUERA, que es lo que de verdad las cierra:
		// [Deliver] lo busca la cascara por su propio marcador, no esta en el manifiesto, no lo
		// ve el asistente, y `consultar` solo llega a [View].
		//
		// UN CAMPO NORMAL, Y ESA ES LA CORRECCION.
		//
		// Esto fue `static ExamenesApp vivo` con su `candado`, su `carpetaDelVivo` y un
		// `GracefulExit` defensivo por si la carpeta cambiaba. Era estatico por una razon que no
		// era de esta institucion: la cascara reconstruia ESTA clase en cada llamada, asi que el
		// actor -- arrancarlo es rehidratar el journal entero -- estaba obligado a sobrevivir a
		// la instancia. Y un estatico perezoso desde varios hilos obliga a un candado.
		//
		// LAS CINCO INSTITUCIONES COPIARON EL MISMO PARCHE, que es como se supo que no era de
		// ninguna: era un hueco de la cascara. Ahora `InProcessShell` guarda la instancia, asi
		// que la vida del actor es la vida de este objeto y no hay nada que compartir entre
		// llamadas. La carpeta tampoco puede cambiar: llega por el constructor.
		//
		// LA PEREZA LA DA `Lazy<T>`, no un doble chequeo escrito a mano. Y hace falta que sea
		// perezoso: `ActualTools` -- la comprobacion de modulos, al instalar y al arrancar --
		// construye esta clase sin preguntarle nada, y rehidratar el journal para listar dos
		// nombres seria pagar de mas. `Lazy<T>` publica una sola vez por omision
		// (ExecutionAndPublication), asi que esto es una linea donde habia cuatro campos.
		//
		// Y NO HAY NINGUN CANDADO MAS PORQUE NO HACE FALTA: dos preguntas simultaneas sobre el
		// mismo actor las ordena el motor, con el ReaderWriterLockSlim de ActorHandler --
		// PerformQry toma el de lectura, PerformCmd el de escritura. Una entrega escribiendo y
		// una vista leyendo no se pisan.
		private readonly Lazy<ExamenesApp> app;

		private ExamenesApp Arrancar()
		{
			var arrancando = new ExamenesApp(ActorName);
			arrancando.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			arrancando.Start();
			return arrancando;
		}

		private T Read<T>(Func<ExamenesAPI, T> question) => question(new ExamenesAPI(app.Value));

		// Y SE CIERRA CUANDO LA CASCARA DESALOJA. No es cortesia: el `Unload` del contexto es
		// COOPERATIVO y no completa si quedaron referencias vivas -- y el actor es justo quien
		// las deja. Sin este cierre, actualizar un modulo abierto dejaba el ensamblado viejo
		// cargado con su actor encima del mismo journal.
		//
		// Por IDisposable y no por una etiqueta de Gnothi: `InProcessShell.Evict` pregunta por la
		// interfaz de la BCL. Una institucion que no tiene nada que cerrar no implementa nada.
		//
		// `IsValueCreated` para no ARRANCAR un actor con tal de apagarlo: si nadie pregunto nada,
		// no hay nada que cerrar.
		public void Dispose()
		{
			if (!app.IsValueCreated) return;

			try { app.Value.Actor.GracefulExit(); } catch { /* already going away */ }
		}


		// NO HAY UNA CLASE "Escribible" ACA, y la hubo: un PerformanceV2 con OnHydrated vacio,
		// para que la entrega no sembrara en cada sobre. Estaba mal por dos razones, y la
		// segunda solo aparecio corriendolo en el telefono:
		//
		//   1. la guarda de `upgrade('semilla')` YA es una-vez-por-actor, asi que no habia nada
		//      que evitar;
		//   2. la semilla no es una entrada de bitacora: es lo que CREA los globales que el DSL
		//      direcciona. Sin ella no hay `bitacora` ni `expediente`, y el bucle muere al
		//      pedir el cursor.
		//
		// La entrega usa ExamenesApp, que es el puppet de verdad. La separacion que importaba
		// -que preguntar nunca escriba- la sigue dando ReadOnly, que es otra clase.
	}
}
