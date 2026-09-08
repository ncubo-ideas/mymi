using System;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L5 de SLICE-CostLedger.md: "una entrada duradera sin vida util no se puede amortizar, y
	// amortizarla sobre un solo periodo convierte la vista amortizada de vuelta en la vista de caja
	// -- exactamente la falla que §2.1 existe para prevenir. RECHAZAR, y decir cual entrada y que
	// falta. No poner un valor por defecto."
	//
	// LAS DOS MITADES DEL RECHAZO VIVEN EN DOS TIPOS, Y ES EL PRECEDENTE DE DEC-90: una guarda de
	// VALOR vive en el tipo que tiene el valor, y una de COLABORADOR REQUERIDO vive en la entrada,
	// donde el asunto esta a la vista para nombrarlo. Asi que:
	//
	//   AnExpectedLife rechaza los meses no positivos -- el valor -- y su mensaje nombra los meses.
	//   ADurableCost rechaza la vida ausente -- el colaborador -- y su mensaje nombra el ASUNTO.
	//
	// Las dos se prueban DIRECTO, cada una por su lado, porque CL-2 §12 midio tres guardas de este
	// modulo que no ponen rojo ningun test cuando se borran: una guarda que nadie defiende es una
	// afirmacion estructural que nada puede ver.
	//
	// Y EL VALOR POR DEFECTO QUE §7 NOMBRA NO EXISTE: no hay ninguna constante de vida util en el
	// dominio, y la mitad estructural de eso se afirma por reflexion aca abajo -- ADurableCost no
	// tiene ningun campo literal donde una vida por defecto podria esconderse.
	[TestFixture]
	public sealed class ADurableCostWithoutItsExpectedLifeIsRefused
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheSubject = "Llantas nuevas";

		private static readonly DateTime Incurred = new DateTime(2026, 1, 1, 0, 0, 0);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);
		}

		// LA GUARDA DE VALOR, directa y en su propio tipo. Cero meses tambien: una vida de cero meses
		// se consume entera en el instante en que se paga, que es una recurrente disfrazada.
		[TestCase(0)]
		[TestCase(-1)]
		[TestCase(-48)]
		public void AnExpectedLifeThatIsNotPositiveIsNotAnExpectedLife(int months)
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => new AnExpectedLife(months));

			Assert.That(refused.Message, Does.Contain(months.ToString()),
				"El rechazo nombra los meses que llegaron: es lo unico que este tipo tiene.");
		}

		[Test]
		public void AndTheRefusalIsTheDomainsOwnAndNotAnArgumentException()
		{
			Assert.That(Assert.Catch(() => new AnExpectedLife(0)),
				Is.InstanceOf<DomainException>().And.Not.InstanceOf<ArgumentException>(),
				"C22/C36/C37: una guarda que tira otra cosa se escapa de Apply y para el bloque de "
				+ "refresco entero.");
		}

		// LA GUARDA DE COLABORADOR, directa, y lo que se mide es QUE DICE: L5 pide que nombre cual
		// entrada y que falta. Un rechazo que solo diga "falta algo" no dice cual de diez llantas.
		[Test]
		public void ADurableCostWithNoLifeAtAllIsRefusedAndTheRefusalNamesTheEntryAndWhatIsMissing()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => new ADurableCost(
					new Money(240000m, Currency.CRC), Incurred, new CostSubject(TheSubject), null));

			Assert.That(refused.Message, Does.Contain(TheSubject),
				"Cual entrada: el asunto es lo unico con lo que una persona la reconoce.");
			Assert.That(refused.Message, Does.Contain("vida"),
				"Y que falta.");
		}

		// EL COMANDO LO HEREDA: la vida entra como un numero y se escala adentro, asi que el rechazo
		// llega igual por el camino que el door va a usar en CL-4.
		[TestCase(0)]
		[TestCase(-6)]
		public void TheCommandRefusesItToo(int months)
		{
			Assert.Throws<DomainException>(
				() => ledger.RecordDurableCost(Incurred, 240000m, "CRC", TheSubject, months),
				"El comando duradero EXIGE la vida (L5). No hay valor por defecto que poner: "
				+ "cuanto dura una llanta lo sabe quien la vendio.");
		}

		// Y NADA SE ESCRIBIO: el rechazo deja el libro como estaba, que es E12 -- validar y armar
		// antes de mutar, mutar al final.
		[Test]
		public void AndTheRefusedEntryDidNotReachTheLedger()
		{
			Assert.Throws<DomainException>(
				() => ledger.RecordDurableCost(Incurred, 240000m, "CRC", TheSubject, 0));

			Assert.That(
				ledger.CostEntryCountBetween(Incurred, new DateTime(2026, 12, 31, 0, 0, 0)),
				Is.EqualTo(0));
			Assert.That(
				ledger.HasAnyCostBetween(Incurred, new DateTime(2026, 12, 31, 0, 0, 0)),
				Is.False);
		}

		// LA MITAD ESTRUCTURAL DE "NO PONER UN VALOR POR DEFECTO": no hay donde ponerlo. Ni un campo
		// literal en ADurableCost ni en AnExpectedLife, y §7 del brief nombra la vida por defecto
		// como la segunda invencion prohibida.
		[TestCase(typeof(ADurableCost))]
		[TestCase(typeof(AnExpectedLife))]
		public void AndNoDefaultLifeIsHiddenInALiteralField(Type subject)
		{
			foreach (FieldInfo field in subject.GetFields(
				BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				Assert.That(field.IsLiteral, Is.False,
					subject.Name + "." + field.Name + " es una constante. §7: una vida util por "
					+ "defecto es la segunda invencion prohibida, y colapsa las dos vistas de L6 en "
					+ "una sola sin que nadie lo note.");
		}

		// Y LA VIDA QUE SI LLEGO SE DICE, en meses, tal como llego: es lo que hace visible la regla
		// de G2 en vez de pedir que se le crea.
		[Test]
		public void TheLifeThatDidArriveIsSaidInTheMonthsItArrivedIn()
		{
			ledger.RecordDurableCost(Incurred, 240000m, "CRC", TheSubject, 48);

			string[] said = ledger.CostEntryExpectedLivesBetween(
				Incurred, new DateTime(2026, 1, 31, 0, 0, 0));

			Assert.That(said, Is.EqualTo(new[] { "48 meses" }));
		}

		[Test]
		public void AndARecurringEntrysCellSaysNothingBecauseItHasNoLife()
		{
			ledger.RecordRecurringCost(Incurred, 25000m, "CRC", "Gasolina de enero");

			string[] said = ledger.CostEntryExpectedLivesBetween(
				Incurred, new DateTime(2026, 1, 31, 0, 0, 0));

			Assert.That(said, Is.EqualTo(new[] { "" }),
				"Una celda vacia dice que no tiene vida util. Un cero ahi se leeria como una vida "
				+ "de cero meses, que es otro hecho.");
		}
	}
}
