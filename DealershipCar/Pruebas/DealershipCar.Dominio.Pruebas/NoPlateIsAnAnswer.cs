using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// L6, PRUEBA 10 DE §12. Un vehiculo cuya agencia nunca publico una placa es un caso ORDINARIO y no
	// un error, y contesta "no hay placa registrada". Nunca una cadena vacia, nunca null, y NUNCA EL
	// VIN HACIENDO DE PLACA -- que es la unica de las tres que se ve razonable y la peor: un VIN
	// mostrado donde va una placa se cruzaria contra los registros de una estacion y no coincidiria
	// con nada, en silencio.
	//
	// LA AUSENCIA TIENE UNA SOLA CLASE AQUI, ASI QUE E47 NO DISPARA (plan §4.3(c)). O hay una placa
	// publicada al instante preguntado o no la hay; no hay una segunda razon estructuralmente
	// distinta. E45 -- el par de dos tipos con objeto nulo -- es toda la forma, y hacer crecer
	// variantes que solo carguen una etiqueta seria inventar clases de ausencia que el dominio no
	// tiene.
	//
	// Y E48 TAMPOCO DISPARA, que vale la pena decirlo porque dispara en todo el resto del modulo. La
	// placa NO TIENE MITAD GENERAL: es enteramente un hecho de UN vehiculo, y no existe una placa a
	// nivel de modelo que alguien sin datos de ningun carro pudiera citar. No se construye ningun
	// DescribePublishedPlate hermano; donde no hay mitad honesta, el hueco se dice CON PALABRAS, que es
	// para lo que existe la frase de la ausencia.
	[TestFixture]
	public sealed class NoPlateIsAnAnswer
	{
		private const string TheVin = ADealershipNarrative.TheVin;

		private static readonly DateTime TheDayItAsks =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		[Test]
		public void AVehicleWithNoPlateAnswersInWordsAndNeverWithAnEmptyString()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				Assert.That(
					APlateOnTheDoorAndInTheDomain.IsPublished(journal.Api, TheDayItAsks), Is.False,
					"Sin ninguna asignacion no hay placa, y la pregunta de presencia es TOTAL: "
					+ "contesta, no revienta.");

				string said = APlateOnTheDoorAndInTheDomain.Describe(journal.Api, TheDayItAsks);

				Assert.That(said, Is.Not.Empty,
					"La ausencia se DICE. Una cadena vacia obliga a quien la lee a inventar la frase, "
					+ "y cada caratula inventaria una distinta.");

				Assert.That(said, Is.Not.Null);
			}
		}

		// LA MITAD QUE NINGUNA OTRA PRUEBA MIRA. El VIN esta a mano -- el agregado se llavea por el --
		// asi que es lo primero que alguien pondria si la frase tuviera que nombrar el carro.
		[Test]
		public void AndTheVinDoesNotAppearInThatAnswer()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				string said = APlateOnTheDoorAndInTheDomain.Describe(journal.Api, TheDayItAsks);

				Assert.That(said, Does.Not.Contain(TheVin),
					"El VIN aparece donde falta la placa. Son dos hechos de forma distinta: el VIN "
					+ "IDENTIFICA y la placa DESCRIBE, y un VIN cruzado contra los registros de una "
					+ "estacion no coincide con nada -- sin error y sin match, que es el fallo que "
					+ "esta rebanada entera existe para prevenir.");

				Assert.That(said, Does.Not.Contain(TheVin.Substring(0, 8)),
					"Ni un pedazo de el.");
			}
		}

		// Y LA FRASE DICE QUIEN PUBLICA UNA PLACA. Es el lado del canal lateral que una PERSONA usa
		// (E48): quien lee la ausencia es justo quien podria ir a pedirla, y una frase que solo dice
		// "no hay" no le sirve de nada.
		[Test]
		public void AndTheSentenceSaysWhoPublishesAPlate()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				string said = APlateOnTheDoorAndInTheDomain.Describe(journal.Api, TheDayItAsks);

				Assert.That(said.ToLowerInvariant(), Does.Contain("concesionario").IgnoreCase,
					"La frase de la ausencia nombra a la agencia como quien publica una placa. Es lo "
					+ "unico accionable que tiene quien la lee.");
			}
		}

		// LOS DOS ACCESORES GUARDADOS RECHAZAN EN VEZ DE INVENTAR. Devolver "" o el VIN seria E47 en
		// su tercer anti-patron: un miembro legitimo del conjunto de respuestas devuelto para un
		// estado que no se puede contestar, que todo lector aguas abajo repite como hecho.
		[Test]
		public void TheTwoGuardedReadsRefuseInsteadOfInventingAPlate()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				// EL TIPO SE AFIRMA, y no alcanza con que ALGO reviente. Mientras el miembro no
				// exista, el andamio de LP-0 revienta con una AssertionException -- asi que un
				// Assert.Catch pelado estaria VERDE por el fallo del andamio y se pondria a decir
				// que el dominio rechaza cuando el dominio ni siquiera esta. Es la misma forma que la
				// puerta ya usa para toda lectura guardada.
				Assert.Catch<InvalidOperationException>(
					() => APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, TheDayItAsks),
					"Sin placa no hay texto publicado. Una cadena vacia parece una respuesta.");

				Assert.Catch<InvalidOperationException>(
					() => APlateOnTheDoorAndInTheDomain.JoinKeyAsOf(journal.Api, TheDayItAsks),
					"Y sin placa no hay llave. Una llave vacia cruzaria contra la estacion y "
					+ "coincidiria con cualquier cosa o con nada, sin decir cual de las dos.");
			}
		}

		// Y LA ENUMERACION CONTESTA VACIA, que NO es lo mismo. Cero filas es una respuesta completa a
		// "cuales asignaciones hay"; cero placa no es una respuesta a "cual es la placa".
		[Test]
		public void AndTheRowsAnswerWithNoRowsWhichIsADifferentThingFromRefusing()
		{
			using (AScheduleOnARealJournal journal = AJournal())
				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AsPublished"),
					Is.Empty,
					"La enumeracion es TOTAL: preguntar cuales hay cuando no hay ninguna se contesta "
					+ "con ninguna. Es el otro lado de E41 y no contradice los accesores guardados de "
					+ "arriba.");
		}

		private static AScheduleOnARealJournal AJournal()
		{
			return new AScheduleOnARealJournal(TheVin, "Standard");
		}
	}
}
