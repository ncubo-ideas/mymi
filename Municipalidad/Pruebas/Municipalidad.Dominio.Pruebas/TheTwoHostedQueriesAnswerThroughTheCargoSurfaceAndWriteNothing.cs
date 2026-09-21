using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Gnothi.Contract;
using Municipalidad.App;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// LAS DOS CONSULTAS QUE ESTE MODULO CONTESTA CUANDO CORRE ADENTRO DE GNOTHI, por la superficie de
	// carga: la que el Host construye pasandole la carpeta que le concede.
	//
	// POR QUE UNA FIXTURE APARTE Y NO UN RENGLON EN TheReadOnlyHostNeverWritesToThePersonsJournal. Esa
	// mide UNA promesa -- que leer no escribe -- sobre el anfitrion del asistente. Aca hay tres cosas
	// distintas: que la superficie ofrezca EXACTAMENTE lo que el manifiesto declara (declarado contra
	// real, que es lo que Gnothi coteja al dar de alta el modulo), que las dos respuestas digan lo que
	// la narrativa dejo, y que preguntar por esta puerta tampoco escriba.
	//
	// LOS NUMEROS SE MIDEN CONTRA UN INSTANTE FIJO Y NO CONTRA EL RELOJ. Las dos consultas toman el
	// instante por parametro justamente por eso: la superficie de carga lee su propio reloj -- es una
	// LECTURA, asi que no es monotono -- y una prueba que dependiera de el afirmaria cosas distintas
	// segun el dia en que corra. Lo que se mide por la superficie es lo que NO depende del reloj.
	//
	// LO QUE ESTA FIXTURE NO PUEDE MEDIR: la otra superficie. En el telefono el modulo contesta por
	// SuperficieDelModulo, que es [JSInvokable] y necesita un WebView; los CUERPOS son los mismos
	// -- ConsultasDelModulo -- y es esa comunidad, y no una segunda prueba, lo que hace que el modulo
	// conteste igual corra donde corra.
	[TestFixture]
	public sealed class TheTwoHostedQueriesAnswerThroughTheCargoSurfaceAndWriteNothing
	{
		private const string ElRubro = "recoleccion-de-basura";
		private const string LaFinca = "F-1";

		private static readonly DateTime LaConsulta = new DateTime(2025, 9, 3);

		// DOS INSTANTES Y NO UNO, porque las dos listas se llenan en momentos distintos: al 2025-09-25
		// nada se ha vencido y hay algo por vencer; al 2025-11-15 es al reves. Con un solo instante, la
		// mitad vacia de la respuesta quedaria sin medir.
		private static readonly DateTime AntesDeQueVenza = new DateTime(2025, 9, 25);
		private static readonly DateTime DespuesDeQueVencio = new DateTime(2025, 11, 15);

		private string carpeta;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-hospedada", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(carpeta);
		}

		[TearDown]
		public void Limpiar()
		{
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

		// DECLARADO CONTRA REAL, y las tres declaraciones tienen que decir lo mismo: los nombres que
		// lleva la superficie, la lista que el modulo contesta cuando Gnothi le pregunta que sabe hacer,
		// y lo que el manifiesto declara. Gnothi coteja las dos primeras al dar de alta el modulo; el
		// manifiesto no lo coteja nadie mas que este renglon.
		[Test]
		public void TheSurfaceOffersExactlyTheTwoQueriesTheManifestDeclares()
		{
			List<string> enLaSuperficie = new List<string>();

			foreach (MethodInfo cada in typeof(MunicipalidadSurface).GetMethods())
			{
				QueryAttribute consulta = cada.GetCustomAttribute<QueryAttribute>();
				if (consulta != null) enLaSuperficie.Add(consulta.Name);
			}

			enLaSuperficie.Sort(StringComparer.Ordinal);

			List<string> declaradas = new List<string>(ConsultasDelModulo.Declaradas);
			declaradas.Sort(StringComparer.Ordinal);

			List<string> enElManifiesto = new List<string>();

			foreach (JsonElement cada in ElManifiesto().GetProperty("consultasHospedadas").EnumerateArray())
				enElManifiesto.Add(cada.GetProperty("nombre").GetString());

			enElManifiesto.Sort(StringComparer.Ordinal);

			Assert.Multiple(() =>
			{
				Assert.That(enLaSuperficie, Is.EqualTo(new List<string> { "lista_de_vencimientos", "resumen" }),
					"La superficie de carga lleva las dos consultas y ninguna mas.");

				Assert.That(declaradas, Is.EqualTo(enLaSuperficie),
					"Y lo que el modulo dice que sabe hacer es exactamente lo que lleva la superficie. "
					+ "Si difieren, el modulo miente cuando Gnothi le pregunta.");

				Assert.That(enElManifiesto, Is.EqualTo(enLaSuperficie),
					"Y el manifiesto declara las mismas dos. Un manifiesto que se atrasa no pone rojo "
					+ "nada por si solo, que es por lo que existe este renglon.");
			});
		}

		// EL RESUMEN CUENTA PERIODOS Y NUNCA UN MONTO, y eso no es una omision: cuanto debe la persona
		// hoy lo contesta el portal de la municipalidad, que es la autoridad sobre su deuda. Un resumen
		// con un total seria esta app contestando la pregunta que decidio no contestar.
		[Test]
		public void TheSummaryCountsPeriodsAndCarriesNoAmountAtAll()
		{
			UnExpedienteComoElDeUnaPersona();

			string dicho = LoQueContesta(api => ConsultasDelModulo.HacerResumen(api, AntesDeQueVenza));

			Assert.Multiple(() =>
			{
				Assert.That(dicho, Does.Contain("\"hayDatos\":true"));
				Assert.That(dicho, Does.Contain("\"preguntadoEn\":\"2025-09-25\""),
					"El dia con que se contesto viaja en la respuesta: los dias que faltan son respecto "
					+ "de ese dia y de ningun otro.");
				Assert.That(dicho, Does.Contain("\"fincas\":1"));
				Assert.That(dicho, Does.Contain("\"rubrosConHistoria\":1"),
					"La narrativa dejo comprobantes de un solo rubro.");
				Assert.That(dicho, Does.Contain("\"diasDeAviso\":7"),
					"Y el umbral con que se midio, que NADIE HA FIRMADO, sale en la respuesta.");
				Assert.That(dicho, Does.Contain("\"ultimoEventoLeido\":0"),
					"Este expediente lo escribio la app con comandos, no un feed, asi que la bitacora "
					+ "esta en cero y eso es un valor legal.");

				Assert.That(dicho, Does.Not.Contain("monto"),
					"Un monto en el resumen seria esta app contestando cuanto debe la persona, que es lo "
					+ "que su portal contesta y esta app no.");
				Assert.That(dicho, Does.Not.Contain("total"),
					"Y un total, lo mismo.");
			});
		}

		// LAS DOS LISTAS NO SE MEZCLAN Y CADA FILA TRAE LA FRASE DEL DOMINIO. Que una este vacia es una
		// respuesta: al 2025-09-25 no hay nada vencido, y eso no es un hueco.
		[Test]
		public void TheListKeepsWhatFallsDueApartFromWhatAlreadyDid()
		{
			UnExpedienteComoElDeUnaPersona();

			string antes = LoQueContesta(
				api => ConsultasDelModulo.HacerListaDeVencimientos(api, AntesDeQueVenza));

			string despues = LoQueContesta(
				api => ConsultasDelModulo.HacerListaDeVencimientos(api, DespuesDeQueVencio));

			Assert.Multiple(() =>
			{
				Assert.That(antes, Does.Contain("\"tasaConQueSeProyecto\":0.062"),
					"La tasa con que se proyecta el costo de pagar tarde sale en la respuesta, porque es "
					+ "una proyeccion de esta app y no la mora de la municipalidad.");

				Assert.That(antes, Does.Contain("\"vencidos\":[]"),
					"Al 2025-09-25 no hay nada vencido, y una lista vacia es una respuesta.");

				Assert.That(antes, Does.Contain("\"periodo\":\"2025-09\",\"vence\":\"2025-09-30\""),
					"El periodo y el dia en que vence son DOS hechos y viajan en dos campos.");

				Assert.That(antes, Does.Contain("\"faltan\":5,\"urgencia\":\"aviso\""),
					"A cinco dias del vencimiento, con siete de aviso, la urgencia es 'aviso'.");

				Assert.That(antes, Does.Contain("\"urgencia\":\"ninguna\""),
					"Y el periodo que vence a 36 dias no apura: son dos filas con urgencias distintas, no "
					+ "una sola clasificacion para el rubro.");

				Assert.That(antes, Does.Contain("es un recordatorio y no una parada obligatoria"),
					"La FRASE de cada fila la compone el dominio y viaja tal cual: es la respuesta, y no un "
					+ "adorno de la fila.");

				Assert.That(despues, Does.Contain("\"porVencer\":[]"),
					"Y al 2025-11-15 no queda nada por vencer.");

				Assert.That(despues, Does.Contain("\"dias\":46,\"costoProyectado\":187.53"),
					"Un periodo vencido cuenta dias de atraso y no dias que faltan, y su costo es el que la "
					+ "tasa proyecta.");

				Assert.That(despues, Does.Contain("no la mora que la municipalidad cobra"),
					"Y la frase de la fila dice que ese costo es una proyeccion, para que nadie lo sume al "
					+ "monto y lo presente como lo que hay que pagar.");
			});
		}

		// PREGUNTAR POR ESTA PUERTA NO ESCRIBE, que es la misma disciplina que la del asistente: con el
		// anfitrion de la app, CADA pregunta del Host dejaria una entrada en el journal de la persona.
		//
		// LO QUE SE MIDE ES LA PREGUNTA Y NO LA APERTURA, y la diferencia es todo el punto. Desde la
		// forma hibrida esta superficie abre UN actor vivo con el anfitrion que SIEMBRA -- es deliberado
		// y su razon esta en el /// del campo `app`: dos actores sobre un journal fue el problema, y sin
		// semilla el DSL se queda sin sus globales y el bucle muere pidiendo el cursor.
		//
		// ESA APERTURA DEJA UN REGISTRO INERTE, uno por hidratacion, y no es un defecto de este modulo:
		// es la forma documentada del anfitrion escribible, medida y dispuesta como C16 -- 124 bytes, el
		// mismo script de `upgrade`, y las respuestas byte-identicas antes y despues. La guarda del
		// upgrade hace la semilla idempotente EN EFECTO, no en el journal.
		//
		// Asi que la linea base se toma con el actor YA ABIERTO. Medido al escribir esto: ocho preguntas
		// y UNA sola entrada nueva, la de la apertura -- si preguntar escribiera, serian ocho.
		[Test]
		public void AskingThroughTheSurfaceLeavesTheJournalWithoutANewEntry()
		{
			UnExpedienteComoElDeUnaPersona();

			using (MunicipalidadSurface superficie = new MunicipalidadSurface(carpeta))
			{
				// La primera pregunta es la que abre: `app` es perezoso, asi que construir la
				// superficie todavia no hidrata nada.
				Assert.That(Dicho(superficie.Resumen()), Does.Contain("\"hayDatos\":true"),
					"La apertura llego de verdad al dominio: sin esto la linea base se tomaria sobre un "
					+ "actor que nunca se hidrato, y la promesa quedaria sin medir.");

				long entradasAntes = LasEntradasDe(carpeta);
				long largoAntes = ElLargoDelJournalDe(carpeta);

				for (int vuelta = 0; vuelta < 4; vuelta++)
				{
					Assert.That(Dicho(superficie.Resumen()), Does.Contain("\"hayDatos\":true"),
						"La vuelta llego de verdad al dominio: sin esto la promesa se estaria midiendo "
						+ "sobre una respuesta vacia.");

					Assert.That(Dicho(superficie.ListaDeVencimientos()), Does.Contain("\"porVencer\":"));
				}

				Assert.Multiple(() =>
				{
					Assert.That(LasEntradasDe(carpeta), Is.EqualTo(entradasAntes),
						"Una pregunta del Host dejo una entrada en el journal de la persona. Eso es un "
						+ "segundo escritor sobre el mismo actor.");

					Assert.That(ElLargoDelJournalDe(carpeta), Is.EqualTo(largoAntes),
						"El archivo del journal crecio. Ninguna lectura puede hacerlo crecer.");
				});
			}
		}

		// Y LA APERTURA DEJA EXACTAMENTE UNA, no dos ni ninguna. Es el otro lado de la de arriba: sin
		// esto, mover la linea base alla habria tapado el dia en que abrir empiece a escribir de mas.
		[Test]
		public void OpeningTheSurfaceLeavesExactlyTheOneInertSeedRecord()
		{
			UnExpedienteComoElDeUnaPersona();

			long entradasAntes = LasEntradasDe(carpeta);

			using (MunicipalidadSurface superficie = new MunicipalidadSurface(carpeta))
			{
				Assert.That(Dicho(superficie.Resumen()), Does.Contain("\"hayDatos\":true"));

				Assert.That(LasEntradasDe(carpeta), Is.EqualTo(entradasAntes + 1),
					"Abrir con el anfitrion que siembra deja UN registro inerte, y solo uno. Dos "
					+ "querria decir que la semilla dejo de ser una-vez-por-actor; ninguno, que el "
					+ "anfitrion cambio y el DSL puede quedarse sin sus globales.");
			}
		}

		// LA APP NUNCA SE ABRIO EN ESTE DISPOSITIVO ES UNA RESPUESTA, y dice DONDE se busco: un "no hay
		// datos" que no dice donde no se puede accionar.
		[Test]
		public void AFolderWithNoJournalIsAnAnswerAndSaysWhereItLooked()
		{
			string dicho = Dicho(new MunicipalidadSurface(carpeta).Resumen());

			Assert.Multiple(() =>
			{
				Assert.That(dicho, Does.Contain("\"hayDatos\":false"));
				Assert.That(dicho, Does.Contain(Path.GetFullPath(carpeta).Replace("\\", "\\\\")),
					"Y la carpeta en la que se busco viaja en la respuesta.");
				Assert.That(dicho, Does.Contain("abra su app de la municipalidad"),
					"Y se dice en espanol y con palabras que no es un error de nadie.");
			});
		}

		// ------------------------------------------------------ el aparejo

		// EL EXPEDIENTE DE UNA PERSONA, escrito por el anfitrion DE LA APP y cerrado antes de leer, que
		// es como estan las dos cosas de verdad: la app en un proceso y Gnothi en otro. Es el mismo
		// expediente de TheReadOnlyHostNeverWritesToThePersonsJournal, sin la credencial: por esta
		// puerta no hay ninguna.
		private void UnExpedienteComoElDeUnaPersona()
		{
			using (MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor))
			{
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();

				MunicipalidadAPI api = new MunicipalidadAPI(app);

				api.IncorporarFinca(LaFinca);

				api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
				api.AgregarLineaAlPago("R-1", ElRubro, "2024.03", 5000m, 0m);

				api.RegistrarPago("R-2", new DateTime(2024, 6, 30), 0m);
				api.AgregarLineaAlPago("R-2", ElRubro, "2024.06", 18000m, 310m);

				api.RegistrarConsulta(LaConsulta);
				api.AgregarPendiente(LaConsulta, ElRubro, "2025.09", 24000m, 45m);

				api.PonerAlCobro(ElRubro, "2025.10", 8000m, new DateTime(2025, 9, 10));

				api.AdoptarReserva(30000m, new DateTime(2025, 8, 1));
			}
		}

		// UNA PREGUNTA POR EL MISMO CAMINO QUE LA SUPERFICIE, con el anfitrion de solo lectura, para
		// medir los numeros contra un instante fijo en vez del reloj.
		private string LoQueContesta(Func<MunicipalidadAPI, object> pregunta)
		{
			AppDeMunicipalidadSoloLectura lector = new AppDeMunicipalidadSoloLectura();
			try
			{
				lector.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				lector.Start();

				return Dicho(pregunta(new MunicipalidadAPI(lector)));
			}
			finally
			{
				try { lector.Actor.GracefulExit(); } catch { }
			}
		}

		// La respuesta como la ve Gnothi: serializada, que es la unica forma en que sale del modulo.
		private static string Dicho(object respuesta) => JsonSerializer.Serialize(respuesta);

		// EL MANIFIESTO SE LEE DEL OUTPUT Y NO DEL ARBOL, y no es capricho: --artifacts-path reubica el
		// output entero, asi que ninguna cantidad fija de '..' llega de vuelta al modulo. Viaja como dato
		// -- ver el ItemGroup del csproj --, igual que para ManifestSaysExactlyWhatTheAssemblyOffers.
		private static JsonElement ElManifiesto()
		{
			string ruta = Path.Combine(TestContext.CurrentContext.TestDirectory, "manifiesto.json.dato");

			Assert.That(File.Exists(ruta), Is.True,
				"El manifiesto no llego al output como dato. Sin el, esta prueba no puede fallar.");

			return JsonDocument.Parse(File.ReadAllText(ruta)).RootElement.Clone();
		}

		private static long LasEntradasDe(string carpeta)
		{
			AppDeMunicipalidadSoloLectura lector = new AppDeMunicipalidadSoloLectura();
			try
			{
				lector.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				lector.Start();

				return lector.Actor.CurrentEntryId;
			}
			finally
			{
				try { lector.Actor.GracefulExit(); } catch { }
			}
		}

		private static long ElLargoDelJournalDe(string carpeta)
		{
			long total = 0;

			foreach (string archivo in Directory.GetFiles(
				Path.Combine(carpeta, MunicipalidadApp.ElActor, "journal"), "*.bin"))
				total += new FileInfo(archivo).Length;

			return total;
		}
	}
}
