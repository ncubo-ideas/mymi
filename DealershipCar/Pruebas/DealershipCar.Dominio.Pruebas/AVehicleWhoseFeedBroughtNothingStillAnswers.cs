using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA POLARIDAD PARTIDA DE E41 SOBRE LAS LECTURAS DEL ODOMETRO, y la respuesta que T6 va a
	// necesitar para elegir entre los dos compositores del perfil.
	//
	// UN VEHICULO CUYO FEED NO TRAJO NADA CONTESTA false Y NO REVIENTA. Eso es una respuesta
	// LEGAL, no un error: la agencia todavia no mando ninguna lectura, y preguntar por el odometro
	// de un carro recien entregado es una pregunta razonable con una respuesta razonable.
	//
	// Y LAS CUATRO LECTURAS QUE ENTREGAN EL HECHO REVIENTAN, porque ahi la ausencia es la ausencia
	// DE LA COSA QUE SE PIDE y no un rango vacio: un 0 se leeria como un odometro en cero -- un
	// carro sin recorrer -- y contra un sobre de garantia eso es lo mas parecido a una promesa que
	// este modulo podria escribir.
	//
	// LA SEGUNDA MITAD ES LA QUE HACE FALTA PARA QUE LA PRIMERA VALGA ALGO: la ausencia esta
	// CLAVEADA AL INSTANTE. Una agenda con una lectura observada DESPUES del instante preguntado
	// contesta lo mismo que una vacia, y por el mismo motivo. Sin este caso, la prueba de arriba
	// pasaria igual con una lectura que solo mirara si la lista esta vacia.
	[TestFixture]
	public sealed class AVehicleWhoseFeedBroughtNothingStillAnswers
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TheReading = 35000;

		private static readonly DateTime TheInstantAsked = new DateTime(2026, 6, 15);
		private static readonly DateTime ObservedLater = new DateTime(2026, 9, 20);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
		}

		[Test]
		public void ThePresenceReadAnswersFalseAndDoesNotThrow()
		{
			Assert.DoesNotThrow(() => { bool _ = schedule.HasAnOdometerReading(TheInstantAsked); });
			Assert.That(schedule.HasAnOdometerReading(TheInstantAsked), Is.False,
				"Un vehiculo cuyo feed no trajo nada contesta que no hay, y eso es una respuesta "
				+ "legal. Reventar aqui obligaria a T6 a atrapar una excepcion para elegir entre "
				+ "los dos compositores del perfil.");
		}

		[Test]
		public void EachAccessorThrowsAndSaysWhatToAskFirst()
		{
			AssertItRefusesAndSaysWhatToAskFirst(() => { int _ = schedule.ReadOdometer(TheInstantAsked); });
			AssertItRefusesAndSaysWhatToAskFirst(
				() => { string _ = schedule.ReadOdometerUnit(TheInstantAsked); });
			AssertItRefusesAndSaysWhatToAskFirst(
				() => { DistanceUnitCode _ = schedule.OdometerUnit(TheInstantAsked); });
			AssertItRefusesAndSaysWhatToAskFirst(
				() => { DateTime _ = schedule.ReadOdometerObservedAt(TheInstantAsked); });
		}

		// LA AUSENCIA ESTA CLAVEADA AL INSTANTE, no a la lista. La agenda TIENE una lectura; lo que
		// no tiene es una lectura observada al instante preguntado ni antes, que es lo unico contra
		// lo que Evaluate mide (ley L6).
		[Test]
		public void AReadingObservedOnlyLaterIsTheSameAbsence()
		{
			schedule.RecordReading(
				TheReading, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.Telemetry);

			Assert.That(schedule.HasAnOdometerReading(TheInstantAsked), Is.False,
				"La agenda tiene una lectura, pero no una que Evaluate(asOf) pudiera usar. Mirar "
				+ "solo si la lista esta vacia daria true aqui y despues un numero del futuro.");

			AssertItRefusesAndSaysWhatToAskFirst(() => { int _ = schedule.ReadOdometer(TheInstantAsked); });

			Assert.That(schedule.HasAnOdometerReading(ObservedLater), Is.True,
				"Y al instante en que si se observo, contesta que si: la misma agenda, la misma "
				+ "lectura, dos instantes.");
		}

		private static void AssertItRefusesAndSaysWhatToAskFirst(TestDelegate lectura)
		{
			DomainException error = Assert.Throws<DomainException>(lectura);

			Assert.That(error.Message, Does.Contain("HasAnOdometerReading"),
				"El mensaje tiene que decir QUE PREGUNTAR ANTES, como hace ReadNextDueKind. Un "
				+ "mensaje que solo diga que no hay deja a quien llama sin la salida.");
			Assert.That(error.Message, Does.Not.Contain("L6"),
				"Y lo dice con palabras: una sigla en un string que el programa puede imprimir se "
				+ "queda vieja sin que nadie la lea donde esta.");
		}
	}
}
