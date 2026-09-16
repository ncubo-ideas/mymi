using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Municipalidad.App;
using Municipalidad.App.Events;
using NUnit.Framework;
using Puppeteer;
using Puppets.Portador;

namespace Municipalidad.Dominio.Pruebas
{
	// NIVEL B: EL BUCLE DE REFRESH DE VERDAD, contra un journal de verdad. No una copia del cuerpo del
	// bucle escrita en la prueba: el BucleDeRefresh del Mecanismo, con una fuente falsa.
	//
	// POR QUE ASI Y NO REESCRIBIENDO EL LAZO. La falsificacion de E-1 lo midio: cambiar UNA linea de
	// BucleDeRefresh.cs -- el checkpoint avanzando un evento de mas, que es saltearse un evento PARA
	// SIEMPRE -- dejo la suite entera en verde, los seis barridos igual y puerta.ps1 sin desacuerdos,
	// porque nada en este repo lo corre. Una prueba que reimplementa el lazo mide la prueba.
	//
	// LAS DOS PROMESAS QUE SE MIDEN. La ingesta es idempotente, y el resume tras una caida es exacto.
	// Las dos salen de la misma decision: el punto de control avanza DENTRO del mismo comando que aplica
	// el hecho, y la guarda es un Check y no un if en el cuerpo.
	//
	//   Check(@evento > bitacora.UltimoEvento()) Error 'ese evento ya se proceso';
	//   Check(cuenta.PuedeX(...)) Error '...';
	//   { cuenta.X(...); bitacora.Avanzar(@evento); }
	//
	// No existe el estado "aplique el hecho pero no avance el checkpoint": o entran los dos o no entra
	// ninguno. Y un rechazo se decide bajo el READ lock, asi que no deja ni una entrada no-op.
	//
	// LA SEGUNDA GUARDA ES LA DEPARTURE, y se mide aca: Concesionario chequea SOLO el numero de evento y
	// deja que el dominio reviente adentro del cuerpo, lo que cicatriza el journal. Este modulo pregunta
	// el Puede... adentro del Check, como los diez comandos de la puerta, asi que un hecho que la cuenta
	// refusa NO deja entrada.
	[TestFixture]
	public sealed class AReceivedEventBlockIsAppliedExactlyOnce
	{
		private const string ElActor = "municipalidad";
		private const string ElToken = "token-de-prueba";

		private string carpeta;
		private MunicipalidadApp app;
		private MunicipalidadAPI api;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-ingesta", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(carpeta);

			app = new MunicipalidadApp(ElActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			api = new MunicipalidadAPI(app);

			// LA APP GUARDA SU CREDENCIAL AL ABRIR, asi que el journal de esta prueba tiene la forma del
			// de verdad. El BUCLE ya no la lee -- es de la fuente HTTP -- y ninguna prueba de aca la
			// necesita: la fuente falsa no pide token.
			api.GuardarCredencial(ElToken, "");
		}

		[TearDown]
		public void Limpiar()
		{
			if (app != null) app.Dispose();

			try
			{
				if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		// ------------------------------------------------------------------ el bloque entero

		[Test]
		public async Task TheWholeNarrativeEntersOnceAndTheCheckpointEndsWhereTheNarrativeEnds()
		{
			ResultadoDeRefresh primero = await UnRefreshDe(LaNarrativa());

			Assert.Multiple(() =>
			{
				Assert.That(primero.Leidos, Is.EqualTo(7));
				Assert.That(primero.Aplicados, Is.EqualTo(7));
				Assert.That(primero.Descartados, Is.EqualTo(0));
				Assert.That(primero.Rechazados, Is.EqualTo(0));
				Assert.That(primero.UltimoEvento, Is.EqualTo(7L));
			});

			Assert.That(api.UltimoEventoAplicado(), Is.EqualTo(7L),
				"Y el punto de control esta en el JOURNAL, no en el resultado: es lo que hace exacto el "
				+ "resume tras una caida.");
		}

		// UN DESCARTE NO MUEVE EL PUNTO DE CONTROL, porque no se escribio nada por el. La senal de un
		// tipo que esta app no consume viaja al final del bloque a proposito: puesta en el medio, el
		// checkpoint la pasaria de largo por el evento siguiente y este renglon no diria nada.
		[Test]
		public async Task ATypeThisAppDoesNotConsumeIsCountedAsDiscardedAndDoesNotMoveTheCheckpoint()
		{
			List<SenalDeEvento> conUnaAjena = new List<SenalDeEvento>(LaNarrativa());
			conUnaAjena.Add(new SenalDeEvento(8, "AvisoDeCorteDeAgua", "{\"aviso\":\"del 12 al 13\"}"));

			ResultadoDeRefresh resultado = await UnRefreshDe(conUnaAjena);

			Assert.Multiple(() =>
			{
				Assert.That(resultado.Leidos, Is.EqualTo(8));
				Assert.That(resultado.Aplicados, Is.EqualTo(7));
				Assert.That(resultado.Descartados, Is.EqualTo(1));
			});

			Assert.That(api.UltimoEventoAplicado(), Is.EqualTo(7L),
				"El 8 se descarto en la frontera, asi que la bitacora queda en el 7: nada se escribio "
				+ "por el.");
		}

		// LA PROMESA 1: RE-ENTREGAR EL MISMO BLOQUE NO DEJA NI UNA ENTRADA.
		//
		// LA FUENTE DE ESTA PRUEBA REENTREGA TODO A PROPOSITO, y descubrirlo fue el punto: con la fuente
		// que respeta el checkpoint, la segunda corrida trae CERO senales y rechaza cero -- que es el
		// checkpoint funcionando, no la idempotencia medida. La promesa que hay que medir es lo que pasa
		// cuando el bloque VUELVE, y para eso el bloque tiene que volver.
		[Test]
		public async Task ARedeliveredBlockLeavesTheJournalByteForByteIdentical()
		{
			await UnRefreshDe(LaNarrativa());

			string antes = ElArbolDe(carpeta);
			string respuestas = LoQueLaCuentaContesta();

			ResultadoDeRefresh otraVez = await new BucleDeRefresh(
				api, new UnaFuenteFalsa(LaNarrativa(), true), MunicipalidadEventRoute.Route).Ejecutar();

			Assert.Multiple(() =>
			{
				Assert.That(otraVez.Aplicados, Is.EqualTo(0), "Ninguno vuelve a entrar.");
				Assert.That(otraVez.Rechazados, Is.EqualTo(7),
					"Los siete los rechaza el Check, uno por uno.");
			});

			Assert.That(ElArbolDe(carpeta), Is.EqualTo(antes),
				"Y el arbol queda BYTE A BYTE igual. Es la forma fuerte del criterio, y solo se sostiene "
				+ "alrededor de algo que no debe escribir NADA: un rechazo bajo el read lock no deja ni "
				+ "una entrada no-op. Con un if adentro del cuerpo en vez de un Check, este renglon "
				+ "diria False y el journal habria crecido en siete.");

			Assert.That(LoQueLaCuentaContesta(), Is.EqualTo(respuestas));
		}

		// EL RECHAZO DICE CUALES GUARDAS FALLARON, EN ORDEN, Y SON DOS CHECK Y NO UNO CON &&.
		//
		// MEDIDO, PORQUE ERA UN TERCER ESTADO: nada en este repo habia puesto DOS Check en un mismo
		// scriptForChk. Los dos corren -- no hay corto circuito entre sentencias Check -- y el motor
		// devuelve un documento con LOS DOS motivos, en el orden en que estan escritos:
		//
		//   {"EWI":[{"Error":"ese evento ya se proceso"},{"Error":"esa finca ya esta, o el numero no sirve"}]}
		//
		// La primera version de esta prueba afirmaba que contestaba SOLO el primero, y se puso roja. Que
		// vengan los dos es mejor que cualquiera de las dos alternativas: con && habria UN mensaje
		// conflando las dos razones -- que es el defecto que DEC-05 ya carga abierto -- y con un solo
		// Check del evento, la cuenta reventaria adentro del cuerpo.
		//
		// EL ORDEN SI IMPORTA Y SE AFIRMA: el del evento va primero, asi que quien lea el documento lee
		// "ya se proceso" antes que "ya estaba", y no al reves -- una re-entrega refusada por contenido
		// se leeria como un feed que manda duplicados.
		[Test]
		public void ARedeliveredEventComesBackNamingEveryGuardThatRefusedItStartingWithTheCheckpoint()
		{
			IMensajeDeEvento laFinca = MunicipalidadEventRoute.Route(LaNarrativa()[0]);

			Assert.That(api.Aplicar(laFinca), Is.Empty, "La primera vez entra.");

			string rechazo = api.Aplicar(laFinca);

			Assert.Multiple(() =>
			{
				Assert.That(rechazo, Is.Not.Empty,
					"Un rechazo mudo seria indistinguible de un exito para el bucle.");

				Assert.That(rechazo, Does.Contain("EWI"),
					"El marcador de rechazo es la propiedad EWI en la raiz del documento, que es lo que "
					+ "Medir.cs ya sabe leer.");

				Assert.That(rechazo, Does.Contain("ese evento ya se proceso"));
				Assert.That(rechazo, Does.Contain("esa finca ya esta"),
					"Los DOS Check corren y los DOS motivos viajan: no hay corto circuito entre "
					+ "sentencias Check.");

				Assert.That(
					rechazo.IndexOf("ese evento ya se proceso"),
					Is.LessThan(rechazo.IndexOf("esa finca ya esta")),
					"Y el del punto de control va PRIMERO, que es el orden en que estan escritos.");
			});
		}

		// Y UN HECHO NUEVO QUE LA CUENTA REFUSA TRAE UN SOLO MOTIVO, que es la otra mitad de lo de
		// arriba: si los dos motivos vinieran siempre juntos, el documento no distinguiria una
		// re-entrega de una refusada por contenido.
		[Test]
		public void AFreshFactTheAccountRefusesComesBackWithOnlyTheAccountsReason()
		{
			string rechazo = api.Aplicar(MunicipalidadEventRoute.Route(
				UnaNarrativaDeLaMunicipalidad.Linea(
					1, "R-QUE-NUNCA-LLEGO", "recoleccion-de-basura", "2026.01", "8000", "0")));

			Assert.Multiple(() =>
			{
				Assert.That(rechazo, Does.Contain("el recibo no esta"));
				Assert.That(rechazo, Does.Not.Contain("ese evento ya se proceso"),
					"El evento 1 es nuevo, asi que esa guarda pasa y no aporta motivo.");
			});
		}

		// ------------------------------------------- lo que la CUENTA refusa, que es la departure

		// EL HUECO DE DEC-07, MEDIDO. Una linea cuyo encabezado no llego se rechaza, NO deja entrada, y
		// no se reintenta. Las tres mitades se afirman por separado porque son tres cosas distintas.
		[Test]
		public async Task ALineWhoseReceiptNeverArrivedIsRefusedWithoutLeavingAnEntry()
		{
			string antes = ElArbolDe(carpeta);
			long checkpointAntes = api.UltimoEventoAplicado();

			ResultadoDeRefresh resultado = await UnRefreshDe(new List<SenalDeEvento>
			{
				UnaNarrativaDeLaMunicipalidad.Linea(
					1, "R-QUE-NUNCA-LLEGO", "recoleccion-de-basura", "2026.01", "8000", "0"),
			});

			Assert.Multiple(() =>
			{
				Assert.That(resultado.Rechazados, Is.EqualTo(1));
				Assert.That(resultado.Aplicados, Is.EqualTo(0));
				Assert.That(resultado.Descartados, Is.EqualTo(0),
					"No se DESCARTA: la senal se pudo leer entera. La refusa la CUENTA, que es otra "
					+ "cosa y otro contador.");
			});

			Assert.That(ElArbolDe(carpeta), Is.EqualTo(antes),
				"Y no deja entrada. Es la departure de este modulo: el Puede... va adentro del Check, "
				+ "asi que la cuenta refusa GRATIS. Chequeando solo el numero de evento, el dominio "
				+ "reventaria adentro del cuerpo con la entrada ya escrita, y esa cicatriz loguearia un "
				+ "fallo de rehidratacion en cada arranque futuro de la cuenta de esta persona.");

			Assert.That(api.UltimoEventoAplicado(), Is.EqualTo(checkpointAntes),
				"Y el punto de control no se movio: el hecho y el checkpoint van juntos o no va ninguno.");
		}

		// Y EL BLOQUE SIGUE. Un hecho que la cuenta refusa no puede parar la narrativa entera: los que
		// vienen detras entran igual, y el checkpoint termina donde termina el ultimo aplicado.
		[Test]
		public async Task AFactTheAccountRefusesDoesNotStopTheBlock()
		{
			List<SenalDeEvento> conUnaHuerfana = new List<SenalDeEvento>
			{
				UnaNarrativaDeLaMunicipalidad.Linea(
					1, "R-QUE-NUNCA-LLEGO", "recoleccion-de-basura", "2026.01", "8000", "0"),
				UnaNarrativaDeLaMunicipalidad.Recibo(2, "R-9001", "2026-02-10T00:00:00Z", "0"),
				UnaNarrativaDeLaMunicipalidad.Linea(
					3, "R-9001", "recoleccion-de-basura", "2026.01", "8000", "0"),
			};

			ResultadoDeRefresh resultado = await UnRefreshDe(conUnaHuerfana);

			Assert.Multiple(() =>
			{
				Assert.That(resultado.Rechazados, Is.EqualTo(1));
				Assert.That(resultado.Aplicados, Is.EqualTo(2));
			});

			Assert.That(api.UltimoEventoAplicado(), Is.EqualTo(3L),
				"El checkpoint salta al 3, asi que el evento 1 NO se vuelve a leer nunca. Ese es el "
				+ "hueco que DEC-07 nombra y no cierra: el feed tiene que mandar el encabezado antes de "
				+ "sus lineas, porque nadie reintenta.");
		}

		// LA MISMA HISTORIA POR EL FEED Y POR LA PUERTA CONTESTA LO MISMO. Es lo que impide que la
		// ingesta se vuelva un segundo camino con su propia aritmetica: los Aplicar... no llaman a nada
		// que los comandos de la puerta no llamen.
		[Test]
		public async Task TheSameStoryThroughTheFeedAndThroughTheDoorAnswersTheSame()
		{
			await UnRefreshDe(LaNarrativa());
			string porElFeed = LoQueLaCuentaContesta();

			string otraCarpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-ingesta", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(otraCarpeta);

			MunicipalidadApp otraApp = new MunicipalidadApp(ElActor);

			try
			{
				otraApp.ConfigureStorage(DatabaseType.FileSystem, "path=" + otraCarpeta);
				otraApp.Start();
				MunicipalidadAPI porLaPuerta = new MunicipalidadAPI(otraApp);

				porLaPuerta.IncorporarFinca("1-234-567");
				porLaPuerta.FijarAvaluoDeFinca(
					"1-234-567", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), 45000000m);
				porLaPuerta.PonerAlCobro(
					"recoleccion-de-basura", "2026.01", 8000m,
					new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));
				porLaPuerta.RegistrarPago(
					"R-9001", new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), 0m);
				porLaPuerta.AgregarLineaAlPago(
					"R-9001", "recoleccion-de-basura", "2026.01", 8000m, 0m);
				porLaPuerta.RegistrarConsulta(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc));
				porLaPuerta.AgregarPendiente(
					new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), "bienes-inmuebles", "2026.01",
					45000m, 1200m);

				Assert.That(LoQueContesta(porLaPuerta), Is.EqualTo(porElFeed),
					"Los mismos siete hechos, por los dos caminos, tienen que dejar la misma CUENTA.");

				// Y LO UNICO QUE DIFIERE ES EL PUNTO DE CONTROL, que no es un hecho de la cuenta sino
				// de la bitacora. La comparacion de arriba lo omite a proposito -- y esta prueba
				// encontro que estaba adentro: EstadoDeLaCuenta cruza las DOS globales de raiz, asi que
				// compararlo entero decia que los dos caminos difieren cuando lo que difiere es
				// exactamente lo que TIENE que diferir.
				Assert.Multiple(() =>
				{
					Assert.That(api.UltimoEventoAplicado(), Is.EqualTo(7L),
						"Por el feed, el punto de control siguio a los hechos.");
					Assert.That(porLaPuerta.UltimoEventoAplicado(), Is.EqualTo(0L),
						"Por la puerta no se movio, y no deberia: la persona y la municipalidad "
						+ "escriben la misma cuenta, pero solo la narrativa recibida tiene numero de "
						+ "evento que avanzar.");
				});
			}
			finally
			{
				otraApp.Dispose();

				try
				{
					if (Directory.Exists(otraCarpeta)) Directory.Delete(otraCarpeta, true);
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
			}
		}

		// ------------------------------------------------------ lo que el bucle le pide al journal

		// EL BUCLE ARRANCA DESDE EL JOURNAL Y NO DESDE UN CAMPO, asi que la fuente recibe el checkpoint
		// journalizado en cada corrida. Con un campo en memoria, una caida a mitad del bloque anterior
		// haria que la segunda corrida pidiera desde un numero que nunca se persistio.
		//
		// EL CHECKPOINT ES LO UNICO QUE EL BUCLE LE PASA A LA FUENTE. Hasta que el modulo se pudo
		// hospedar, tambien le pasaba el token que leia del journal; la credencial es del transporte y
		// se mudo a la fuente HTTP, asi que aca ya no hay nada que afirmar sobre ella.
		[Test]
		public async Task TheLoopAsksTheFeedFromTheCheckpointItReadBackOutOfTheJournal()
		{
			UnaFuenteFalsa fuente = new UnaFuenteFalsa(LaNarrativa());

			await new BucleDeRefresh(api, fuente, MunicipalidadEventRoute.Route).Ejecutar();
			await new BucleDeRefresh(api, fuente, MunicipalidadEventRoute.Route).Ejecutar();

			Assert.That(fuente.DesdeDondeLePidieron, Is.EqualTo(new List<long> { 0L, 7L }),
				"La primera corrida pide desde 0 y la segunda desde 6, que es lo que quedo escrito.");

			Assert.That(fuente.LoQueSirvio, Is.EqualTo(new List<int> { 7, 0 }),
				"Y la segunda corrida trae CERO senales, no siete rechazadas: un proveedor que respeta el "
				+ "checkpoint no reenvia lo que la app ya dijo tener. Lo que pasa cuando SI reenvia lo "
				+ "mide ARedeliveredBlockLeavesTheJournalByteForByteIdentical, con una fuente que "
				+ "reentrega todo.");
		}

		// SIN CREDENCIAL NO SE LEE NADA, y revienta en vez de contestar un bloque vacio: un cero de
		// eventos leidos y un "no tengo con que leer" son dos cosas distintas, y confundirlas haria que
		// una app sin credencial se viera como una municipalidad sin novedades.
		//
		// LA NEGATIVA SE MUDO DEL BUCLE A LA FUENTE HTTP, y sigue siendo la misma promesa. Quien lee
		// necesita credencial o no segun POR DONDE lee: por HTTP hace falta, y por el puente de Gnothi no,
		// porque la credencial la sostiene Gnothi del otro lado. Con la negativa en el bucle, hospedarse
		// obligaba a inventarle al modulo un token que nadie usa.
		[Test]
		public void TheHttpFeedRefusesToReadAtAllWithoutACredential()
		{
			using (HttpClient http = new HttpClient { BaseAddress = new Uri("http://localhost:1/") })
			{
				FuenteHttpDeEventos fuente = new FuenteHttpDeEventos(
					http, "contribuyente", "1", ct => Task.FromResult(""));

				Assert.That(
					Assert.ThrowsAsync<InvalidOperationException>(
						async () => await fuente.LeerDesde(0, CancellationToken.None)).Message,
					Does.Contain("credencial"));
			}
		}

		// EL DUAL, y es la razon de que la negativa se haya mudado: HOSPEDADO NO HAY CREDENCIAL Y EL BLOQUE
		// ENTRA IGUAL. El modulo le pide el sobre a Gnothi, que releva con la suya contra el endpoint que
		// quedo registrado al instalarlo; el punto de control sigue saliendo del journal del actor. El
		// journal de esta prueba no tiene ni un GuardarCredencial, y ademas mide el sobre de verdad: los
		// payloads de este modulo cruzando SobreDeEventos.Interpretar.
		[Test]
		public async Task TheFeedThroughGnothiNeedsNoCredentialAndTheBlockStillEntersOnce()
		{
			string otraCarpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-ingesta", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(otraCarpeta);

			MunicipalidadApp hospedada = new MunicipalidadApp(ElActor);

			try
			{
				hospedada.ConfigureStorage(DatabaseType.FileSystem, "path=" + otraCarpeta);
				hospedada.Start();

				MunicipalidadAPI puerta = new MunicipalidadAPI(hospedada);
				string sobre = ElSobreQueTraeGnothi(LaNarrativa());

				BucleDeRefresh bucle = new BucleDeRefresh(
					puerta,
					new FuenteDeEventosPorGnothi((desde, ct) => Task.FromResult(sobre)),
					MunicipalidadEventRoute.Route);

				ResultadoDeRefresh primero = await bucle.Ejecutar();
				ResultadoDeRefresh segundo = await bucle.Ejecutar();

				Assert.Multiple(() =>
				{
					Assert.That(puerta.TokenGuardado(), Is.Empty,
						"Y se contesto sin credencial guardada: la de Gnothi no entra al journal del modulo.");

					Assert.That(primero.Aplicados, Is.EqualTo(7),
						"El bloque entero entra por el sobre que trae Gnothi, igual que por HTTP.");

					Assert.That(primero.UltimoEvento, Is.EqualTo(7L),
						"Y el punto de control queda donde termina la narrativa.");

					Assert.That(segundo.Aplicados, Is.EqualTo(0),
						"Y el mismo sobre entregado de nuevo no aplica nada: la guarda Check es la misma.");
				});
			}
			finally
			{
				hospedada.Dispose();

				try
				{
					if (Directory.Exists(otraCarpeta)) Directory.Delete(otraCarpeta, true);
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
			}
		}

		// EL FLUSH CORRE DESPUES DE CADA COMANDO Y NO POR TIMER, que es lo que angosta la ventana en la
		// que un cierre abrupto perderia entradas ya commiteadas. En la consola es un no-op; en wasm es
		// el puente MEMFS -> carpeta, y ahi la diferencia es la carpeta de la persona.
		[Test]
		public async Task TheFlushRunsOncePerAppliedCommandAndNotOncePerBlock()
		{
			int flushes = 0;

			await new BucleDeRefresh(
				api,
				new UnaFuenteFalsa(LaNarrativa()),
				MunicipalidadEventRoute.Route,
				() => { flushes++; return Task.CompletedTask; })
			.Ejecutar();

			Assert.That(flushes, Is.EqualTo(7),
				"Siete aplicados, siete flushes. Uno solo al final diria que el flush es por bloque.");

			flushes = 0;

			await new BucleDeRefresh(
				api,
				new UnaFuenteFalsa(LaNarrativa()),
				MunicipalidadEventRoute.Route,
				() => { flushes++; return Task.CompletedTask; })
			.Ejecutar();

			Assert.That(flushes, Is.EqualTo(0),
				"Y un bloque re-entregado no baja nada a disco, porque no escribio nada.");
		}

		// ------------------------------------------------------------------------------- el andamio

		private static IReadOnlyList<SenalDeEvento> LaNarrativa()
		{
			return new List<SenalDeEvento>(UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera());
		}

		// EL SOBRE TAL COMO LO SIRVE EL FEED, que es lo que Gnothi releva sin mirar adentro. El numero y
		// el tipo viajan ADENTRO de cada evento -- son campos del sobre, no de la senal -- asi que se le
		// ponen al payload: es la forma que EndPointDeEventos sirve de verdad.
		private static string ElSobreQueTraeGnothi(IReadOnlyList<SenalDeEvento> senales)
		{
			StringBuilder sobre = new StringBuilder("{\"events\":[");

			for (int i = 0; i < senales.Count; i++)
			{
				if (i > 0) sobre.Append(',');

				sobre.Append("{\"id\":").Append(senales[i].Numero)
					.Append(",\"tipo\":\"").Append(senales[i].Tipo).Append("\",")
					.Append(senales[i].Contenido.Substring(1));
			}

			return sobre.Append("]}").ToString();
		}

		private async Task<ResultadoDeRefresh> UnRefreshDe(IReadOnlyList<SenalDeEvento> senales)
		{
			return await new BucleDeRefresh(
				api, new UnaFuenteFalsa(senales), MunicipalidadEventRoute.Route).Ejecutar();
		}

		private string LoQueLaCuentaContesta()
		{
			return LoQueContesta(api);
		}

		// LO QUE LA CUENTA CONTESTA, como una sola cadena, y SIN el punto de control. El snapshot cruza
		// las DOS globales de raiz, asi que su campo ultimoEventoLeido es de la bitacora y no de la
		// cuenta: dejarlo adentro haria que esta cadena dijera que los dos caminos difieren cuando lo
		// que difiere es lo unico que tiene que diferir. Se tacha en vez de omitir el snapshot entero,
		// para no perder los seis contadores que trae.
		private static string LoQueContesta(MunicipalidadAPI puerta)
		{
			StringBuilder dicho = new StringBuilder();

			dicho.Append(SinElPuntoDeControl(puerta.EstadoDeLaCuenta())).Append('\n');
			dicho.Append(puerta.LoPagadoEn(2026)).Append('\n');

			foreach (string rubro in puerta.RubrosConHistoria())
			{
				RegistroDelHistorial historia = puerta.HistorialDe(rubro, "es");

				dicho.Append(rubro).Append(' ').Append(historia.Estado).Append('\n');

				for (int i = 0; i < historia.Periodo.Length; i++)
					dicho.Append("  ").Append(historia.Periodo[i]).Append(' ')
						.Append(historia.Monto[i]).Append(' ').Append(historia.Mora[i]).Append('\n');
			}

			foreach (string numero in puerta.NumerosDeLasFincas())
				dicho.Append("finca ").Append(numero).Append('\n');

			return dicho.ToString();
		}

		private static string SinElPuntoDeControl(string snapshot)
		{
			int donde = snapshot.IndexOf("\"ultimoEventoLeido\":");
			int coma = snapshot.IndexOf(',', donde);

			Assert.That(donde, Is.GreaterThan(-1),
				"El snapshot dejo de traer ultimoEventoLeido, asi que este tachado ya no tacha nada y "
				+ "la comparacion mide otra cosa.");

			return snapshot.Remove(donde, coma - donde).Insert(donde, "\"ultimoEventoLeido\":<omitido>");
		}

		private static string ElArbolDe(string raiz)
		{
			List<string> archivos = new List<string>(
				Directory.GetFiles(raiz, "*", SearchOption.AllDirectories));
			archivos.Sort(StringComparer.Ordinal);

			StringBuilder huella = new StringBuilder();

			foreach (string archivo in archivos)
			{
				huella.Append(archivo.Substring(raiz.Length)).Append(' ');

				// SE ABRE COMPARTIDO PORQUE EL ACTOR LO TIENE ABIERTO. File.ReadAllBytes revienta con
				// IOException sobre el journal vivo, y esa excepcion se leeria como "la prueba falla"
				// cuando lo que falla es como se lee el archivo.
				using (FileStream bytes = new FileStream(
					archivo, FileMode.Open, FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete))
				using (MD5 md5 = MD5.Create())
					huella.Append(Convert.ToHexString(md5.ComputeHash(bytes)));

				huella.Append('\n');
			}

			return huella.ToString();
		}

		// LA FUENTE FALSA. Sirve el bloque DESDE el numero que le pidan, que es lo que hace un proveedor
		// de verdad, y anota que le pidieron: sin eso no habria como ver de donde salio el "desde".
		//
		// NO LEE NINGUN RELOJ y no tiene estado propio mas alla de lo que anota: lo que guiona una
		// secuencia recibida no toma reloj, porque reproducir la secuencia es todo el punto.
		//
		// NO RECIBE TOKEN, y eso es del mecanismo y no de esta prueba: la credencial es del TRANSPORTE.
		// La fuente HTTP se consigue la suya y la fuente que pasa por Gnothi no tiene ninguna, asi que el
		// bucle no tiene por que saber cual es el caso. Donde vive ahora esa negativa lo miden
		// TheHttpFeedRefusesToReadAtAllWithoutACredential y su dual por el puente.
		private sealed class UnaFuenteFalsa : IFuenteDeEventos
		{
			private readonly IReadOnlyList<SenalDeEvento> senales;
			private readonly bool reentregaTodo;

			internal UnaFuenteFalsa(IReadOnlyList<SenalDeEvento> senales)
				: this(senales, false)
			{
			}

			// REENTREGA TODO: sirve el bloque entero sin mirar el checkpoint. No es una fuente rota --
			// es lo que pasa de verdad cuando el proveedor reenvia, cuando la app pide desde un
			// checkpoint que nunca se persistio, o cuando alguien reinstala sobre el mismo journal. Es
			// el unico modo en que la promesa de idempotencia se puede medir: con la fuente que SI
			// respeta el checkpoint, la segunda corrida no trae nada que rechazar.
			internal UnaFuenteFalsa(IReadOnlyList<SenalDeEvento> senales, bool reentregaTodo)
			{
				this.senales = senales;
				this.reentregaTodo = reentregaTodo;
			}

			internal List<long> DesdeDondeLePidieron { get; } = new List<long>();

			internal List<int> LoQueSirvio { get; } = new List<int>();

			public string Nombre { get { return "una fuente falsa"; } }

			Task<SobreLeido> IFuenteDeEventos.LeerDesde(long despuesDe, CancellationToken ct)
			{
				DesdeDondeLePidieron.Add(despuesDe);

				List<SenalDeEvento> desdeAhi = new List<SenalDeEvento>();

				foreach (SenalDeEvento senal in senales)
					if (reentregaTodo || senal.Numero > despuesDe) desdeAhi.Add(senal);

				LoQueSirvio.Add(desdeAhi.Count);

				return Task.FromResult(new SobreLeido { Senales = desdeAhi });
			}
		}
	}
}
