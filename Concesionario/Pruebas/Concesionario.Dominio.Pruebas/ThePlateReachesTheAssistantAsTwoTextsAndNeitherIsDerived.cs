using System;
using System.Globalization;
using System.Text.Json;
using Concesionario.Mcp;
using Concesionario.Mcp.Tools;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// L7 Y L8 EN EL BORDE DEL ASISTENTE: los dos textos llegan, cada uno desde SU lectura de la
	// puerta, y ninguno se deriva del otro ni se rearma en la caratula.
	//
	// POR QUE EXISTE ESTE ARCHIVO, Y LA MEDICION QUE LO PIDIO. LP-4 falsifico la tool a mano contra un
	// servidor vivo: con ReadPlateJoinKey de la puerta atado a ReadPlateAsPublished -- un renglon --,
	// joinKey volvio "CJP 991" con el espacio adentro, la respuesta NO reventó, nada quedo en rojo y
	// LA SUITE ENTERA SIGUIO VERDE. Ese es exactamente el modo de falla que §1.3 del brief describe:
	// el cruce no da error, simplemente no coincide, y se lee igual que un carro que nunca visito la
	// otra estacion. Un defecto que ninguna prueba ve es un defecto que se despacha.
	//
	// Y ESTE FIXTURE ES EL QUE FALTABA POR SIMETRIA. Cada una de las rebanadas anteriores le dejo a su
	// tool un The<X>ReachesTheAssistant...: el sobre, la cuenta atras, el ritmo y el gasto lo tienen.
	// La placa no lo tenia.
	//
	// LO QUE NO SE ESCRIBE ACA, A PROPOSITO. Que ningun parametro de ninguna tool sea una placa NO se
	// vuelve a afirmar: lo afirma TheStandingReachesTheAssistantAndNoToolAsksForIt por el conjunto
	// EXACTO de parametros, y APlateIsReceivedAndNeverAsked afirma que ese censo sigue existiendo. El
	// brief pide que se apoye en el instrumento que ya esta, no que se le ponga uno paralelo al lado.
	[TestFixture]
	public sealed class ThePlateReachesTheAssistantAsTwoTextsAndNeitherIsDerived
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string AVehicleWithNoPlate = "1FTFW1ET5DFA00001";

		// LAS DOS PLACAS LLEVAN SEPARADORES DISTINTOS a proposito -- guion y espacio -- porque una sola
		// no distinguiria una normalizacion que quita guiones de una que normaliza. Y las dos son de un
		// instante PASADO y se quedan pasadas: la tool lee el reloj del anfitrion, asi que un instante
		// futuro haria que esta prueba dependiera del dia en que corre la suite.
		private const string TheFirstPlateAsPublished = "BQR-482";
		private const string TheFirstJoinKey = "BQR482";

		private const string TheCurrentPlateAsPublished = "CJP 991";
		private const string TheCurrentJoinKey = "CJP991";

		private static readonly DateTime TheDayTheFirstWasAssigned =
			new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayTheCurrentOneWasAssigned =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void AVehicleThatHasBeenRePlatedOnce()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordPlateAssignment(
				TheFirstPlateAsPublished, TheDayTheFirstWasAssigned);
			journal.Api.RecordPlateAssignment(
				TheCurrentPlateAsPublished, TheDayTheCurrentOneWasAssigned);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LOS DOS TEXTOS Y LA FRASE SON LOS DE LA PUERTA, LETRA POR LETRA. El instante se saca de la
		// PROPIA respuesta, porque la tool lee el reloj y nadie mas puede saber cual uso.
		[Test]
		public void EachTextTheAssistantGetsIsItsOwnDoorReadLetterForLetter()
		{
			PlateOfThisCar answer = TheAssistantAsksForThePlate();

			DateTime asOf = DateTime.Parse(
				answer.AskedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

			Assert.That(answer.HasData, Is.True);
			Assert.That(answer.PlateKnown, Is.True);
			Assert.That(answer.Vin, Is.EqualTo(TheVin));
			Assert.That(answer.PlatePublished, Is.True);

			Assert.That(answer.AsPublished, Is.EqualTo(journal.Api.ReadPlateAsPublished(asOf)));
			Assert.That(answer.JoinKey, Is.EqualTo(journal.Api.ReadPlateJoinKey(asOf)));
			Assert.That(answer.Says, Is.EqualTo(journal.Api.DescribePlate(asOf)));
		}

		// EL RENGLON QUE ATRAPA EL DEFECTO QUE SE MIDIO, y no lo atrapa el de arriba. Comparar cada
		// campo contra la lectura de la puerta que le corresponde deja pasar que la tool este atada a
		// la lectura EQUIVOCADA de las dos, porque entonces las dos comparaciones se hacen contra la
		// misma lectura y las dos pasan. Lo que discrimina es que los dos textos sean DISTINTOS entre
		// si: la placa publicada lleva un separador y la llave no.
		[Test]
		public void TheKeyIsNotThePublishedTextAndTheTwoAreNamedByValue()
		{
			PlateOfThisCar answer = TheAssistantAsksForThePlate();

			Assert.That(answer.AsPublished, Is.EqualTo(TheCurrentPlateAsPublished));
			Assert.That(answer.JoinKey, Is.EqualTo(TheCurrentJoinKey));

			Assert.That(answer.JoinKey, Is.Not.EqualTo(answer.AsPublished),
				"joinKey volvio igual que asPublished. MEDIDO EN LP-4 contra un servidor vivo: con la "
				+ "lectura de la puerta atada a la otra, esto pasa sin ningun error y el cruce contra "
				+ "el otro sistema deja de coincidir EN SILENCIO, que es la falla que L2 y §1.3 "
				+ "existen para atajar. Los dos textos salen de DOS lecturas distintas.");
		}

		// LAS DOS ASIGNACIONES SIGUEN LEGIBLES Y LA VIGENTE LA DECIDE LA PUERTA (L5). La anterior no se
		// pierde y no se corrige: es un hecho con su propio instante.
		[Test]
		public void BothAssignmentsArriveAsRowsAndTheLaterOneIsTheCurrent()
		{
			PlateOfThisCar answer = TheAssistantAsksForThePlate();

			Assert.That(answer.Assignments, Has.Count.EqualTo(2));

			Assert.That(answer.Assignments[0].AsPublished, Is.EqualTo(TheFirstPlateAsPublished));
			Assert.That(answer.Assignments[0].JoinKey, Is.EqualTo(TheFirstJoinKey));
			Assert.That(answer.Assignments[0].AssignedAt, Is.EqualTo("2026-01-15"));
			Assert.That(answer.Assignments[0].IsCurrent, Is.EqualTo("false"));

			Assert.That(answer.Assignments[1].AsPublished, Is.EqualTo(TheCurrentPlateAsPublished));
			Assert.That(answer.Assignments[1].JoinKey, Is.EqualTo(TheCurrentJoinKey));
			Assert.That(answer.Assignments[1].AssignedAt, Is.EqualTo("2026-06-01"));
			Assert.That(answer.Assignments[1].IsCurrent, Is.EqualTo("true"));

			// Y SE LE DICE AL ASISTENTE QUE UNA PLACA NUEVA NO CORRIGE A LA ANTERIOR. Sin esa frase, un
			// modelo que ve dos filas puede reportar la primera como un error del registro.
			Assert.That(answer.Detail, Does.Contain("no corrige a la anterior"));
		}

		// LA AUSENCIA CONTESTA CON PALABRAS Y NO REVIENTA, y esa es la mitad que la ORDEN de las
		// llamadas sostiene: las dos lecturas de texto de la puerta son GUARDADAS y revientan sin
		// placa, asi que la tool pregunta primero. MEDIDO EN LP-4: con IsAPlatePublished contestando
		// siempre que si, esta misma llamada vuelve como isError del protocolo, que es lo que un
		// asistente le reporta a la persona como "la herramienta fallo".
		[Test]
		public void AVehicleWithNoPlateIsAnsweredInWordsAndNeverThrown()
		{
			using (var bare = new AScheduleOnARealJournal(AVehicleWithNoPlate, "Standard"))
			{
				PlateOfThisCar answer =
					new LicensePlateTools(new JournalReader(bare.Folder, "")).WhatPlateMyCarHas();

				Assert.That(answer.HasData, Is.True);
				Assert.That(answer.PlateKnown, Is.True);
				Assert.That(answer.PlatePublished, Is.False);

				Assert.That(answer.AsPublished, Is.Empty);
				Assert.That(answer.JoinKey, Is.Empty);
				Assert.That(answer.Assignments, Is.Empty);

				Assert.That(answer.Says, Is.Not.Empty,
					"La constancia de que no hay placa la compone el DOMINIO y es una respuesta.");
				Assert.That(answer.Detail, Does.Contain("NUNCA conteste el VIN en su lugar"));

				// L6: EL VIN NO SE PUEDE COLAR DONDE VA UNA PLACA. Se afirma sobre la respuesta ENTERA
				// serializada y no sobre says: un vin cruzado contra los registros de otro sistema no
				// coincide con nada y no avisa, asi que lo que importa es que no aparezca en ningun
				// campo que alguien pueda leer como la placa. El campo vin propio se excluye a mano
				// porque ese SI dice de que vehiculo habla la respuesta.
				string everythingButTheVinField =
					JsonSerializer.Serialize(answer).Replace("\"vin\":\"" + AVehicleWithNoPlate + "\"", "");

				Assert.That(everythingButTheVinField, Does.Not.Contain(AVehicleWithNoPlate),
					"El VIN aparecio en algun otro campo de una respuesta sin placa. Un VIN puesto "
					+ "donde va una placa se cruza contra el otro sistema y no coincide con nada, en "
					+ "silencio, que es peor que no contestar.");
			}
		}

		// ------------------------------------------------------ el aparejo

		private PlateOfThisCar TheAssistantAsksForThePlate()
		{
			return new LicensePlateTools(new JournalReader(journal.Folder, "")).WhatPlateMyCarHas();
		}
	}
}
