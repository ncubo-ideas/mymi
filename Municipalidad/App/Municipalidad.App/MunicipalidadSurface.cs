using System;
using System.IO;
using System.Threading.Tasks;
using Choreography.Theater;
using Gnothi.Contract;
using Municipalidad.App.Events;
using Municipalidad.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Municipalidad.App
{
	/// <summary>
	/// La superficie con que este modulo viaja como carga de Gnothi: el mismo dominio y las mismas
	/// respuestas, expuestas al Host SIN el SDK del asistente.
	/// <para>
	/// Lleva las etiquetas del contrato de Gnothi en vez de las del SDK, que pesa mas de un megabyte: por
	/// eso el paquete de carga son dos dll pequenos -- este y el dominio -- y el motor, la coreografia y
	/// el contrato se los presta el Host. El Host construye esta clase pasandole la carpeta que le
	/// concede: el modulo no elige la carpeta ni abre el journal por su cuenta.
	/// </para>
	/// <para>
	/// TRES PUBLICOS, TRES MARCADORES, y ninguno entra por la puerta del otro. <c>[Query]</c> es lo que
	/// el ASISTENTE puede preguntar -- dos, gruesas, declaradas en el manifiesto y apagables una por
	/// una --; <c>[View]</c> es lo que la PANTALLA necesita para pintar, no esta en el manifiesto y el
	/// modelo no lo ve nunca; <c>[Deliver]</c> es lo que el CARTERO deja, y es lo unico que escribe.
	/// </para>
	/// <para>
	/// ESTE MODULO NO ES UN TABLERO: es un menu de SEIS PREGUNTAS, cada una su propia pantalla en la
	/// app suelta. Por eso hay una vista por pregunta y no una vista grande: partirlas es la
	/// arquitectura de informacion que la institucion eligio, no una decision de esta clase.
	/// </para>
	/// </summary>
	[ModuleSurface]
	public sealed class MunicipalidadSurface : IDisposable
	{
		private readonly string carpeta;

		public MunicipalidadSurface(string carpetaConcedida)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(carpetaConcedida);

			carpeta = Path.GetFullPath(carpetaConcedida);
			app = new Lazy<MunicipalidadApp>(Arrancar);
		}

		private bool HayJournal => MunicipalidadApp.HayJournalEn(carpeta);

		/// <summary>
		/// EN QUE IDIOMA CONTESTA CADA VISTA, y lo dice QUIEN PREGUNTA: la carátula manda su etiqueta
		/// como un parametro mas de la vista, igual que la app suelta se la manda a la puerta.
		/// <para>
		/// Esta clase no se lo pregunta al dispositivo, y esa sigue siendo la decision: corre en
		/// proceso adentro de Gnothi, donde no hay <c>navigator</c>, y preguntarselo desde aca ataria
		/// el paquete de carga al navegador, que es lo que la forma hibrida existe para no hacer. La
		/// carátula SI puede -- es una pagina --, asi que negocia ella y lo dice al preguntar.
		/// </para>
		/// <para>
		/// TOTAL: una etiqueta vacia o que esta app no habla cae al idioma del registro, que es la
		/// misma caida que hace la app suelta cuando no puede leer la eleccion. Una carátula que no
		/// mande nada sigue recibiendo una respuesta, en espanol, y no un rechazo.
		/// </para>
		/// </summary>
		private static string Elegido(string idioma) =>
			Frases.Frases.EsUnaEtiquetaConocida(idioma) ? idioma : Frases.Frases.IdiomaDelRegistro;

		// ------------------------------------------------------------ lo que contesta al asistente

		[Query(ConsultasDelModulo.Resumen)]
		public object Resumen() =>
			Leer(api => ConsultasDelModulo.HacerResumen(api, ElInstanteEnQueSePregunto()));

		/// <summary>Lo que se vence y lo que ya vencio, en filas planas.</summary>
		[Query(ConsultasDelModulo.ListaDeVencimientos)]
		public object ListaDeVencimientos() =>
			Leer(api => ConsultasDelModulo.HacerListaDeVencimientos(api, ElInstanteEnQueSePregunto()));

		// ------------------------------------------------------------ lo que pinta la cara
		//
		// UNA VISTA POR PREGUNTA, con el nombre de la pregunta. No es una traduccion floja de los
		// nombres del dominio: es que la app suelta tiene seis pantallas y cada una contesta UNA cosa,
		// y esa reparticion es lo que la institucion decidio que es su app.

		/// <summary>Las fincas que tiene, para poder elegir de cual se habla.</summary>
		[View("fincas")]
		public object Fincas() => Leer(api => (object)new
		{
			hayDatos = true,
			fincas = LasFincasQueTengo.LeerPor(api, ElInstanteEnQueSePregunto()),
		});

		/// <summary>Que cambio entre los dos ultimos comprobantes de cada rubro, y que lo explica.</summary>
		[View("por-que-pago-mas")]
		public object PorQuePagoMas(string idioma = "") => Leer(api => (object)new
		{
			variacion = LaVariacionDeCadaRubro.LeerPor(api, Elegido(idioma)),
		});

		/// <summary>Lo que esta por vencerse y lo que ya vencio.</summary>
		[View("que-se-me-vence")]
		public object QueSeMeVence(string idioma = "") => Leer(api =>
		{
			DateTime alDia = ElInstanteEnQueSePregunto();
			return (object)new { vence = LoQueSeVence.LeerPor(api, alDia, ElDiaDe(alDia), Elegido(idioma)) };
		});

		/// <summary>Lo que esta app propone apartar por mes, y lo que la persona adopto.</summary>
		[View("cuanto-apartar")]
		public object CuantoApartar(string idioma = "") => Leer(api =>
		{
			DateTime alDia = ElInstanteEnQueSePregunto();
			return (object)new
			{
				reserva = LaReservaDeEsteAnio.LeerPor(api, alDia, ElDiaDe(alDia), Elegido(idioma)),
			};
		});

		/// <summary>Lo que el portal da por pagado y aca no tiene recibo.</summary>
		[View("sin-comprobante")]
		public object SinComprobante(string idioma = "") => Leer(api => (object)new
		{
			sinComprobante = LoSinComprobante.LeerPor(api, Elegido(idioma)),
		});

		/// <summary>
		/// El total de un anio con la mora adentro. El anio lo elige la cara -- es la unica vista con
		/// un parametro de verdad -- y <c>loEligioLaPersona</c> dice si fue una eleccion o el ano por
		/// omision, porque el dominio lo redacta distinto.
		/// </summary>
		[View("cuanto-he-pagado")]
		public object CuantoHePagado(int anio, string idioma = "") => Leer(api =>
		{
			bool loEligio = anio > 0;
			int elAnio = loEligio ? anio : ElInstanteEnQueSePregunto().Year;

			return (object)new
			{
				pagado = LoQuePagueEnUnAnio.LeerPor(api, elAnio, loEligio, Elegido(idioma)),
			};
		});

		/// <summary>
		/// Que entro de lo que el portal publica, y que falta. SIN idioma, y no es un olvido: esta
		/// vista no trae ni una frase redactada -- son cuentas, dias y banderas --, asi que un
		/// parametro de idioma aca no tendria a quien pasarselo. La carátula lo manda igual y el Host
		/// lo ignora, que es lo que hace con todo argumento que la vista no declara.
		/// </summary>
		[View("que-tan-al-dia")]
		public object QueTanAlDia() => Leer(api =>
		{
			DateTime alDia = ElInstanteEnQueSePregunto();
			return (object)new { alDia = LoQueEntroYLoQueFalta.LeerPor(api, alDia, ElDiaDe(alDia)) };
		});

		// ------------------------------------------------------------ lo que el cartero deja

		/// <summary>
		/// Gnothi hizo la red -- con SU credencial, contra el endpoint que EL fijo al instalar -- y
		/// entrega un sobre ya leido. Esta superficie lo asienta.
		/// <para>
		/// Es el mismo armado que hace la app suelta: la misma ruta, la misma guarda Check y el mismo
		/// actor, y de ahi sale gratis que entregar dos veces el mismo sobre no duplique nada. Las dos
		/// diferencias son que aca el sobre YA llego -- no hay a quien pedirselo -- y que no hay flush:
		/// el journal de esta forma vive en la carpeta que Gnothi concedio, no en un MEMFS.
		/// </para>
		/// </summary>
		[Deliver]
		public string Entregar(string sobreJson)
		{
			ArgumentNullException.ThrowIfNull(sobreJson);

			var unaVez = new BucleDeRefresh(
				new MunicipalidadAPI(app.Value),
				new FuenteDeEventosPorGnothi((_, _) => Task.FromResult(sobreJson), "gnothi.deliver"),
				MunicipalidadEventRoute.Route);

			ResultadoDeRefresh resultado = unaVez.Ejecutar().GetAwaiter().GetResult();
			return resultado.ToString();
		}

		/// <summary>
		/// Hasta donde ya leyo, para que el cartero pida de ahi en adelante y no el historial entero.
		/// Sale del MISMO actor que asienta: es el cursor real del journal, no una cuenta que lleve la
		/// pantalla. Cero cuando todavia no hay journal, y cero significa «traeme todo».
		/// </summary>
		[Cursor]
		public long HastaDondeLei() =>
			!HayJournal ? 0L : new MunicipalidadAPI(app.Value).UltimoEventoAplicado();

		// ------------------------------------------------------------ el actor

		/// <summary>
		/// UN SOLO ACTOR, VIVO, compartido por las tres puertas. Medido en Examenes el 2026-09-11 y
		/// traido tal cual, porque el problema no era del laboratorio sino de la forma.
		/// <para>
		/// Antes habia un <c>AppDeMunicipalidadSoloLectura</c> que se REHIDRATABA en cada consulta, y
		/// su razon estaba escrita: «la app instalada escribe en esta carpeta mientras este proceso
		/// vive». Es cierta en A2/A3; en la forma HIBRIDA no hay app externa -- Gnothi es el unico
		/// dueno del journal, que era el punto entero de la forma.
		/// </para>
		/// <para>
		/// Y DOS ACTORES SOBRE UN JOURNAL ERA EL PROBLEMA, no la solucion: el lector abria mientras el
		/// escritor todavia cerraba su operacion atomica, y eso se veia como
		/// <c>FileNotFoundException: Temp file not found</c>.
		/// </para>
		/// <para>
		/// LA SEMILLA HACE FALTA. Un anfitrion que no siembra deja al DSL sin sus globales, y el bucle
		/// muere al pedir el cursor. La guarda del upgrade es una-vez-por-actor, asi que no habia nada
		/// que evitar. Y las escrituras no son alcanzables desde afuera: <c>[Deliver]</c> lo busca la
		/// cascara por su propio marcador, no esta en el manifiesto, y <c>consultar</c> solo llega a
		/// <c>[View]</c>.
		/// </para>
		/// <para>
		/// UN CAMPO NORMAL, y antes era <c>static</c> con candado. Lo era por una razon que no
		/// era de esta institucion: la cascara reconstruia esta clase en cada llamada, asi que
		/// el actor -- arrancarlo es rehidratar el journal entero -- estaba obligado a
		/// sobrevivir a la instancia. Las CINCO instituciones copiaron el mismo parche, que es
		/// como se supo que el hueco era de la cascara. Ahora <c>InProcessShell</c> guarda la
		/// instancia y la vida del actor es la vida de este objeto.
		/// </para>
		/// <para>
		/// PEREZOSO CON <c>Lazy&lt;T&gt;</c>: <c>ActualTools</c> -- la comprobacion de modulos,
		/// al instalar y al arrancar -- construye esta clase sin preguntarle nada, y listar dos
		/// nombres no tiene que rehidratar el journal. Publica una sola vez por omision, asi que
		/// es una linea donde habia cuatro campos y un doble chequeo.
		/// </para>
		/// <para>
		/// Y SIN CANDADO, porque no hace falta: dos preguntas simultaneas sobre el mismo actor
		/// las ordena el motor, con el <c>ReaderWriterLockSlim</c> de <c>ActorHandler</c> --
		/// <c>PerformQry</c> toma el de lectura, <c>PerformCmd</c> el de escritura.
		/// </para>
		/// </summary>
		private readonly Lazy<MunicipalidadApp> app;

		private MunicipalidadApp Arrancar()
		{
			var arrancando = new MunicipalidadApp(MunicipalidadApp.ElActor);
			arrancando.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			arrancando.Start();
			return arrancando;
		}

		/// <summary>
		/// SE CIERRA CUANDO LA CASCARA DESALOJA. El <c>Unload</c> del contexto es cooperativo y
		/// no completa si el actor dejo referencias vivas; por <c>IDisposable</c>, que es lo que
		/// <c>InProcessShell.Evict</c> pregunta, y no por una etiqueta nueva de Gnothi.
		/// </summary>
		public void Dispose()
		{
			if (!app.IsValueCreated) return;

			try { app.Value.Actor.GracefulExit(); } catch { /* ya se estaba yendo */ }
		}

		private object Leer(Func<MunicipalidadAPI, object> pregunta)
		{
			if (!HayJournal) return ConsultasDelModulo.SinDatos(carpeta);

			MunicipalidadApp abierto = app.Value;
			if (abierto.Actor.CurrentEntryId == 0) return ConsultasDelModulo.SinDatos(carpeta);

			return pregunta(new MunicipalidadAPI(abierto));
		}

		/// <summary>
		/// El instante en que se pregunto, al segundo entero. No <c>UtcNow</c> pelado: la precision de
		/// sub-segundo hace que dos lecturas tomadas en el mismo aliento no coincidan, y todas estas
		/// vistas se calculan CONTRA un instante.
		/// </summary>
		private static DateTime ElInstanteEnQueSePregunto()
		{
			DateTime ahora = DateTime.UtcNow;

			return new DateTime(ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}

		/// <summary>Ese instante dicho como el dia de calendario en que cae.</summary>
		private static string ElDiaDe(DateTime instante) =>
			instante.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
	}
}
