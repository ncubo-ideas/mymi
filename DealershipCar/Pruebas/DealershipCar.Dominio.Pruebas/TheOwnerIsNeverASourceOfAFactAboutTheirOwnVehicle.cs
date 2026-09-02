using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
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
	//   externo: todos int o DateTime. Hay exactamente DOS excepciones y las dos son claves de
	//   catalogo y no hechos de ningun carro -- 'asOf', el instante en que se pregunta, y un int
	//   'milestone…', que dice CUAL precio publicado se esta pidiendo.
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
		private static readonly string[] EveryCommandTheDealershipHas =
		{
			"SeedPlanOperation", "AdoptPlanRevision", "AddOwnerOperation", "RemoveOwnerOperation",
			"DeclareUsageProfile", "DeferOperation", "RecordReading", "RecordConditionThreshold",
			"CompleteOperation", "RegisterDevice", "StoreCredential",
			"OpenRevision", "PublishFluidSpecification", "PublishTyreSpecification",
			"PublishFuelGrade", "PublishCoveredSystem", "PublishCoverageExclusion",
			"PublishServiceInterval", "PublishServicePrice", "PublishServicingPolicy",
			"SealRevision", "SupersedeRevision",
			"RecordDelivery", "PublishWarrantyEnvelope",
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
			"DescribeServiceInterval/3(distanceRemaining)",

			"DescribeServicingElsewhereRule/3(externalServicesWithEvidence)",
			"DescribeServicingElsewhereRule/3(externalServicesWithoutEvidence)",
			"DescribeServicingElsewhereRule/3(missedMaintenanceCount)",

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

			"DescribeCoverage/9(envelopeMonths)",
			"DescribeCoverage/9(envelopeDistance)",
			"DescribeCoverage/9(deliveredAt)",
			"DescribeCoverage/9(deliveryReading)",
			"DescribeCoverage/9(currentReading)",
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

		// Y LAS TRES DE COBERTURA TOMAN NUEVE Y NO DIEZ. Es el renglon que atrapa un decimo argumento
		// antes que el censo, y con un mensaje que dice lo que paso: la aritmetica de "nueve hechos"
		// esta escrita en cuatro documentos y en el /// de la puerta, y crecer sin decirlo la
		// desmiente en silencio.
		[Test]
		public void EachCoverageReadStillAsksForExactlyNineFacts()
		{
			foreach (string each in new[]
				{ "CanCoverageBeEvaluated", "ReadCoverageStanding", "DescribeCoverage" })
				Assert.That(TheDoorsMember(each).GetParameters().Length, Is.EqualTo(9),
					$"'{each}' dejo de tomar nueve. Las tres son legitimas porque piden el escenario "
					+ "COMPLETO: quien las llama monta uno entero. Una que pidiera OCHO y derivara el "
					+ "noveno estaria componiendo con un hecho que nadie le dio.");
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

		// Y LAS DOS CLAVES DE CATALOGO SIGUEN SIENDO DOS. Es lo que impide que el censo de mediciones
		// se vacie por la puerta de atras: agrandar la lista de claves esconde un hecho en vez de
		// censarlo, y agrandarla tiene que costar un rojo.
		[Test]
		public void TheOnlyNumbersOrInstantsAQuestionMayTakeWithoutBeingAMeasurementAreTwo()
		{
			Assert.That(TheTwoCatalogueKeys, Has.Length.EqualTo(2),
				"Si esto crecio, alguien amplio lo que cuenta como clave y no como hecho. Se decide a "
				+ "proposito, con el motivo escrito, o no se decide.");

			Assert.That(TheTwoCatalogueKeys[0], Is.EqualTo("asOf"));
			Assert.That(TheTwoCatalogueKeys[1], Is.EqualTo("milestone"));
		}

		// 'asOf' es el instante en que se PREGUNTA -- no un hecho del carro -- y 'milestone…' dice
		// CUAL precio publicado del catalogo se esta pidiendo. Las dos son de la pregunta y no del
		// vehiculo, y son las dos unicas.
		private static readonly string[] TheTwoCatalogueKeys = { "asOf", "milestone" };

		private static bool IsACatalogueKey(ParameterInfo parameter)
		{
			return parameter.Name == TheTwoCatalogueKeys[0]
				|| parameter.Name.StartsWith(TheTwoCatalogueKeys[1], StringComparison.Ordinal);
		}

		// Una MEDICION es un numero o un instante que no es clave de catalogo. Los NOMBRES de esta
		// puerta son cadenas -- miembro cerrado, revision, sistema, unidad -- y las mediciones son
		// numeros e instantes, asi que el tipo es el que discrimina y no el nombre.
		private static bool IsAMeasurement(ParameterInfo parameter)
		{
			if (IsACatalogueKey(parameter)) return false;

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
			var found = new List<KeyValuePair<string, string>>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (each.ReturnType == typeof(void)) continue;

				ParameterInfo[] parameters = each.GetParameters();

				foreach (ParameterInfo parameter in parameters)
					if (IsAMeasurement(parameter))
						found.Add(new KeyValuePair<string, string>(
							each.Name + "/" + parameters.Length + "(" + parameter.Name + ")",
							each.Name));
			}

			return found;
		}
	}
}
