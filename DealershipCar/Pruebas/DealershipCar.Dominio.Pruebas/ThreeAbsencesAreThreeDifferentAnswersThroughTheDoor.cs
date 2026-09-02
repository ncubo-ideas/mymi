using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// PRUEBAS 9, 10, 11 Y 12 DE §11 DEL BRIEF, EN LA PUERTA. El nivel A ya afirma que el perfil
	// PRODUCE la respuesta correcta en cada ausencia; lo que solo se puede ver aca es que la lectura
	// compuesta llegue a la misma, sacando del registro lo que hay y sin inventar lo que falta.
	//
	// LA TRAMPA ES D1 Y SE DICE ANTES DE QUE PAREZCA UN ERROR: un sobre NO PUBLICADO es una
	// respuesta DECIDIDA, no una suspension. Nadie publico un sobre es algo que el registro sabe con
	// certeza, asi que CanTheEnvelopeBeEvaluated contesta que SI para un vehiculo sin sobre. Se lee
	// raro una vez y es correcto.
	//
	// Y LA POLARIDAD DE LAS SEIS LECTURAS DE T6 SE MIDE AQUI, porque este es el unico fixture que
	// recorre los cuatro estados del registro: las dos totales del sobre contestan siempre, la
	// guardada rechaza cuando no se pudo decidir, y las tres de cobertura rechazan cuando al registro
	// le falta uno de sus tres hechos.
	[TestFixture]
	public sealed class ThreeAbsencesAreThreeDifferentAnswersThroughTheDoor
	{
		private const string TheVin = "KMHD35LE1EU123456";
		private const string TheSystem = "POWERTRAIN";
		private const string TheRevision = "R1";

		private const int ThirtySixMonths = 36;
		private const int FifteenThousand = 15000;
		private const string TheUnitEverythingIsIn = "UnitB";

		private const int TheOdometerAtDelivery = 30000;
		private const int TheOdometerToday = 35000;

		private static readonly DateTime ThePublication =
			new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		// PRUEBA 9, Y ES LA QUE MAS IMPORTA DEL SLICE: con el sobre publicado y la entrega registrada
		// pero SIN NINGUNA LECTURA, la respuesta nombra la lectura que falta. Ni cero, ni la lectura
		// de la entrega haciendo de la de hoy, ni "adentro".
		[Test]
		public void WithNoOdometerReadingTheAnswerNamesTheReadingThatIsMissing()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithNoReading())
			{
				Assert.That(journal.Api.HasAnOdometerReading(TheDayItIsAsked), Is.False);
				Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(TheDayItIsAsked), Is.False);
				Assert.Throws<InvalidOperationException>(
					() => journal.Api.ReadEnvelopeStanding(TheDayItIsAsked));

				string said = journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked);

				Assert.That(said, Does.Contain("no odometer reading"));
				Assert.That(said, Does.Contain("Time: 28 months left of 36 months."),
					"La mitad de tiempo se mide igual: no depende de ninguna lectura.");

				Assert.That(said, Does.Not.Contain("Distance: 0"));
				Assert.That(said, Does.Not.Contain("left of 15000"),
					"Y NINGUN NUMERO DE DISTANCIA. Un cero, o la lectura de la entrega haciendo de la "
					+ "de hoy, seria una medicion que nadie tomo, y quien la lea rio abajo no podria "
					+ "distinguirla de una de verdad.");
			}
		}

		// PRUEBA 10 Y D1: sin sobre, la respuesta esta DECIDIDA y dice que nadie publico ninguno. Es
		// una respuesta distinta de la de arriba, y CanTheEnvelopeBeEvaluated contesta que SI.
		[Test]
		public void WithNoEnvelopeTheAnswerIsDecidedAndSaysNobodyPublishedOne()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithNoEnvelope())
			{
				Assert.That(journal.Api.IsWarrantyEnvelopePublished(), Is.False);

				Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(TheDayItIsAsked), Is.True,
					"D1: no publicado es una respuesta que el registro da con certeza, no una que no "
					+ "se pudo dar. Se lee raro una vez y es correcto.");

				Assert.That(
					journal.Api.ReadEnvelopeStanding(TheDayItIsAsked), Is.EqualTo("NotPublished"));
				Assert.That(
					journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked),
					Does.Contain("not published"));
			}
		}

		// PRUEBA 11: con sobre y SIN ENTREGA, la respuesta nombra el ancla ausente -- una tercera
		// cosa, ni la de arriba ni la de abajo.
		[Test]
		public void WithNoDeliveryTheAnswerNamesTheAnchorThatIsMissing()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithNoDelivery())
			{
				Assert.That(journal.Api.HasDeliveryBeenRecorded(), Is.False);
				Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(TheDayItIsAsked), Is.False);
				Assert.Throws<InvalidOperationException>(
					() => journal.Api.ReadEnvelopeStanding(TheDayItIsAsked));

				string said = journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked);

				Assert.That(said, Does.Contain("no delivery"));
				Assert.That(said, Does.Contain("36 months or 15000 mi"),
					"Y LO PUBLICADO SE SIGUE CITANDO. Faltar un hecho personal no vuelve incitable lo "
					+ "que la agencia publico.");
				Assert.That(said, Does.Not.Contain("Time:"),
					"Sin ancla no hay desde donde contar el plazo, asi que tampoco hay mitad de "
					+ "tiempo que decir: un numero aqui se contaria desde una fecha inventada.");
			}
		}

		// PRUEBA 12: LAS TRES AUSENCIAS SON TRES RESPUESTAS DISTINTAS, comparadas entre si y no solo
		// contra el vacio (C60). Dos que se dijeran igual dejarian a quien lea sin saber cual de los
		// tres arreglos le toca -- y los tres son de gente distinta: la entrega la registra la
		// agencia, la lectura la trae el feed, y las unidades no las arregla nadie convirtiendo.
		[Test]
		public void TheFourSituationsAreFourDifferentSentences()
		{
			using (AScheduleOnARealJournal noEnvelope = AVehicleWithNoEnvelope())
			using (AScheduleOnARealJournal noDelivery = AVehicleWithNoDelivery())
			using (AScheduleOnARealJournal noReading = AVehicleWithNoReading())
			using (AScheduleOnARealJournal everything = AVehicleWithEverything())
			{
				string[] four =
				{
					noEnvelope.Api.DescribeWarrantyEnvelope(TheDayItIsAsked),
					noDelivery.Api.DescribeWarrantyEnvelope(TheDayItIsAsked),
					noReading.Api.DescribeWarrantyEnvelope(TheDayItIsAsked),
					everything.Api.DescribeWarrantyEnvelope(TheDayItIsAsked),
				};

				Assert.That(four, Is.Unique);
			}
		}

		// LA POLARIDAD DE LAS TRES LECTURAS DE COBERTURA, EN LOS CUATRO ESTADOS. Las tres necesitan
		// los TRES hechos del registro, asi que las tres rechazan mientras falte alguno -- y la que
		// contesta con palabras rechaza tambien, que es la posicion de §4.2 que T6 movio.
		//
		// EL MOTIVO ESTA EN QUE LA FRASE DE COBERTURA LA COMPONE EL CATALOGO DEL FABRICANTE, que es
		// de un MODELO y no de este carro: no puede decir que le falta a un vehiculo porque no sabe
		// de ninguno. Lo que si contesta sin ningun hecho de ningun vehiculo es el ALCANCE publicado,
		// y cual de los tres hechos falta lo dice la frase del sobre. Las dos mitades siguen en la
		// puerta, que es lo que hace que rechazar aca no le quite nada a nadie.
		[Test]
		public void TheThreeCoverageReadsRefuseWhileAnyOfTheThreeFactsIsMissing()
		{
			foreach (Func<AScheduleOnARealJournal> incomplete in new Func<AScheduleOnARealJournal>[]
				{ AVehicleWithNoEnvelope, AVehicleWithNoDelivery, AVehicleWithNoReading })
			{
				using (AScheduleOnARealJournal journal = incomplete())
				{
					Assert.That(
						journal.Api.CanCoverageBeEvaluatedForThisVehicle(TheSystem, TheDayItIsAsked),
						Is.False);

					Assert.Throws<InvalidOperationException>(
						() => journal.Api.ReadCoverageStandingForThisVehicle(
							TheSystem, TheDayItIsAsked));

					Assert.Throws<InvalidOperationException>(
						() => journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked));

					// Y LAS DOS MITADES SIGUEN CONTESTANDO. El alcance publicado, sin ningun hecho de
					// ningun vehiculo; y cual hecho falta, con palabras.
					Assert.That(
						journal.Api.DescribeCoverageSummary(), Does.Contain(TheSystem));
					Assert.That(
						journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked), Is.Not.Empty);
				}
			}
		}

		// Y CON LOS TRES PUESTOS LAS TRES CONTESTAN. Es el renglon que hace discriminar al de arriba:
		// sin el, tres lecturas que rechazaran SIEMPRE lo pasarian.
		[Test]
		public void WithTheThreeFactsRecordedTheThreeCoverageReadsAnswer()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithEverything())
			{
				Assert.That(
					journal.Api.CanCoverageBeEvaluatedForThisVehicle(TheSystem, TheDayItIsAsked),
					Is.True);
				Assert.That(
					journal.Api.ReadCoverageStandingForThisVehicle(TheSystem, TheDayItIsAsked),
					Is.EqualTo("Covered"));
				Assert.That(
					journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked),
					Does.Contain("Covered"));
			}
		}

		// UNA LECTURA OBSERVADA SOLO DESPUES DEL INSTANTE PREGUNTADO ES, PARA ESA PREGUNTA, NINGUNA
		// LECTURA. Es el estado que separa "el registro no tiene lectura" de "el registro no tiene
		// lectura A ESE INSTANTE", y sin el, las guardas de las tres de cobertura se podrian estar
		// preguntando con OTRO instante que el que la lectura compuesta va a usar y nada lo veria:
		// con la lectura ausente en los dos instantes, las dos claves dan lo mismo.
		//
		// MEDIDO: con la guarda de la puerta preguntada 90 dias mas tarde que la lectura, ninguna
		// prueba que nombre esta regla se ponia roja -- se ponia roja una que defiende otra cosa. La
		// afirmacion que lo atrapa es el TIPO de la excepcion: con la guarda mal fechada, la
		// pregunta pasa el filtro de la puerta y revienta adentro del script con la excepcion del
		// dominio, que es interna y que ninguna caratula puede nombrar.
		[Test]
		public void AReadingObservedOnlyAfterTheInstantAskedIsTheSameAsNoReadingAtAll()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOnlyReadingCameLater())
			{
				Assert.That(journal.Api.HasAnOdometerReading(TheDayItIsAsked), Is.False);

				Assert.That(
					journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked),
					Does.Contain("no odometer reading"));

				Assert.Throws<InvalidOperationException>(
					() => journal.Api.ReadEnvelopeStanding(TheDayItIsAsked));
				Assert.Throws<InvalidOperationException>(
					() => journal.Api.ReadCoverageStandingForThisVehicle(TheSystem, TheDayItIsAsked));
				Assert.Throws<InvalidOperationException>(
					() => journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked),
					"Y EL TIPO IMPORTA: la excepcion del dominio es interna y ninguna caratula la "
					+ "puede nombrar, asi que dejarla salir le daria a quien lee un rechazo que solo "
					+ "puede atrapar como Exception pelada -- junto con el fallo del motor, el del "
					+ "disco y un defecto de programacion.");

				// Y PREGUNTADO DESPUES DE ESA LECTURA, LAS SEIS CONTESTAN. Es lo que hace discriminar
				// a todo lo de arriba: sin esto, un registro que rechazara SIEMPRE lo pasaria igual.
				DateTime afterwards = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

				Assert.That(journal.Api.HasAnOdometerReading(afterwards), Is.True);
				Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(afterwards), Is.True);
				Assert.That(
					journal.Api.ReadCoverageStandingForThisVehicle(TheSystem, afterwards),
					Is.EqualTo("Covered"));
			}
		}

		// NINGUNO DE LOS NOMBRES POSIBLES SALE PARA UNA PREGUNTA QUE NO SE PUDO DECIDIR. Es E47 en la
		// puerta: la respuesta guardada rechaza en vez de devolver el miembro benigno, que es como
		// una app termina repitiendo rio abajo, como si fuera un hecho, algo que nadie midio.
		[Test]
		public void NoStandingNameIsEverReturnedForAQuestionThatCouldNotBeDecided()
		{
			foreach (Func<AScheduleOnARealJournal> unevaluable in new Func<AScheduleOnARealJournal>[]
				{ AVehicleWithNoDelivery, AVehicleWithNoReading })
			{
				using (AScheduleOnARealJournal journal = unevaluable())
				{
					Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(TheDayItIsAsked), Is.False);

					InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
						() => journal.Api.ReadEnvelopeStanding(TheDayItIsAsked));

					Assert.That(refused.Message, Does.Contain("CanTheEnvelopeBeEvaluated"),
						"Y el rechazo dice que preguntar antes. Un rechazo mudo obliga a quien lea a "
						+ "adivinar la secuencia.");
				}
			}
		}

		private static AScheduleOnARealJournal AVehicleWithEverything()
		{
			AScheduleOnARealJournal journal = AVehicleWithNoReading();

			journal.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			return journal;
		}

		// EL SOBRE Y LA ENTREGA PUESTOS, Y LA UNICA LECTURA FECHADA DESPUES del instante que este
		// fixture pregunta. El feed la trajo; simplemente no habla de febrero.
		//
		// NUEVE DIAS DESPUES Y NO CUATRO MESES, y la distancia es la medida. Lo que este estado
		// atrapa es una guarda fechada distinto de la lectura, y solo lo atrapa si el desfase cae
		// ENTRE los dos instantes: con la lectura en junio, una guarda corrida noventa dias seguia
		// sin encontrarla y el defecto pasaba verde. Cuanto mas pegada al instante preguntado este la
		// lectura, mas chico es el desfase que se descubre.
		private static readonly DateTime NineDaysAfterTheInstantAsked =
			new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);

		private static AScheduleOnARealJournal AVehicleWhoseOnlyReadingCameLater()
		{
			AScheduleOnARealJournal journal = AVehicleWithNoReading();

			journal.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, NineDaysAfterTheInstantAsked, "Telemetry");

			return journal;
		}

		private static AScheduleOnARealJournal AVehicleWithNoReading()
		{
			AScheduleOnARealJournal journal = AVehicleWithNoDelivery();

			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);

			return journal;
		}

		private static AScheduleOnARealJournal AVehicleWithNoDelivery()
		{
			AScheduleOnARealJournal journal = AVehicleWhoseManufacturerPublishedItsScope();

			journal.Api.PublishWarrantyEnvelope(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn, ThePublication);

			return journal;
		}

		// SIN SOBRE PERO CON TODO LO DEMAS, para que el caso de arriba se distinga de este por el
		// sobre y no por lo que le falte ademas.
		private static AScheduleOnARealJournal AVehicleWithNoEnvelope()
		{
			AScheduleOnARealJournal journal = AVehicleWhoseManufacturerPublishedItsScope();

			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			journal.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			return journal;
		}

		private static AScheduleOnARealJournal AVehicleWhoseManufacturerPublishedItsScope()
		{
			AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, TheUnitEverythingIsIn);
			journal.Api.SealRevision(TheRevision);

			return journal;
		}
	}
}
