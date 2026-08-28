using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE LA PUERTA LE HACE A LOS ARGUMENTOS DE UNA LECTURA ANTES DE MANDARLA. El hermano de
	// ASpecificationIsPublishedThroughTheDoor, del lado de las treinta y cinco lecturas.
	//
	// POR QUE HACE FALTA UN BARRIDO Y NO BASTAN LAS QUE UNO SE ACUERDE DE ESCRIBIR. Las lecturas de
	// la especificacion tienen VEINTIDOS ranuras de conjunto cerrado y CINCO cadenas obligatorias, y
	// la guarda que falta es siempre la del parametro que se agrego despues. MEDIDO: la primera
	// version de estas pruebas no barria, y tres WireName quitados a proposito -- el de la unidad de
	// lo que le queda al vehiculo, el del odometro y el del sobre -- dejaron la suite entera verde.
	// Cada una de las tres estaba en una lectura que ninguna prueba llamaba nunca con un nombre
	// inventado.
	//
	// Y QUE SE PIERDE SIN ELLAS, que no es lo mismo que en los comandos. Una lectura no escribe, asi
	// que un nombre inventado no deja cicatriz: lo que se pierde es que el rechazo sea INTELIGIBLE.
	// Con WireName la puerta dice que ese texto no nombra ningun miembro y cuales hay; sin el, el
	// texto crudo llega al script y revienta el motor por una razon que no nombra al parametro.
	[TestFixture]
	public sealed class ASpecificationIsReadBackThroughTheDoor
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheRevision = "R1";
		private const string TheSystem = "POWERTRAIN";
		private const string NoMember = "NoEsMiembroDeNada";
		private const string Blank = "   ";

		private const int TenThousandUnits = 10000;

		private static readonly DateTime Published =
			new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime Delivered =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime ValidUntil = Published.AddYears(1);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
			ASealedSpecSheet();
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LAS VEINTIDOS RANURAS DE CONJUNTO CERRADO, una por una, con un nombre inventado por turno y
		// el resto valido. La clave de cada par es lo que se lee cuando esa es la que falla.
		[Test]
		public void EveryClosedSetNameOfEveryReadIsRefusedAtTheDoorWhenItNamesNoMember()
		{
			foreach (KeyValuePair<string, TestDelegate> each in EveryClosedSetSlotMadeUp())
				Assert.That(Assert.Catch(each.Value), Is.InstanceOf<ArgumentException>(), each.Key);
		}

		// Y LAS CINCO CADENAS OBLIGATORIAS, por lo mismo. Un codigo de sistema en blanco no nombra
		// ningun sistema y un codigo de revision en blanco no nombra ninguna revision: las dos son
		// llamadas mal hechas, y se dicen asi en vez de dejarlas llegar al script.
		[Test]
		public void EveryRequiredTextOfEveryReadIsRefusedAtTheDoorWhenItIsBlank()
		{
			foreach (KeyValuePair<string, TestDelegate> each in EveryRequiredTextLeftBlank())
				Assert.That(Assert.Catch(each.Value), Is.InstanceOf<ArgumentException>(), each.Key);
		}

		// EL INSTANTE QUE ENTRA POR UNA LECTURA SE TRUNCA AL SEGUNDO ENTERO, igual que el que entra
		// por un comando. Aca no hay journal de por medio -- una consulta no escribe nada -- asi que
		// lo que se compara es el instante recibido contra uno que el journal YA trunco al guardarlo.
		// Media fraccion de segundo decide entonces entre "rige" y "vencido", y es la persona
		// parada en el taller la que ve la diferencia.
		[Test]
		public void APriceValidityIsJudgedAtTheWholeSecondTheJournalKeeps()
		{
			Assert.That(ThePriceClauseAsOf(ValidUntil), Does.Contain("in force"));

			Assert.That(ThePriceClauseAsOf(ValidUntil.AddMilliseconds(500)),
				Does.Contain("in force"),
				"Media fraccion despues del ultimo segundo de la vigencia sigue siendo ESE segundo: "
				+ "es el unico que el journal guardo. Sin truncar en la puerta, este precio se "
				+ "declara vencido por una fraccion que no existe en ningun lado.");

			Assert.That(ThePriceClauseAsOf(ValidUntil.AddSeconds(1)),
				Does.Contain("not current"),
				"Y un segundo entero despues si esta vencido: el truncado no corre la vigencia, la "
				+ "redondea a la resolucion que hay.");
		}

		// LA MISMA MITAD, en la guarda de hechos imposibles de la cobertura. Preguntar por un
		// instante ANTERIOR a la entrega es un defecto de quien llama y revienta; media fraccion de
		// segundo no puede ser lo que lo convierta en uno.
		[Test]
		public void AnInstantThatArrivesWithAFractionDoesNotFakeAnImpossibleFact()
		{
			Assert.DoesNotThrow(() => TheStandingAskedAt(Delivered, Delivered));

			Assert.DoesNotThrow(
				() => TheStandingAskedAt(Delivered.AddMilliseconds(500), Delivered),
				"La entrega llego con fraccion y la pregunta es del mismo segundo: truncados los dos, "
				+ "es el mismo instante. Sin truncar, la puerta acusa al que llama de preguntar antes "
				+ "de que le entregaran el carro.");

			Assert.Throws<DomainException>(
				() => TheStandingAskedAt(Delivered.AddSeconds(1), Delivered),
				"Un segundo entero antes de la entrega si es el hecho imposible que la guarda nombra.");
		}

		// LO QUE ESTA PRUEBA NO PUEDE DEFENDER, dicho en voz alta para que no se lea como cubierto.
		// El truncado del instante de la PREGUNTA de cobertura no tiene consecuencia observable hoy:
		// el plazo se mide en meses enteros -- resolucion de dia -- y la guarda de arriba compara el
		// instante recibido contra otro que ya paso por el mismo truncado. Se normaliza igual, porque
		// la frontera trata todos sus instantes de una sola manera, pero quitarlo no pone roja
		// ninguna prueba de esta suite. El dia que una lectura compare ese instante contra algo con
		// resolucion de segundo, esto pasa a ser falsificable y hay que escribirla.
		[Test]
		public void TheCoverageAsOfIsNormalizedWithoutAnythingBeingAbleToTellTheDifference()
		{
			Assert.That(
				TheStandingAskedAt(Delivered, Delivered.AddMonths(12)),
				Is.EqualTo(TheStandingAskedAt(Delivered, Delivered.AddMonths(12).AddMilliseconds(500))));
		}

		private Dictionary<string, TestDelegate> EveryClosedSetSlotMadeUp()
		{
			return new Dictionary<string, TestDelegate>
			{
				{ "IsFluidPublished.kind",
					() => journal.Api.IsFluidPublished(NoMember) },
				{ "DescribeFluid.kind",
					() => journal.Api.DescribeFluid(NoMember) },
				{ "ReadFluidRevision.kind",
					() => journal.Api.ReadFluidRevision(NoMember) },
				{ "IsTyrePublished.position",
					() => journal.Api.IsTyrePublished(NoMember) },
				{ "DescribeTyre.position",
					() => journal.Api.DescribeTyre(NoMember) },
				{ "ReadTyreRevision.position",
					() => journal.Api.ReadTyreRevision(NoMember) },
				{ "IsServiceIntervalPublished.kind",
					() => journal.Api.IsServiceIntervalPublished(NoMember) },
				{ "DescribeServiceInterval.kind",
					() => journal.Api.DescribeServiceInterval(NoMember, 8400, "UnitA") },
				{ "DescribeServiceInterval.remainingUnit",
					() => journal.Api.DescribeServiceInterval("FluidChange", 8400, NoMember) },
				{ "ReadServiceIntervalRevision.kind",
					() => journal.Api.ReadServiceIntervalRevision(NoMember) },
				{ "IsServicePricePublished.kind",
					() => journal.Api.IsServicePricePublished(NoMember, TenThousandUnits, "UnitA") },
				{ "IsServicePricePublished.milestoneUnit",
					() => journal.Api.IsServicePricePublished(
						"FluidChange", TenThousandUnits, NoMember) },
				{ "DescribeServicePrice.kind",
					() => journal.Api.DescribeServicePrice(
						NoMember, TenThousandUnits, "UnitA", Published) },
				{ "DescribeServicePrice.milestoneUnit",
					() => journal.Api.DescribeServicePrice(
						"FluidChange", TenThousandUnits, NoMember, Published) },
				{ "ReadServicePriceRevision.kind",
					() => journal.Api.ReadServicePriceRevision(
						NoMember, TenThousandUnits, "UnitA") },
				{ "ReadServicePriceRevision.milestoneUnit",
					() => journal.Api.ReadServicePriceRevision(
						"FluidChange", TenThousandUnits, NoMember) },
				{ "CanCoverageBeEvaluated.envelopeUnit",
					() => journal.Api.CanCoverageBeEvaluated(
						TheSystem, 36, 100000, NoMember,
						Delivered, 10, 12000, "UnitA", Published) },
				{ "CanCoverageBeEvaluated.currentUnit",
					() => journal.Api.CanCoverageBeEvaluated(
						TheSystem, 36, 100000, "UnitA",
						Delivered, 10, 12000, NoMember, Published) },
				{ "ReadCoverageStanding.envelopeUnit",
					() => journal.Api.ReadCoverageStanding(
						TheSystem, 36, 100000, NoMember,
						Delivered, 10, 12000, "UnitA", Published) },
				{ "ReadCoverageStanding.currentUnit",
					() => journal.Api.ReadCoverageStanding(
						TheSystem, 36, 100000, "UnitA",
						Delivered, 10, 12000, NoMember, Published) },
				{ "DescribeCoverage.envelopeUnit",
					() => journal.Api.DescribeCoverage(
						TheSystem, 36, 100000, NoMember,
						Delivered, 10, 12000, "UnitA", Published) },
				{ "DescribeCoverage.currentUnit",
					() => journal.Api.DescribeCoverage(
						TheSystem, 36, 100000, "UnitA",
						Delivered, 10, 12000, NoMember, Published) },
			};
		}

		private Dictionary<string, TestDelegate> EveryRequiredTextLeftBlank()
		{
			return new Dictionary<string, TestDelegate>
			{
				{ "TheVocabularyIsTheManufacturers.systemCode",
					() => journal.Api.TheVocabularyIsTheManufacturers(Blank) },
				{ "CanCoverageBeEvaluated.systemCode",
					() => journal.Api.CanCoverageBeEvaluated(
						Blank, 36, 100000, "UnitA", Delivered, 10, 12000, "UnitA", Published) },
				{ "ReadCoverageStanding.systemCode",
					() => journal.Api.ReadCoverageStanding(
						Blank, 36, 100000, "UnitA", Delivered, 10, 12000, "UnitA", Published) },
				{ "DescribeCoverage.systemCode",
					() => journal.Api.DescribeCoverage(
						Blank, 36, 100000, "UnitA", Delivered, 10, 12000, "UnitA", Published) },
				{ "ReadRevisionState.revisionCode",
					() => journal.Api.ReadRevisionState(Blank) },
			};
		}

		private string ThePriceClauseAsOf(DateTime asOf)
		{
			return journal.Api.DescribeServicePrice(
				"FluidChange", TenThousandUnits, "UnitA", asOf);
		}

		private string TheStandingAskedAt(DateTime deliveredAt, DateTime asOf)
		{
			return journal.Api.ReadCoverageStanding(
				TheSystem, 36, 100000, "UnitA", deliveredAt, 10, 12000, "UnitA", asOf);
		}

		private void ASealedSpecSheet()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			journal.Api.PublishFluidSpecification(
				TheRevision, "EngineOil", "5W-30", 4.2m, "UnitA", TenThousandUnits, "UnitA", 365);

			journal.Api.PublishTyreSpecification(
				TheRevision, "Front", "205/55R16", 2.4m, "UnitA", "94V");

			journal.Api.PublishServiceInterval(
				TheRevision, "FluidChange", TenThousandUnits, "UnitA", 365);

			journal.Api.PublishServicePrice(
				TheRevision, "FluidChange", TenThousandUnits, "UnitA", 75.50m, "USD",
				Published, ValidUntil);

			journal.Api.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, "UnitA");

			journal.Api.SealRevision(TheRevision);
		}
	}
}
