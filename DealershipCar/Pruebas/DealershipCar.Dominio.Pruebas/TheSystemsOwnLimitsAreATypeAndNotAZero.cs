using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// E45, REGLA 2, sobre los limites propios de un sistema cubierto: "cero-o-uno se escribe como
	// un PAR DE NULL OBJECT, nunca como un campo anulable" -- ASystemLimits / NoSystemLimits, la
	// convencion de la casa que §3.3 del plan pide por nombre.
	//
	// LO QUE ESTA PRUEBA DEFIENDE DE VERDAD ES EL CERO DEL WIRE. §9 del brief publica "limites
	// propios opcionales en meses y distancia", y en el wire un limite ausente llega como 0. Un
	// cero que entrara al dominio como limite dejaria a TODO sistema sin limites propios fuera de
	// su sobre desde el primer dia: cero meses cumplidos el dia de la entrega. Por eso el cero se
	// traduce en la frontera del agregado y no viaja mas adentro.
	//
	// Y EL PAR TIENE LA FORMA DE E41: una pregunta de presencia TOTAL y accesores GUARDADOS. Los
	// guardados revientan en vez de contestar un cero benigno, que es el mismo anti-patron que
	// C47 nombra en el otro extremo del slice -- una respuesta comoda para algo que no se sabe.
	[TestFixture]
	public sealed class TheSystemsOwnLimitsAreATypeAndNotAZero
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "REV-2025-01";
		private const string TheSystem = "ENG-BLOCK";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime DeliveredAt = new DateTime(2025, 1, 15, 9, 0, 0);
		private static readonly DateTime TheDayAfterDelivery = new DateTime(2025, 1, 16, 9, 0, 0);

		[Test]
		public void TheAbsenceIsAnObjectThatAnswersThatItIsNotThere()
		{
			Assert.That(SystemLimits.None.Exists(), Is.False);
		}

		[Test]
		public void ThePresentOneAnswersThatItIsThereAndSaysItself()
		{
			SystemLimits limits =
				SystemLimits.OfTheSystemsOwn(36, 60000, DistanceUnit.UnitA);

			Assert.That(limits.Exists(), Is.True);
			Assert.That(limits.Describe(), Does.Contain("36 meses"));
			Assert.That(limits.Describe(), Does.Contain("60000 km"));
		}

		// EL CERO DEL WIRE NO PASA. Publicar un sistema sin limites propios da la ausencia, no un
		// limite de cero.
		[Test]
		public void AWireZeroOnBothHalvesIsTheAbsenceAndNotALimitOfZero()
		{
			SystemLimits limits = SystemLimits.OfTheSystemsOwn(0, 0, DistanceUnit.UnitA);

			Assert.That(limits.Exists(), Is.False,
				"Un cero en las dos mitades es como el wire dice 'este sistema no publico limites "
				+ "propios'. Tomarlo como limite dejaria al sistema fuera de su sobre el mismo dia "
				+ "de la entrega.");
		}

		// Y SE VE DESDE AFUERA: un sistema publicado sin limites propios se mide contra el sobre
		// del vehiculo, no contra un cero.
		[Test]
		public void ASystemPublishedWithoutOwnLimitsIsMeasuredAgainstTheVehiclesEnvelope()
		{
			ModelSpecification specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, DistanceUnitCode.UnitA);
			specification.SealRevision(TheRevision);

			Assert.That(
				specification.ReadCoverageStanding(
					TheSystem, 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 10, 11, DistanceUnitCode.UnitA, TheDayAfterDelivery),
				Is.EqualTo("Covered"),
				"Si el cero del wire llegara como limite, este sistema estaria fuera del sobre al "
				+ "dia siguiente de que le entregaron el carro.");
			Assert.That(
				specification.DescribeCoverage(
					TheSystem, 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 10, 11, DistanceUnitCode.UnitA, TheDayAfterDelivery),
				Does.Contain("36 meses o 100000 km"),
				"Y los limites que la respuesta nombra son los del SOBRE, no unos ceros: si el cero "
				+ "hubiera entrado como limite propio, la frase diria contra que se midio y seria un "
				+ "limite que nadie publico.");
		}

		// Media mitad SI es un limite: cero meses y una distancia publicada es un limite de
		// distancia, no una ausencia. La ausencia es que no haya NINGUNA de las dos.
		[Test]
		public void OneHalfPublishedIsStillALimitAndNotAnAbsence()
		{
			Assert.That(SystemLimits.OfTheSystemsOwn(0, 60000, DistanceUnit.UnitA).Exists(), Is.True);
			Assert.That(SystemLimits.OfTheSystemsOwn(36, 0, DistanceUnit.UnitA).Exists(), Is.True);
		}

		// LOS ACCESORES DEL AUSENTE REVIENTAN. Un false benigno ahi diria que el vehiculo esta
		// dentro de unos limites que nadie publico.
		[Test]
		public void EveryAccessorOfTheAbsentOneRefusesToAnswer()
		{
			Assert.Throws<DomainException>(() => SystemLimits.None.Describe());
			Assert.Throws<DomainException>(() => SystemLimits.None.HasADistanceHalf());
			Assert.Throws<DomainException>(
				() => SystemLimits.None.MeasuresDistanceIn(DistanceUnit.UnitA));
			Assert.Throws<DomainException>(
				() => SystemLimits.None.TheTimeHalfIsPast(DeliveredAt, TheDayAfterDelivery));
			Assert.Throws<DomainException>(() => SystemLimits.None.TheDistanceHalfIsPast(10, 20));
			Assert.Throws<DomainException>(
				() => SystemLimits.None.TheUnitItsDistanceIsMeasuredIn());
		}

		// El sobre del vehiculo SE RECIBE y tiene que traer al menos una mitad: un sobre sin
		// ninguna no es un sobre, y es una llamada mal hecha y no un estado del dominio.
		[Test]
		public void AVehicleEnvelopeWithNeitherHalfIsACallerBug()
		{
			Assert.Throws<DomainException>(
				() => SystemLimits.OfTheVehicleEnvelope(0, 0, DistanceUnit.UnitA));
		}

		[Test]
		public void ALimitWithANegativeHalfIsACallerBug()
		{
			Assert.Throws<DomainException>(
				() => SystemLimits.OfTheSystemsOwn(-1, 60000, DistanceUnit.UnitA));
			Assert.Throws<DomainException>(
				() => SystemLimits.OfTheSystemsOwn(36, -1, DistanceUnit.UnitA));
		}

		// Una distancia sin unidad es lo que L8 prohibe con todas las letras -- departure D8 del
		// plan, que agrego la unidad a los limites propios justamente por esto.
		[Test]
		public void ALimitWithADistanceAndNoUnitIsACallerBug()
		{
			Assert.Throws<DomainException>(() => SystemLimits.OfTheSystemsOwn(36, 60000, null));
		}

		// El par es un TIPO, no un anulable: la base es abstracta y las dos variantes son suyas.
		// Es la misma afirmacion que TheOccurrenceSpellsItsAbsencesWithTypes hace de OS2.
		[Test]
		public void TheAbsenceIsSpelledWithATypeAndNotWithANullableSlot()
		{
			Assert.That(typeof(SystemLimits).IsAbstract, Is.True);

			List<string> variantes = Assembly.Load(new AssemblyName("DealershipCar.Dominio"))
				.GetTypes()
				.Where(tipo => tipo != typeof(SystemLimits)
					&& typeof(SystemLimits).IsAssignableFrom(tipo))
				.Select(tipo => tipo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(variantes, Is.EqualTo(new[] { "ASystemLimits", "NoSystemLimits" }),
				"El par es exactamente dos: el que esta y el que no. Una tercera variante seria una "
				+ "clase de ausencia sin nombre propio (E47) o un estado disfrazado de limite.");
		}
	}
}
