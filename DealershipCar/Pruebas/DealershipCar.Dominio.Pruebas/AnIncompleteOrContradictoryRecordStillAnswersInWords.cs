using System;
using System.IO;
using Choreography.Theater;
using DealershipCar.App;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE EL ASISTENTE RECIBE CUANDO AL REGISTRO LE FALTA UN HECHO, O CUANDO SE CONTRADICE.
	//
	// LA FRASE COMPUESTA DE COBERTURA RECHAZA cuando al registro le falta el sobre, la entrega o una
	// lectura al instante preguntado. No es capricho: el catalogo del fabricante esta llaveado por
	// MODELO y lo comparten todos los carros de ese modelo, asi que no sabe -- ni puede saber -- que
	// le falta a UN vehiculo. Lo que si contesta, y entero, son las DOS MITADES por separado: lo que
	// el fabricante publico sobre el alcance, y lo que el registro tiene sobre este carro, que nombra
	// con sus propias palabras cual de los tres hechos falta.
	//
	// LAS DOS MITADES SON DEL DOMINIO Y NO SE ARMA UNA TERCERA CON PEDAZOS DE LAS DOS. Esa es la
	// diferencia entre una respuesta y una disculpa, y la razon por la que lo es se puede afirmar:
	// NINGUNA de las dos frases habla del fallo. Cada una habla de su propio sujeto.
	//
	// UN MODELO Y UNA PERSONA FALLAN DISTINTO ANTE UN HUECO. Una persona se encoge de hombros; un
	// modelo lo rellena. Por eso lo que se afirma aca no es solo que las frases lleguen, sino que
	// llegue con ellas la instruccion de que los tres hechos los pone LA AGENCIA -- ni la persona ni
	// el asistente -- y de que no se estiman.
	//
	// Y EL REGISTRO QUE SE CONTRADICE es el unico hueco conocido de esta superficie. Dos hechos que
	// la agencia registro pueden no poder ser ciertos los dos, y quien pregunta no puso ninguno de
	// los dos: una tool que dejara salir eso le devolveria a un asistente el reventon del dominio.
	[TestFixture]
	public sealed class AnIncompleteOrContradictoryRecordStillAnswersInWords
	{
		private const string TheUnit = "UnitB";
		private const string TheSystem = "POWERTRAIN";
		private const string TheRevision = "WE2025-R1";

		private static readonly DateTime ThePublication =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDelivery =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		// UNA ENTREGA FECHADA EN EL FUTURO. La agencia corrige entregas -- el comando es correctivo a
		// proposito -- asi que un ano mal tecleado deja el registro contradiciendose para todo
		// instante anterior a esa fecha, que hoy son todos.
		private static readonly DateTime TheDeliveryThatHasNotHappenedYet =
			new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		// ===== AL REGISTRO LE FALTA UN HECHO =============================================

		// LAS DOS MITADES CONTESTAN ENTERAS Y LA COMPUESTA NO SE PIDE. Sobre un registro al que le
		// falta todo, la tool devuelve el alcance publicado y la frase del vehiculo, cada una del
		// dominio, y la compuesta vacia.
		[Test]
		public void WithNothingRecordedBothHalvesStillAnswerAndNeitherIsAboutTheFailure()
		{
			using (var bare = AVehicleWithASealedScopeAndNothingElse())
			{
				WarrantyScope scope = TheAssistantAsksAboutCoverage(bare);

				Assert.That(scope.EnvelopePublished, Is.False);
				Assert.That(scope.DeliveryRecorded, Is.False);
				Assert.That(scope.OdometerKnown, Is.False);

				Assert.That(scope.StandingSays, Is.EqualTo(""),
					"La compuesta no se pide: el catalogo es de un MODELO y no sabe que le falta a un "
					+ "carro.");
				Assert.That(scope.Standing, Is.EqualTo(""));
				Assert.That(scope.StandingCanBeEvaluated, Is.False);

				// LA MITAD PUBLICADA, ENTERA.
				Assert.That(scope.Published, Is.True);
				Assert.That(scope.Says, Does.Contain("'POWERTRAIN' está dentro del alcance publicado"));
				Assert.That(scope.Says, Does.Contain("revisión " + TheRevision));

				// LA MITAD DEL VEHICULO, ENTERA, Y NOMBRANDO LO QUE FALTA CON SUS PALABRAS.
				Assert.That(scope.ThisVehicleSays, Does.Contain("Sin publicar"));
				Assert.That(
					scope.ThisVehicleSays,
					Does.Contain("no es lo mismo que este vehículo no tenga garantía"));

				// Y NINGUNA DE LAS DOS HABLA DEL FALLO: cada una habla de su propio sujeto. Es lo
				// que hace que esto se lea como una respuesta y no como una disculpa.
				Assert.That(scope.Says, Does.Not.Contain("no se pudo"));
				Assert.That(scope.ThisVehicleSays, Does.Not.Contain("no se pudo"));
			}
		}

		// Y SE LE DICE AL ASISTENTE QUIEN PONE LOS TRES HECHOS. Sin esto, un modelo que lee "no se
		// pudo" rellena el hueco con una estimacion que suena razonable y viaja despues como un
		// hecho: es la unica forma de fallar que le cuesta dinero a la persona.
		[Test]
		public void TheAssistantIsToldWhoRecordsTheMissingFactsAndNotToEstimateThem()
		{
			using (var bare = AVehicleWithASealedScopeAndNothingElse())
			{
				string detail = TheAssistantAsksAboutCoverage(bare).Detail;

				Assert.That(detail, Does.Contain("al menos uno de los tres hechos"));
				Assert.That(detail, Does.Contain("Las dos mitades siguen contestando por separado"));
				Assert.That(detail, Does.Contain("Los tres hechos vienen del concesionario"));
				Assert.That(detail, Does.Contain("ni la persona ni usted pueden aportar uno"));

				string envelope = TheAssistantAsks(bare).Detail;

				Assert.That(envelope, Does.Contain("El concesionario es quien declara los dos"));
				Assert.That(envelope, Does.Contain("NO es un vehículo sin garantía"));
			}
		}

		// EL SOBRE Y LA ENTREGA ESTAN Y LA LECTURA NO: solo se pudo medir la mitad de tiempo, y la de
		// distancia se dice AUSENTE en vez de cero. Un cero seria una lectura, y el registro no tiene
		// ninguna.
		[Test]
		public void WithNoOdometerReadingTheTimeHalfIsMeasuredAndTheDistanceHalfIsNamedAbsent()
		{
			using (AScheduleOnARealJournal noReading = AVehicleWithNoOdometerReading())
			{
				WarrantyStanding standing = TheAssistantAsks(noReading);

				Assert.That(standing.EnvelopePublished, Is.True);
				Assert.That(standing.DeliveryRecorded, Is.True);
				Assert.That(standing.OdometerKnown, Is.False);
				Assert.That(standing.Odometer, Is.EqualTo(0),
					"Cero con odometerKnown en false NO es una lectura de cero, y el campo lo dice.");

				Assert.That(standing.CanBeEvaluated, Is.False);
				Assert.That(standing.Standing, Is.EqualTo(""),
					"Ninguno de los tres nombres quiere decir 'no lo se', asi que no se devuelve uno.");

				Assert.That(standing.Says, Does.Contain("meses de 240 meses."),
					"La mitad de tiempo SI se midio: no tener odometro no la degrada.");
				Assert.That(
					standing.Says,
					Does.Contain("Distancia: no se puede medir, porque no hay ninguna lectura del odómetro"));
				Assert.That(
					standing.Says,
					Does.Contain("quien trae una es el feed del concesionario"));

				Assert.That(
					standing.Detail,
					Does.Contain("no ha llegado ninguna lectura del odómetro"));
				Assert.That(standing.Detail, Does.Contain("no que está bien, y no cero"));
			}
		}

		// ===== EL REGISTRO SE CONTRADICE =================================================

		// LA PRIMERA FILA: el odometro de hoy por debajo de la lectura de la entrega, en la misma
		// unidad. Las DOS familias de tools lo devuelven como una bandera y unas palabras, y ninguna
		// deja salir el reventon del dominio.
		[Test]
		public void AnOdometerBelowTheDeliveryReadingComesBackAsWordsAndNotAsAnError()
		{
			using (AScheduleOnARealJournal broken = AVehicleWhoseOdometerWentBackwards())
			{
				WarrantyStanding standing = TheAssistantAsks(broken);

				Assert.That(standing.RecordContradictsItself, Is.True);
				Assert.That(standing.Says, Is.EqualTo(""));
				Assert.That(standing.Standing, Is.EqualTo(""));
				Assert.That(standing.CanBeEvaluated, Is.False);
				Assert.That(standing.Detail, Does.Contain("se contradicen entre sí"));
				Assert.That(standing.Detail, Does.Contain("solo el concesionario puede corregirlos"));
				Assert.That(standing.Detail, Does.Contain("No estime la posición"));

				// LOS HECHOS SUELTOS SIGUEN CONTESTANDO, porque ninguno de ellos compone nada: es
				// justo lo que le deja a la persona ver los dos numeros que no pueden ser los dos.
				Assert.That(standing.Odometer, Is.EqualTo(9000));
				Assert.That(standing.DeliveryOdometer, Is.EqualTo(50000));

				WarrantyScope scope = TheAssistantAsksAboutCoverage(broken);

				Assert.That(scope.RecordContradictsItself, Is.True);
				Assert.That(scope.StandingSays, Is.EqualTo(""));
				Assert.That(scope.ThisVehicleSays, Is.EqualTo(""),
					"Y la mitad del vehiculo tambien sale vacia: una bandera que dice 'no se pudo "
					+ "medir nada' al lado de una frase que mide algo es peor que las dos por "
					+ "separado.");

				Assert.That(scope.Published, Is.True,
					"El alcance PUBLICADO sigue contestando: no depende de ningun hecho de este "
					+ "carro.");
				Assert.That(scope.Says, Does.Contain("revisión " + TheRevision));
			}
		}

		// EL CONTROL, Y SIN EL LO DE ARRIBA NO PRUEBA NADA: la puerta SI revienta, y revienta con la
		// excepcion del DOMINIO. La caratula no puede nombrar ese tipo -- es interno a su ensamblado
		// -- asi que lo reconoce por el nombre, y esto es lo que fija que el nombre sigue siendo ese.
		[Test]
		public void TheDoorReallyThrowsAndItIsTheDomainsOwnExceptionThatTheToolRecognises()
		{
			using (AScheduleOnARealJournal broken = AVehicleWhoseOdometerWentBackwards())
			{
				DateTime asOf = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

				Exception thrown = Assert.Catch(
					() => broken.Api.DescribeWarrantyEnvelope(asOf));

				Assert.That(thrown.GetType().Name, Is.EqualTo("DomainException"),
					"Si el dominio le cambia el nombre a su excepcion, la tool deja de reconocerla y "
					+ "el reventon le llega al asistente. El nombre es lo unico que una caratula "
					+ "tiene, y esta es la prueba que lo dice.");

				Assert.Throws<DomainException>(
					() => broken.Api.DescribeCoverageForThisVehicle(TheSystem, asOf));
			}
		}

		// LA SEGUNDA FILA, Y LAS DOS TOOLS SE PARTEN. La entrega esta fechada en el futuro: la de
		// cobertura RECHAZA, y la del sobre CONTESTA -- como una respuesta decidida, con mas meses
		// que los que el plazo tiene. Esta medido y NO esta arreglado: arreglarlo es una decision del
		// dominio y esta anotada como hueco abierto. Lo unico que separa esa frase de un "le quedan
		// setenta anos de garantia" es el campo deliveredAt y lo que su descripcion le dice al modelo.
		[Test]
		public void ADeliveryDatedInTheFutureIsRefusedByOneToolAndAnsweredByTheOther()
		{
			using (AScheduleOnARealJournal ahead = AVehicleDeliveredInTheFuture())
			{
				WarrantyScope scope = TheAssistantAsksAboutCoverage(ahead);

				Assert.That(scope.RecordContradictsItself, Is.True);
				Assert.That(scope.StandingSays, Is.EqualTo(""));
				Assert.That(scope.Detail, Does.Contain("se contradicen entre sí"));

				WarrantyStanding standing = TheAssistantAsks(ahead);

				Assert.That(standing.RecordContradictsItself, Is.False,
					"La del sobre NO revienta con este registro: contesta.");
				Assert.That(standing.Standing, Is.EqualTo("Inside"));
				Assert.That(standing.Says, Does.Contain("meses de 36 meses."),
					"Y contesta con MAS plazo del que el sobre publico, porque los meses cumplidos "
					+ "salen negativos. No miente: dice exactamente lo que el registro implica.");

				// LO UNICO QUE LO SEPARA DE UN DISPARATE ES ESTE CAMPO Y SU DESCRIPCION. La fecha de
				// entrega esta en la respuesta y es POSTERIOR al instante en que se pregunto, asi que
				// el modelo lo puede ver sin que la caratula vuelva a derivar la regla del dominio.
				Assert.That(
					DateTime.Parse(
						standing.DeliveredAt, null,
						System.Globalization.DateTimeStyles.RoundtripKind),
					Is.GreaterThan(
						DateTime.Parse(
							standing.AskedAt, null,
							System.Globalization.DateTimeStyles.RoundtripKind)),
					"deliveredAt posterior a askedAt es lo que el modelo tiene que poder ver. Si "
					+ "alguno de los dos campos desapareciera, el disparate se quedaria sin nada que "
					+ "lo delate.");
			}
		}

		// ===== UN JOURNAL MAS VIEJO QUE LA CUARTA GLOBAL ==================================

		// LA GARANTIA DE ESTE VEHICULO LLEGO DESPUES QUE TODO LO DEMAS, asi que todo journal escrito
		// antes no la tiene, y este proceso NO la puede crear: crearla seria escribir. Se degrada
		// SOLO ahi -- el alcance publicado sigue contestando entero -- y se dice con una bandera y no
		// con silencio.
		[Test]
		public void AJournalOlderThanTheVehiclesOwnRecordSaysSoAndTheScopeStillAnswers()
		{
			string folder = AJournalWrittenBeforeTheProfileExisted();

			try
			{
				WarrantyStanding standing =
					new WarrantyEnvelopeTools(new JournalReader(folder, ""))
						.IsMyCarStillUnderWarranty();

				Assert.That(standing.HasData, Is.True,
					"El registro EXISTE. 'No hay nada grabado' y 'esta app es anterior a esto' son "
					+ "dos ausencias distintas con dos remedios distintos.");
				Assert.That(standing.EnvelopeKnown, Is.False);
				Assert.That(standing.Says, Is.EqualTo(""));
				Assert.That(standing.PublishedSays, Is.EqualTo(""));
				Assert.That(standing.Detail, Does.Contain("todavía no guardaba lo que"));
				Assert.That(standing.Detail, Does.Contain("abra su app una vez"));
				Assert.That(
					standing.Detail,
					Does.Contain("no es lo mismo que el carro no tenga garantía"));

				WarrantyScope scope =
					new ModelSpecificationTools(new JournalReader(folder, ""))
						.DoesTheWarrantyCover(TheSystem);

				Assert.That(scope.EnvelopeKnown, Is.False);
				Assert.That(scope.StandingSays, Is.EqualTo(""));
				Assert.That(scope.ThisVehicleSays, Is.EqualTo(""));

				Assert.That(scope.SpecificationKnown, Is.True,
					"Y la especificacion SI esta: se degrada una global y no la respuesta entera.");
				Assert.That(scope.Published, Is.True);
				Assert.That(scope.Says, Does.Contain("revisión " + TheRevision));
			}
			finally
			{
				SweepAway(folder);
			}
		}

		// EL CONTROL DE LA BANDERA NUEVA, y es el que impide que se vuelva un "siempre false": sobre
		// un journal de la app de HOY la cuarta global SI esta.
		[Test]
		public void AndOnAJournalTheCurrentAppWroteTheVehiclesOwnRecordIsKnown()
		{
			using (var current = new AScheduleOnARealJournal("1HGCM82633A004352", "Standard"))
			{
				WarrantyStanding standing =
					new WarrantyEnvelopeTools(new JournalReader(current.Folder, ""))
						.IsMyCarStillUnderWarranty();

				Assert.That(standing.EnvelopeKnown, Is.True,
					"Si esto se pone rojo, la bandera se volvio una constante y la prueba de arriba "
					+ "no prueba nada.");
				Assert.That(standing.PublishedSays, Is.Not.Empty);
				Assert.That(standing.Says, Is.Not.Empty);
			}
		}

		// ------------------------------------------------------ el aparejo

		private static WarrantyStanding TheAssistantAsks(AScheduleOnARealJournal journal)
		{
			return new WarrantyEnvelopeTools(new JournalReader(journal.Folder, ""))
				.IsMyCarStillUnderWarranty();
		}

		private static WarrantyScope TheAssistantAsksAboutCoverage(AScheduleOnARealJournal journal)
		{
			return new ModelSpecificationTools(new JournalReader(journal.Folder, ""))
				.DoesTheWarrantyCover(TheSystem);
		}

		private static AScheduleOnARealJournal AVehicleWithASealedScopeAndNothingElse()
		{
			return ASealedScope("1FTFW1ET5DFA00001");
		}

		private static AScheduleOnARealJournal AVehicleWithNoOdometerReading()
		{
			AScheduleOnARealJournal journal = ASealedScope("1HGCM82633A004352");

			journal.Api.PublishWarrantyEnvelope(240, 20000, TheUnit, ThePublication);
			journal.Api.RecordDelivery(TheDelivery, 120, TheUnit);

			return journal;
		}

		// LA LECTURA DE LA ENTREGA POR ENCIMA DEL ODOMETRO DE HOY. Se llega con UN comando legitimo:
		// registrar la entrega es CORRECTIVO, asi que una agencia que corrige hacia arriba la lectura
		// del dia de la entrega deja el registro asi. La guarda de la linea de tiempo no lo ve,
		// porque compara lecturas contra lecturas y la de la entrega no es una de ellas.
		private static AScheduleOnARealJournal AVehicleWhoseOdometerWentBackwards()
		{
			AScheduleOnARealJournal journal = ASealedScope("JTDKN3DU0A0123456");

			journal.Api.PublishWarrantyEnvelope(240, 20000, TheUnit, ThePublication);
			journal.Api.RecordDelivery(TheDelivery, 50000, TheUnit);
			journal.Api.RecordReading(9000, TheUnit, TheDayItWasObserved, "Telemetry");

			return journal;
		}

		private static AScheduleOnARealJournal AVehicleDeliveredInTheFuture()
		{
			AScheduleOnARealJournal journal = ASealedScope("WAUZZZ8V5KA000001");

			journal.Api.PublishWarrantyEnvelope(36, 60000, TheUnit, ThePublication);
			journal.Api.RecordDelivery(TheDeliveryThatHasNotHappenedYet, 120, TheUnit);
			journal.Api.RecordReading(9000, TheUnit, TheDayItWasObserved, "Telemetry");

			return journal;
		}

		private static AScheduleOnARealJournal ASealedScope(string vin)
		{
			var journal = new AScheduleOnARealJournal(vin, "Standard");

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, TheUnit);
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystem, "wear parts", "when the part is a consumable");
			journal.Api.SealRevision(TheRevision);

			return journal;
		}

		// Un journal con las TRES globales viejas y sin la cuarta, escrito y dejado cerrado. El host
		// es una forma HISTORICA que no va a cambiar nunca, y cada fixture de esta suite es dueno de
		// su propio aparejo.
		private static string AJournalWrittenBeforeTheProfileExisted()
		{
			string folder = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);

			var older = new AnAppFromBeforeTheVehiclesOwnRecord(
				"1HGCM82633A004352", AScheduleOnARealJournal.TheModelAtDelivery);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			var api = new DealershipCarAPI(older);

			api.OpenRevision(TheRevision, ThePublication);
			api.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, TheUnit);
			api.SealRevision(TheRevision);

			// EL RENGLON QUE HACE QUE ESTE APAREJO PUEDA DISTINGUIR ALGO: la cuarta global falta DE
			// VERDAD. Sin el, todo lo de arriba estaria verde aunque 'semilla' la hubiera creado.
			Assert.That(() => api.IsWarrantyEnvelopePublished(), Throws.Exception);

			older.Dispose();

			return folder;
		}

		private static void SweepAway(string folder)
		{
			try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		private sealed class AnAppFromBeforeTheVehiclesOwnRecord : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppFromBeforeTheVehiclesOwnRecord(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('semilla') {
						schedule = MaintenanceSchedule(@vin, @usage);
					}
					upgrade('bitacora-de-eventos') {
						logbook = Logbook();
					}
					upgrade('especificacion-del-modelo') {
						specification = ModelSpecification(@model);
					}
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
					p["model", typeof(string)] = modelCode;
				})
				.PerformCommand();
			}
		}
	}
}
