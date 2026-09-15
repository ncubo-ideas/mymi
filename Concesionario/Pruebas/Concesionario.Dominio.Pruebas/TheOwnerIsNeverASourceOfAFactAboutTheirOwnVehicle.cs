using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA PRUEBA 2 DE §11 DEL BRIEF, Y LA LEY L1: el sobre, el instante de entrega y la lectura de
	// entrega llegan DE LA AGENCIA -- superficie T, por comando y por feed. El dueno no es fuente de
	// un hecho de su propio vehiculo: ni por un parametro de una tool, ni por un default, ni por un
	// formulario en una pantalla.
	//
	// ESCRITA LITERAL, ESTA PRUEBA ESTA ROJA EL PRIMER DIA. CanCoverageBeEvaluated,
	// ReadCoverageStanding y DescribeCoverage toman NUEVE argumentos, y OCHO son exactamente los
	// hechos que L1 protege. Y se quedan: C63 establecio que una frase emparejada SI la puede pedir
	// quien esta MONTANDO UN ESCENARIO -- que es la consola, y la consola monta uno.
	//
	// ASI QUE EL BARRIDO TIENE QUE SEPARAR "las tres emparejadas de siempre" de "el parametro de
	// comodidad que alguien agrega el ano que viene". Y EL INSTRUMENTO OBVIO ES EL QUE C57 PROHIBE:
	// una lista de miembros permitidos crece en silencio, y el dia que alguien le agregue un DECIMO
	// argumento a una lectura que ya esta en la lista, el barrido reporta una cobertura que no tiene.
	//
	// LO QUE SE TOMO, Y POR QUE. El discriminador NO es un vocabulario de nombres de parametro --
	// quien agrega el parametro de comodidad elige el nombre, asi que reconocer nombres de hecho es
	// un proxy que se rompe justo cuando importa. Es la FORMA del miembro:
	//
	//   una puerta de SOLO LECTURA tiene dos clases de miembro. Uno que REGISTRA -- devuelve void,
	//   es superficie T, y puede tomar cualquier hecho porque registrarlo es su oficio. Y uno que
	//   PREGUNTA -- devuelve un valor, y pregunta con CLAVES, nunca con MEDICIONES.
	//
	//   en las claves de esta puerta los NOMBRES son cadenas y las MEDICIONES son numeros e
	//   instantes. Un miembro cerrado, una revision, un sistema, una unidad: todos string. El
	//   odometro, los dos limites del sobre, el instante de entrega, los tres conteos de servicio
	//   externo: todos int o DateTime. Las excepciones son claves de catalogo y no hechos de ningun
	//   carro -- 'asOf', el instante en que se pregunta; un int 'milestone…', que dice CUAL precio
	//   publicado se esta pidiendo; y los SEIS EXTREMOS DE PERIODO que BuildOrder/OD-2 agrego.
	//
	// LOS SEIS EXTREMOS DE PERIODO SON UNA AMPLIACION DELIBERADA DE LAS CLAVES, escrita el mismo dia
	// y en el mismo commit que las lecturas que la piden, y con un renglon aparte que vuelve a correr
	// las DOS claves de antes contra esta puerta y las deja rojas. Sin ese renglon, ampliar la lista
	// de claves se ve exactamente igual que esconder un hecho: el censo de mediciones se vaciaria por
	// la puerta de atras y nada diria que se vacio a proposito.
	//
	// LA DISTINCION QUE LA DECIDE, y es la misma que se tomo antes en el borde del asistente. 'asOf'
	// es EL INSTANTE EN QUE SE MIDE UNA SITUACION -- un hecho, y uno inventado produce una situacion
	// falsa. Un extremo de periodo es CUALES LECTURAS LISTAR: elige la pregunta, no afirma nada del
	// carro, y LA RESPUESTA NOMBRA LOS EXTREMOS QUE USO, asi que un extremo inventado produce una
	// respuesta visiblemente acotada y nunca un hecho falso.
	//
	// Y LA MITAD QUE HACE QUE SEA UNA DECISION Y NO UNA ESCAPATORIA: los treinta y cuatro pares que la
	// ampliacion deja pasar se ENUMERAN, exactos, con su miembro. Las claves se amplian por NOMBRE en
	// toda la puerta, pero los sitios donde ese nombre aparece estan escritos: un 'DateTime from'
	// nuevo en una lectura que no sea una de las SEIS de la serie, una de las SIETE del libro o una de
	// las DOS del cruce se pone rojo igual.
	//
	// ASI QUE EL CENSO SE ARMA POR ELIMINACION Y NO POR RECONOCIMIENTO: todo int y todo DateTime de
	// una PREGUNTA que no sea una de esas dos claves es una medicion, y el censo se afirma como
	// CONJUNTO EXACTO. Un parametro nuevo se pone rojo con CUALQUIER nombre. Un decimo argumento en
	// una de las tres de siempre se pone rojo tambien, que es justo lo que la lista de miembros no
	// veria.
	//
	// LO QUE ESTE INSTRUMENTO NO VE, dicho y no disimulado: un numero pasado como TEXTO. Nada en esta
	// puerta lo hace -- ni TheText ni WireName sirven para eso -- pero si alguien escribiera
	// DescribeAlgo(string odometer), el barrido lo leeria como un nombre. Es el unico hueco, y queda
	// escrito en vez de tapado con un vocabulario que tendria varios mas.
	[TestFixture]
	public sealed class TheOwnerIsNeverASourceOfAFactAboutTheirOwnVehicle
	{
		// ===== LA MITAD QUE REGISTRA: EL CENSO ENTERO, POR NOMBRE =========================

		// TODA ESCRITURA DE LA PUERTA, ENUMERADA. Es la unica lista escrita a mano de este fixture y
		// lo es a proposito: la app del dueno es de SOLO LECTURA -- Amendment 2 retiro los setenta y
		// cinco comandos del dueno -- asi que un comando nuevo, cualquiera, es una decision y no un
		// detalle. Con el censo exacto, quien agregue uno tiene que escribirlo aca y, al escribirlo,
		// decidir si es superficie T.
		//
		// NO ES UNA LISTA DE PERMITIDOS QUE CRECE SOLA: es igualdad de conjuntos, asi que tambien se
		// pone rojo si alguien BORRA uno, y el rojo dice cual.
		//
		// LOS DOS DE BuildOrder/CL-4 SON GASTOS QUE UNA SUCURSAL REGISTRO, y por eso entran aqui y no
		// del lado de las preguntas. Un gasto es un hecho ocurrido con autor: la sucursal cobro, y lo
		// que llega es lo que cobro. El dueno no lo inventa ni lo estima -- la app del dueno sigue
		// siendo de SOLO LECTURA, Amendment 2 -- y el que un comando tome un monto y un instante no
		// es el dueno siendo fuente de un hecho de su propio vehiculo: es la agencia registrando el
		// suyo, que es lo que un miembro que REGISTRA hace por oficio. La forma que separa las dos
		// cosas es la del miembro y no el nombre de sus parametros: estos dos devuelven void.
		//
		// Y EL DE LicensePlate/LP-2 ES EL MISMO CASO, con la tentacion en su forma mas fuerte: la placa
		// es el unico hecho de este vehiculo que el dueno TIENE A LA VISTA, asi que un parametro que la
		// pida se ve razonable. Entra aca porque REGISTRA -- devuelve void, superficie F/X -- y quien
		// la publica es el concesionario. Que sea el UNICO miembro de la puerta que toma una placa lo
		// afirma APlateIsReceivedAndNeverAsked por nombre, porque este censo mira la FORMA y una placa
		// es un string, que de este lado se lee como una clave.
		private static readonly string[] EveryCommandTheDealershipHas =
		{
			"SeedPlanOperation", "AdoptPlanRevision", "AddOwnerOperation", "RemoveOwnerOperation",
			"DeclareUsageProfile", "DeferOperation", "RecordReading", "RecordConditionThreshold",
			// RegisterDevice y StoreCredential ya no estan en la puerta (2026-09-03: la credencial
			// salio del journal), asi que salen de este censo. No cambia lo que el censo afirma:
			// ninguno de los dos tomaba un hecho del vehiculo.
			"CompleteOperation",
			"OpenRevision", "PublishFluidSpecification", "PublishTyreSpecification",
			"PublishFuelGrade", "PublishCoveredSystem", "PublishCoverageExclusion",
			"PublishServiceInterval", "PublishServicePrice", "PublishServicingPolicy",
			"SealRevision", "SupersedeRevision",
			"RecordDelivery", "PublishWarrantyEnvelope", "RecordPlateAssignment",
			"RecordShopEntry", "RecordShopCollection", "RecordShopStay",
			"RecordRecurringCost", "RecordDurableCost",
		};

		[Test]
		public void TheDoorWritesThroughTheseCommandsAndNoOthers()
		{
			Assert.That(EveryRecording(), Is.EquivalentTo(EveryCommandTheDealershipHas),
				"La puerta gano o perdio una escritura. Toda escritura de este modulo es superficie T "
				+ "-- la agencia -- y una nueva se escribe aca a proposito, porque la app del dueno no "
				+ "escribe nada.");
		}

		// Y LOS DOS DE T4 SON ESCRITURAS, no lecturas con efecto. Sin este renglon, el censo de
		// arriba pasaria igual si RecordDelivery devolviera algo y cayera del otro lado del corte.
		[Test]
		public void TheTwoNewFactsArriveByCommandAndNotByAnythingElse()
		{
			Assert.That(EveryRecording(), Does.Contain("RecordDelivery"));
			Assert.That(EveryRecording(), Does.Contain("PublishWarrantyEnvelope"));

			foreach (string each in new[] { "RecordDelivery", "PublishWarrantyEnvelope" })
				Assert.That(TheDoorsMember(each).ReturnType, Is.EqualTo(typeof(void)),
					$"'{each}' dejo de ser un comando. Un hecho de la agencia se REGISTRA; si esto "
					+ "devolviera algo, seria una pregunta que escribe.");
		}

		// ===== LA MITAD QUE PREGUNTA: EL CENSO DE MEDICIONES, EXACTO ======================

		// LAS DIECINUEVE MEDICIONES QUE HOY CRUZAN HACIA UNA PREGUNTA, con el miembro en que estan.
		// Las diecinueve son legitimas por la MISMA razon y es una sola: C63 -- un llamador que MONTA
		// UN ESCENARIO puede pasarlas, y los cinco miembros son exactamente las cinco lecturas
		// emparejadas que la caratula del browser tiene prohibido llamar.
		//
		// LO QUE HACE QUE ESTO NO SEA LA LISTA QUE C57 PROHIBE es que el censo es de PARES y no de
		// miembros, y que se arma por eliminacion: un decimo argumento en cualquiera de las tres de
		// cobertura es un PAR nuevo, no una entrada vieja que sigue valiendo.
		//
		// Y LA CLAVE DEL PAR LLEVA LA ARIDAD -- 'Miembro/n(parametro)' -- que es C57 al pie de la
		// letra: lo que se enumera son los SITIOS por donde la regla puede romperse y no los
		// sustantivos de los que trata. MEDIDO: con la clave sin aridad, una SOBRECARGA de diez
		// argumentos aparecia como cinco pares repetidos mas uno nuevo, y el rojo era una lista de
		// duplicados que no decia que habia dos miembros con el mismo nombre. Con la aridad, cada
		// sobrecarga es su propio sitio y el rojo lo nombra.
		private static readonly string[] EveryMeasurementAQuestionTakes =
		{
			"DescribeServiceInterval/4(distanceRemaining)",

			"DescribeServicingElsewhereRule/4(externalServicesWithEvidence)",
			"DescribeServicingElsewhereRule/4(externalServicesWithoutEvidence)",
			"DescribeServicingElsewhereRule/4(missedMaintenanceCount)",

			"CanCoverageBeEvaluated/9(envelopeMonths)",
			"CanCoverageBeEvaluated/9(envelopeDistance)",
			"CanCoverageBeEvaluated/9(deliveredAt)",
			"CanCoverageBeEvaluated/9(deliveryReading)",
			"CanCoverageBeEvaluated/9(currentReading)",

			"ReadCoverageStanding/9(envelopeMonths)",
			"ReadCoverageStanding/9(envelopeDistance)",
			"ReadCoverageStanding/9(deliveredAt)",
			"ReadCoverageStanding/9(deliveryReading)",
			"ReadCoverageStanding/9(currentReading)",

			"DescribeCoverage/10(envelopeMonths)",
			"DescribeCoverage/10(envelopeDistance)",
			"DescribeCoverage/10(deliveredAt)",
			"DescribeCoverage/10(deliveryReading)",
			"DescribeCoverage/10(currentReading)",
		};

		[Test]
		public void NoQuestionOnTheDoorTakesAMeasurementBeyondTheseNineteen()
		{
			Assert.That(EveryMeasurement(), Is.EquivalentTo(EveryMeasurementAQuestionTakes),
				"Una pregunta de la puerta gano o perdio una MEDICION -- un int o un DateTime que no "
				+ "es 'asOf' ni una clave de catalogo. ESTA ES LA PRUEBA QUE SE PONE ROJA EL DIA QUE "
				+ "ALGUIEN AGREGA EL PARAMETRO DE COMODIDAD: un odometro, un limite del sobre o un "
				+ "hecho de la entrega pedidos a quien pregunta, con cualquier nombre. Si el par "
				+ "nuevo es de verdad una clave de catalogo y no un hecho de un carro, su forma se "
				+ "agrega a TheTwoCatalogueKeys y se dice por que; si es un hecho, el registro ya lo "
				+ "lleva y la lectura lo saca de ahi en vez de pedirlo.");
		}

		// SOLO CINCO MIEMBROS LLEVAN MEDICION, y son las cinco emparejadas. El censo de arriba ya lo
		// implica; escrito aparte, el rojo dice QUE MIEMBRO en vez de que par.
		[Test]
		public void OnlyTheFivePairedReadsCarryAMeasurementAtAll()
		{
			var members = new List<string>();

			foreach (KeyValuePair<string, string> each in EveryMeasurementByMember())
				if (!members.Contains(each.Value)) members.Add(each.Value);

			Assert.That(members, Is.EquivalentTo(new[]
			{
				"DescribeServiceInterval", "DescribeServicingElsewhereRule",
				"CanCoverageBeEvaluated", "ReadCoverageStanding", "DescribeCoverage",
			}));
		}

		// Y LAS TRES DE COBERTURA SIGUEN PIDIENDO LOS MISMOS NUEVE HECHOS. Es el renglon que atrapa
		// un hecho de mas antes que el censo, y con un mensaje que dice lo que paso: la aritmetica de
		// "nueve hechos" esta escrita en cuatro documentos y en el /// de la puerta, y crecer sin
		// decirlo la desmiente en silencio.
		//
		// LA QUE COMPONE UNA FRASE TOMA ADEMAS EL IDIOMA, asi que toma diez PARAMETROS y los mismos
		// nueve HECHOS. El idioma no es un hecho de ningun vehiculo: lo pone quien pregunta, igual
		// que asOf, y por eso el renglon de abajo lo nombra en vez de contarlo. Las otras dos
		// contestan un bool y un codigo, que no tienen idioma.
		[Test]
		public void EachCoverageReadStillAsksForExactlyNineFacts()
		{
			foreach (string each in new[] { "CanCoverageBeEvaluated", "ReadCoverageStanding" })
				Assert.That(TheDoorsMember(each).GetParameters().Length, Is.EqualTo(9),
					$"'{each}' dejo de tomar nueve. Las tres son legitimas porque piden el escenario "
					+ "COMPLETO: quien las llama monta uno entero. Una que pidiera OCHO y derivara el "
					+ "noveno estaria componiendo con un hecho que nadie le dio.");

			ParameterInfo[] said = TheDoorsMember("DescribeCoverage").GetParameters();

			Assert.That(said, Has.Length.EqualTo(10),
				"'DescribeCoverage' dejo de tomar los nueve hechos mas el idioma.");
			Assert.That(said[9].Name, Is.EqualTo("language"),
				"El decimo de 'DescribeCoverage' tiene que ser el IDIOMA y ningun otro. Sin este "
				+ "renglon, un decimo hecho del vehiculo -- un odometro, un limite -- entraria con "
				+ "el mismo conteo y la ley de los nueve seguiria verde.");
		}

		// ===== EL BARRIDO NO SE SALTA NADA ================================================

		// LO QUE HACE CREIBLES A LOS DOS CENSOS: cada miembro declarado de la puerta cae en uno de los
		// dos lados y en ninguno de los dos casos se descarta. Sin esto, un tercer caso -- un miembro
		// que el corte no clasificara -- saldria del barrido sin que nada lo notara, y los dos censos
		// seguirian verdes hablando de una puerta mas chica que la que hay.
		[Test]
		public void EveryDeclaredMemberOfTheDoorFallsOnOneSideOfTheCutAndNoneIsSkipped()
		{
			int declared = TheDoorsMembers().Length;

			Assert.That(declared, Is.GreaterThan(0),
				"El barrido dejo de encontrar la puerta y no defiende nada.");

			Assert.That(EveryRecording().Count + EveryQuestion().Count, Is.EqualTo(declared),
				"Un miembro se quedo sin clasificar. El corte es void contra no-void y no tiene "
				+ "tercera rama, asi que esto solo puede fallar si el barrido dejo de mirar alguno.");

			Assert.That(EveryRecording(), Is.Not.Empty);
			Assert.That(EveryQuestion(), Is.Not.Empty);
		}

		// Y LAS CLAVES DE CATALOGO SIGUEN SIENDO OCHO. Es lo que impide que el censo de mediciones se
		// vacie por la puerta de atras: agrandar la lista de claves esconde un hecho en vez de
		// censarlo, y agrandarla tiene que costar un rojo.
		//
		// EL NOMBRE DE ESTA PRUEBA LLEVA LA CUENTA A PROPOSITO, para que ampliarla obligue a
		// renombrarla -- y renombrar una prueba es un acto. Decia "AreTwo" hasta BuildOrder/OD-2.
		[Test]
		public void TheOnlyNumbersOrInstantsAQuestionMayTakeWithoutBeingAMeasurementAreEight()
		{
			Assert.That(TheEightCatalogueKeys, Has.Length.EqualTo(8),
				"Si esto crecio, alguien amplio lo que cuenta como clave y no como hecho. Se decide a "
				+ "proposito, con el motivo escrito, o no se decide.");

			Assert.That(TheEightCatalogueKeys, Is.EqualTo(new[]
			{
				"asOf", "from", "to", "from1", "to1", "from2", "to2", "milestone",
			}));

			Assert.That(TheKeySaidAsAPrefix, Is.EqualTo("milestone"),
				"Una sola de las ocho se reconoce por PREFIJO, porque un hito cruza como par -- "
				+ "milestoneDistance y milestoneUnit. Las otras siete se reconocen enteras: 'fromDay' "
				+ "no es 'from', y que no lo sea es lo que mantiene separadas las dos formas de "
				+ "nombrar un periodo en este modulo.");
		}

		// LOS TREINTA Y CUATRO PARES QUE LA AMPLIACION DEJA PASAR, CON SU MIEMBRO, COMO CONJUNTO
		// EXACTO. Las claves se amplian por NOMBRE en toda la puerta; esto es lo que impide que ese
		// nombre sirva en cualquier parte. Un 'DateTime from' en una lectura que no sea una de las SEIS
		// de la serie, una de las SIETE del libro de costos o una de las DOS del cruce es un PAR nuevo
		// aca, y el rojo lo nombra.
		//
		// CATORCE ERAN HASTA BuildOrder/CL-5, que agrego DIECISEIS con las siete lecturas del libro, y
		// BuildOrder/CL-6 agrega CUATRO con las dos del cruce. LAS CLAVES NO SE MOVIERON NI UNA VEZ:
		// las nueve lecturas piden from/to y from1/to1/from2/to2, que ya eran seis de las OCHO claves
		// de catalogo desde OD-2. Lo que se movio es DONDE valen, que es exactamente lo que este
		// conjunto enumera.
		private static readonly string[] EveryPeriodBoundAQuestionTakes =
		{
			"CanARateOfUseBeSaid/2(from)",
			"CanARateOfUseBeSaid/2(to)",

			"ReadRateOfUseStanding/2(from)",
			"ReadRateOfUseStanding/2(to)",

			"DescribeRateOfUse/2(from)",
			"DescribeRateOfUse/2(to)",

			"DescribeTwoPeriodsOfUseCompared/4(from1)",
			"DescribeTwoPeriodsOfUseCompared/4(to1)",
			"DescribeTwoPeriodsOfUseCompared/4(from2)",
			"DescribeTwoPeriodsOfUseCompared/4(to2)",

			"ReadingsBetween/2(from)",
			"ReadingsBetween/2(to)",

			"ReadingCountBetween/2(from)",
			"ReadingCountBetween/2(to)",

			"HasAnyCostBetween/2(from)",
			"HasAnyCostBetween/2(to)",

			"DescribeSpendBetween/2(from)",
			"DescribeSpendBetween/2(to)",

			"IsSpendSaidInOneCurrencyBetween/2(from)",
			"IsSpendSaidInOneCurrencyBetween/2(to)",

			"DescribeSpendPerCurrencyBetween/2(from)",
			"DescribeSpendPerCurrencyBetween/2(to)",

			"DescribeTwoPeriodsOfSpendCompared/4(from1)",
			"DescribeTwoPeriodsOfSpendCompared/4(to1)",
			"DescribeTwoPeriodsOfSpendCompared/4(from2)",
			"DescribeTwoPeriodsOfSpendCompared/4(to2)",

			"CostEntriesBetween/2(from)",
			"CostEntriesBetween/2(to)",

			"CostEntryCountBetween/2(from)",
			"CostEntryCountBetween/2(to)",

			"CanCostPerDistanceBeSaid/2(from)",
			"CanCostPerDistanceBeSaid/2(to)",

			"DescribeCostPerDistanceBetween/2(from)",
			"DescribeCostPerDistanceBetween/2(to)",
		};

		[Test]
		public void OnlyTheFifteenReadsOfTheSeriesTheLedgerAndTheCrossingTakeAPeriodBoundAtAll()
		{
			Assert.That(EveryPeriodBound(), Is.EquivalentTo(EveryPeriodBoundAQuestionTakes),
				"Una pregunta de la puerta gano o perdio un EXTREMO DE PERIODO. Los seis nombres son "
				+ "claves de catalogo porque eligen la pregunta y la respuesta los nombra; que lo "
				+ "sean no los vuelve libres en cualquier miembro. Si el par nuevo es de verdad un "
				+ "periodo, se escribe aca; si es un hecho del carro con nombre de extremo, el "
				+ "registro ya lo lleva y la lectura lo saca de ahi en vez de pedirlo.");

			var members = new List<string>();

			foreach (KeyValuePair<string, string> each in EveryPeriodBoundByMember())
				if (!members.Contains(each.Value)) members.Add(each.Value);

			Assert.That(members, Is.EquivalentTo(new[]
			{
				"CanARateOfUseBeSaid", "ReadRateOfUseStanding", "DescribeRateOfUse",
				"DescribeTwoPeriodsOfUseCompared", "ReadingsBetween", "ReadingCountBetween",
				"HasAnyCostBetween", "DescribeSpendBetween", "IsSpendSaidInOneCurrencyBetween",
				"DescribeSpendPerCurrencyBetween", "DescribeTwoPeriodsOfSpendCompared",
				"CostEntriesBetween", "CostEntryCountBetween",
				"CanCostPerDistanceBeSaid", "DescribeCostPerDistanceBetween",
			}),
				"Y son QUINCE MIEMBROS -- SEIS de la serie del odometro, SIETE del libro de costos y "
				+ "DOS del cruce entre las dos globales. El censo de pares ya lo implica; escrito "
				+ "aparte, el rojo dice que miembro en vez de que par.");
		}

		// LA AMPLIACION, HECHA VISIBLE. Las DOS claves de antes se vuelven a correr contra esta
		// puerta y se afirma que ya NO alcanzan: con ellas, los treinta y cuatro extremos de periodo
		// son mediciones y el censo de mediciones seria de CINCUENTA Y TRES.
		//
		// SIN ESTE RENGLON, LA AMPLIACION SE VE IGUAL QUE UN HUECO QUE DEJO DE NOTARSE: la lista de
		// claves habria crecido seis nombres y nada diria que crecio a proposito. Con el, quien
		// vuelva a estrechar las claves a dos tiene que borrar esta prueba, y borrarla es un acto.
		private static readonly string[] TheKeysBeforeTheWidening = { "asOf", "milestone" };

		[Test]
		public void ThePreWideningKeysAreRedAgainstThisDoor()
		{
			Assert.That(TheKeysBeforeTheWidening, Has.Length.EqualTo(2));

			List<string> withTheOldKeys = EveryMeasurementUnder(TheKeysBeforeTheWidening);

			Assert.That(withTheOldKeys, Is.Not.EquivalentTo(EveryMeasurementAQuestionTakes),
				"Con las dos claves de antes, la puerta vuelve a contestar exactamente las "
				+ "diecinueve mediciones de siempre. Si eso es a proposito, la ampliacion se retira "
				+ "entera -- encabezado, claves, el nombre de la prueba que las cuenta y el censo de "
				+ "extremos -- y no solo esta prueba.");

			Assert.That(
				withTheOldKeys,
				Has.Count.EqualTo(
					EveryMeasurementAQuestionTakes.Length + EveryPeriodBoundAQuestionTakes.Length),
				"Y la diferencia son exactamente los treinta y cuatro extremos de periodo: diecinueve "
				+ "mas treinta y cuatro son las cincuenta y tres que las claves de antes contarian.");

			foreach (string bound in EveryPeriodBoundAQuestionTakes)
			{
				Assert.That(withTheOldKeys, Does.Contain(bound));
				Assert.That(EveryMeasurementAQuestionTakes, Does.Not.Contain(bound));
			}
		}

		// 'asOf' es el instante en que se PREGUNTA -- no un hecho del carro -- 'milestone…' dice CUAL
		// precio publicado del catalogo se esta pidiendo, y los seis extremos dicen QUE PERIODO se
		// esta listando. Las ocho son de la pregunta y no del vehiculo, y son las ocho unicas.
		private static readonly string[] TheEightCatalogueKeys =
		{
			"asOf", "from", "to", "from1", "to1", "from2", "to2", "milestone",
		};

		// La unica que se reconoce por PREFIJO, porque un hito cruza como par. Las otras siete se
		// reconocen enteras: un 'fromDay' no es un 'from'.
		private const string TheKeySaidAsAPrefix = "milestone";

		private static bool IsACatalogueKey(ParameterInfo parameter)
		{
			return IsACatalogueKeyUnder(parameter, TheEightCatalogueKeys);
		}

		private static bool IsACatalogueKeyUnder(ParameterInfo parameter, string[] keys)
		{
			foreach (string each in keys)
			{
				if (each == TheKeySaidAsAPrefix)
				{
					if (parameter.Name.StartsWith(each, StringComparison.Ordinal)) return true;

					continue;
				}

				if (parameter.Name == each) return true;
			}

			return false;
		}

		// Un EXTREMO DE PERIODO: un instante cuyo nombre es uno de los seis. Se reconoce por nombre
		// entero y por tipo, las dos cosas -- un 'from' que dejara de ser un DateTime seria otra
		// clase de parametro y tendria que decir cual.
		private static bool IsAPeriodBound(ParameterInfo parameter)
		{
			if (parameter.ParameterType != typeof(DateTime)) return false;

			foreach (string each in TheEightCatalogueKeys)
				if (each != TheKeySaidAsAPrefix && each != "asOf" && parameter.Name == each)
					return true;

			return false;
		}

		// Una MEDICION es un numero o un instante que no es clave de catalogo. Los NOMBRES de esta
		// puerta son cadenas -- miembro cerrado, revision, sistema, unidad -- y las mediciones son
		// numeros e instantes, asi que el tipo es el que discrimina y no el nombre.
		private static bool IsAMeasurement(ParameterInfo parameter)
		{
			return IsAMeasurementUnder(parameter, TheEightCatalogueKeys);
		}

		private static bool IsAMeasurementUnder(ParameterInfo parameter, string[] keys)
		{
			if (IsACatalogueKeyUnder(parameter, keys)) return false;

			return parameter.ParameterType == typeof(int)
				|| parameter.ParameterType == typeof(DateTime);
		}

		private static MethodInfo[] TheDoorsMembers()
		{
			return typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		}

		// EL MIEMBRO, Y REVIENTA SI HAY DOS CON ESE NOMBRE. Devolver el primero seria el defecto que
		// C57 nombra: un item leido por dos sobrecargas se afirma una vez y el barrido reporta una
		// cobertura que no tiene. Si aparece una sobrecarga, esto lo dice en vez de elegir.
		private static MethodInfo TheDoorsMember(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (each.Name == name) found.Add(each);

			Assert.That(found, Has.Count.EqualTo(1),
				$"La puerta tiene {found.Count} miembros llamados '{name}'. Con dos, afirmar sobre "
				+ "'el' miembro es elegir uno y no mirar el otro.");

			return found[0];
		}

		private static List<string> EveryRecording()
		{
			var names = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (each.ReturnType == typeof(void)) names.Add(each.Name);

			return names;
		}

		private static List<string> EveryQuestion()
		{
			var names = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (each.ReturnType != typeof(void)) names.Add(each.Name);

			return names;
		}

		private static List<string> EveryMeasurement()
		{
			var pairs = new List<string>();

			foreach (KeyValuePair<string, string> each in EveryMeasurementByMember())
				pairs.Add(each.Key);

			return pairs;
		}

		private static List<KeyValuePair<string, string>> EveryMeasurementByMember()
		{
			return EveryPairWhere(parameter => IsAMeasurement(parameter));
		}

		// El mismo censo, corrido bajo OTRO juego de claves. Es lo que hace que la ampliacion se
		// pueda enseñar en vez de solo afirmar: con las dos claves de antes, el censo cuenta
		// cincuenta y tres.
		//
		// ESTA CUENTA DECIA "TREINTA Y TRES" Y LLEVABA UNA REBANADA ENTERA STALE. Era cierta hasta
		// BuildOrder/OD-2 -- 19 + 14 -- y CL-5 movio las dos cuentas del cuerpo de la prueba de arriba
		// y se dejo esta, que esta en un helper y no al lado de lo que cuenta. Es el mismo defecto que
		// el encabezado de EveryScriptOfTheDoorHasBeenRun documenta de si mismo, en un segundo archivo:
		// una cuenta afirmada en prosa envejece en silencio y la suite no la puede ver.
		private static List<string> EveryMeasurementUnder(string[] keys)
		{
			var pairs = new List<string>();

			foreach (KeyValuePair<string, string> each
				in EveryPairWhere(parameter => IsAMeasurementUnder(parameter, keys)))
				pairs.Add(each.Key);

			return pairs;
		}

		private static List<string> EveryPeriodBound()
		{
			var pairs = new List<string>();

			foreach (KeyValuePair<string, string> each in EveryPeriodBoundByMember())
				pairs.Add(each.Key);

			return pairs;
		}

		private static List<KeyValuePair<string, string>> EveryPeriodBoundByMember()
		{
			return EveryPairWhere(parameter => IsAPeriodBound(parameter));
		}

		// Todo par (miembro/aridad, parametro) de una PREGUNTA que cumple la condicion. La clave
		// lleva la aridad porque lo que se enumera son los SITIOS por donde la regla puede romperse:
		// una sobrecarga es su propio sitio y el rojo la nombra.
		private static List<KeyValuePair<string, string>> EveryPairWhere(
			Func<ParameterInfo, bool> condition)
		{
			var found = new List<KeyValuePair<string, string>>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (each.ReturnType == typeof(void)) continue;

				ParameterInfo[] parameters = each.GetParameters();

				foreach (ParameterInfo parameter in parameters)
					if (condition(parameter))
						found.Add(new KeyValuePair<string, string>(
							each.Name + "/" + parameters.Length + "(" + parameter.Name + ")",
							each.Name));
			}

			return found;
		}
	}
}
