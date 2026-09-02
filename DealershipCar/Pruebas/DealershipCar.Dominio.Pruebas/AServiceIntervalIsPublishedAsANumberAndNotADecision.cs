using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// EL INTERVALO PUBLICADO ES UN NUMERO, NO UN PROCEDIMIENTO. El fabricante dice "cada 10000" y
	// esta especificacion lo guarda y lo contesta con su procedencia. Quien decide si algo VENCIO
	// es la agenda del vehiculo, con sus cinco bases y su periodo de gracia, y nada de eso se
	// re-deriva aca.
	//
	// D2 DEL PLAN (§4.3), Y ES EL PRECIO DE ESTE ITEM: §9 del brief pide la lectura del intervalo
	// publicado y su tabla de comandos no publica ninguno, asi que el comando se agrego. La
	// consecuencia es que el mismo numero del fabricante existe ahora en DOS sitios: aca, y en la
	// copia por vehiculo que la agenda usa para medir. La especificacion es la VERDAD PUBLICADA --
	// nombra su revision -- y la copia de la agenda sigue siendo la BASE DE MEDIDA. Decirlo es la
	// diferencia entre un numero en dos sitios y una divergencia que nadie puede atribuir.
	//
	// LA OPERACION ES LA MISMA DE LA AGENDA (D9): las nueve clases de operacion son un solo
	// conjunto cerrado, reusado. Un segundo conjunto sobre los mismos miembros seria el defecto que
	// las pruebas de espejo existen para agarrar.
	[TestFixture]
	public sealed class AServiceIntervalIsPublishedAsANumberAndNotADecision
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "REV-2025-01";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		[Test]
		public void TheSentenceSaysTheOperationTheIntervalTheReceivedHalfAndThenItsRevision()
		{
			TheFluidChangeIntervalEnters();
			specification.SealRevision(TheRevision);

			Assert.That(
				specification.DescribeServiceInterval(
					OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitA),
				Is.EqualTo(
					"Cambio de fluido: se hace cada 10000 km o 365 días. A este vehículo le quedan "
					+ "8400 km contra él. Publicado en la revisión REV-2025-01."));
		}

		// Las dos mitades del intervalo se dicen por separado y NUNCA se colapsan en un numero. Un
		// intervalo con distancia y plazo son dos limites, y el que llegue primero es el que manda
		// -- pero eso lo decide la agenda, no esta hoja.
		[Test]
		public void AnIntervalWithBothHalvesSaysBothAndCollapsesNeither()
		{
			TheFluidChangeIntervalEnters();
			specification.SealRevision(TheRevision);

			string answer = specification.DescribeServiceInterval(
				OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitA);

			Assert.That(answer, Does.Contain("cada 10000 km"));
			Assert.That(answer, Does.Contain("365 días"));
		}

		[Test]
		public void AnIntervalWithOnlyATermSaysOnlyTheTerm()
		{
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.TimedComponent,
				0, DistanceUnitCode.UnitA, 1825);
			specification.SealRevision(TheRevision);

			Assert.That(specification.IsServiceIntervalPublished(OperationKindCode.TimedComponent),
				Is.True);
			Assert.That(specification.ReadServiceIntervalRevision(OperationKindCode.TimedComponent),
				Is.EqualTo(TheRevision));
		}

		// LEY L2 para los intervalos: una clase de operacion que nadie publico contesta, no
		// revienta, y no dice ningun numero -- ni el suyo ni el de otra clase.
		[Test]
		public void AnOperationKindThatWasNeverPublishedAnswersNotPublished()
		{
			TheFluidChangeIntervalEnters();
			specification.SealRevision(TheRevision);

			Assert.That(specification.IsServiceIntervalPublished(OperationKindCode.Inspection),
				Is.False);
			Assert.That(specification.ReadServiceIntervalRevision(OperationKindCode.Inspection),
				Is.EqualTo(""));

			string answer = specification.DescribeServiceInterval(
				OperationKindCode.Inspection, 8400, DistanceUnitCode.UnitA);

			Assert.That(answer, Does.Contain("revisión"));
			Assert.That(answer, Does.Not.Contain("10000"));
			Assert.That(HayAlgunDigito(answer), Is.False,
				"La ausencia no lleva ni la distancia recibida: sin mitad publicada no hay contra "
				+ "que pararla, y un numero suelto ahi se lee como publicado.");
		}

		[Test]
		public void AnIntervalThatIsNeitherADistanceNorATermIsRefused()
		{
			Assert.Throws<DomainException>(() => specification.PublishServiceInterval(
				TheRevision, OperationKindCode.FluidChange, 0, DistanceUnitCode.UnitA, 0));
		}

		[TestCase(-1, 365)]
		[TestCase(10000, -1)]
		public void AnIntervalWithANegativeHalfIsRefused(int distance, int days)
		{
			Assert.Throws<DomainException>(() => specification.PublishServiceInterval(
				TheRevision, OperationKindCode.FluidChange, distance, DistanceUnitCode.UnitA, days));
		}

		[Test]
		public void EachOperationKindKeepsItsOwnInterval()
		{
			TheFluidChangeIntervalEnters();
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.Inspection, 20000, DistanceUnitCode.UnitA, 730);
			specification.SealRevision(TheRevision);

			Assert.That(
				specification.DescribeServiceInterval(
					OperationKindCode.Inspection, 8400, DistanceUnitCode.UnitA),
				Does.Contain("cada 20000 km"));
			Assert.That(
				specification.DescribeServiceInterval(
					OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitA),
				Does.Not.Contain("20000"));
		}

		[Test]
		public void WithinOneDraftTheLastPublicationOfAnOperationKindIsTheOneThatStands()
		{
			TheFluidChangeIntervalEnters();
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.FluidChange, 12000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheRevision);

			string answer = specification.DescribeServiceInterval(
				OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitA);

			Assert.That(answer, Does.Contain("cada 12000 km"));
			Assert.That(answer, Does.Not.Contain("10000"));
		}

		[Test]
		public void AnIntervalPublishedOnlyIntoADraftIsNotAnswered()
		{
			TheFluidChangeIntervalEnters();

			Assert.That(specification.IsServiceIntervalPublished(OperationKindCode.FluidChange),
				Is.False);
		}

		private void TheFluidChangeIntervalEnters()
		{
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.FluidChange, 10000, DistanceUnitCode.UnitA, 365);
		}

		private static bool HayAlgunDigito(string frase)
		{
			foreach (char letra in frase) if (char.IsDigit(letra)) return true;
			return false;
		}
	}
}
