using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L7 de SLICE-CostLedger.md: "la parte amortizada se calcula a partir de la entrada y del
	// periodo EN CADA LECTURA. Ningun total corriente mantenido, ninguna asignacion guardada, ningun
	// calendario de partes futuras."
	//
	// LAS DOS MITADES SON CIEGAS UNA A LA OTRA Y POR ESO ESTAN LAS DOS. El censo de campos no puede
	// ver una parte que se calcula bien pero siempre igual; los tres periodos no pueden ver un campo
	// cacheado, porque tres lecturas de un cache recien llenado contestan lo correcto. Y el censo
	// tiene que cubrir LOS DOS LADOS: un total corriente en el AGREGADO es invisible a un censo sobre
	// la entrada, y una parte guardada en la ENTRADA es invisible a un censo sobre el agregado.
	//
	// LOS NUMEROS SON EXACTOS A PROPOSITO. ₡240000 con 4 meses de vida desde el 1 de enero de 2026
	// reparten sobre una ventana de 120 dias -- enero 31, febrero 28, marzo 31, abril 30 -- o sea
	// ₡2000 por dia. Asi que 31 dias son ₡62000, 28 son ₡56000 y 15 son ₡30000, y las tres cifras se
	// escriben como literales en vez de recalcularse aca: E49 pide que UN lado este anclado a lo que
	// se recibio, y una prueba que rehace la cuenta del dominio no ancla nada.
	//
	// LA MITAD DEL JOURNAL ES DE CL-5. El test 9 del brief pide ademas que el journal no crezca entre
	// las tres lecturas; eso necesita el door y una carpeta, y es el [REPLAY] de CL-5. Lo que se
	// afirma aca es lo que a nivel A se puede afirmar, dicho en vez de omitido.
	[TestFixture]
	public sealed class AShareIsDerivedAndNeverStored
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheTyres = "Llantas nuevas";

		private static readonly DateTime TheTyresWereBought = new DateTime(2026, 1, 1, 0, 0, 0);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);
			ledger.RecordDurableCost(TheTyresWereBought, 240000m, "CRC", TheTyres, 4);
		}

		// TRES PERIODOS, TRES PARTES, cada una por literal.
		[TestCase("2026-01-01", "2026-02-01", "₡62.000,00")]
		[TestCase("2026-02-01", "2026-03-01", "₡56.000,00")]
		[TestCase("2026-03-01", "2026-03-16", "₡30.000,00")]
		public void TheSameDurableEntryReadFromThreePeriodsAnswersThreeDifferentShares(
			string from, string to, string theShare)
		{
			string said = TheSentenceTheDoorWouldCompose.TheSpend(ledger, TheInstant(from), TheInstant(to), TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain(theShare),
				"La parte se deriva del periodo preguntado. Una parte guardada contestaria la misma "
				+ "en los tres.");
		}

		// Y LEERLA DOS VECES NO LA MUEVE NI LA ACUMULA: un total corriente mantenido daria ₡124000 la
		// segunda vez, que es exactamente el defecto que L7 nombra.
		[Test]
		public void AndReadingItTwiceDoesNotAccumulateAnything()
		{
			DateTime from = TheInstant("2026-01-01");
			DateTime to = TheInstant("2026-02-01");

			string first = TheSentenceTheDoorWouldCompose.TheSpend(ledger, from, to, TheLanguage.OfTheRecord);
			string second = TheSentenceTheDoorWouldCompose.TheSpend(ledger, from, to, TheLanguage.OfTheRecord);

			Assert.That(second, Is.EqualTo(first));
			Assert.That(second, Does.Not.Contain("₡124000"),
				"Un total corriente mantenido sumaria la parte otra vez en cada lectura.");
		}

		// EL CENSO, LADO DE LA ENTRADA: ADurableCost guarda exactamente lo que recibio, y ni una parte
		// ni un calendario de partes futuras.
		[Test]
		public void TheDurableEntryStoresOnlyWhatItReceived()
		{
			List<string> fields = new List<string>();

			foreach (FieldInfo field in TheFieldsOf(typeof(ADurableCost)))
			{
				fields.Add(field.FieldType.Name + " " + field.Name);

				Assert.That(field.FieldType, Is.Not.EqualTo(typeof(AnAmortizedShare)),
					"ADurableCost." + field.Name + " guarda una parte amortizada. La parte depende "
					+ "del periodo preguntado, asi que guardarla es guardar la respuesta a una "
					+ "pregunta que nadie hizo todavia.");
			}

			fields.Sort(StringComparer.Ordinal);

			Assert.That(fields, Is.EqualTo(new[] { "AnExpectedLife _life" }),
				"ADurableCost declara " + string.Join(", ", fields) + ". Lo unico que una duradera "
				+ "tiene de mas que cualquier entrada es su vida util.");
		}

		// EL CENSO, LADO DEL AGREGADO: ningun campo del libro guarda dinero ni una parte. Es la mitad
		// que el censo sobre la entrada no puede ver.
		[Test]
		public void AndTheLedgerItselfKeepsNoRunningTotalAndNoStoredShare()
		{
			List<string> fields = new List<string>();

			foreach (FieldInfo field in TheFieldsOf(typeof(CostLedger)))
			{
				fields.Add(field.FieldType.Name + " " + field.Name);

				Assert.That(field.FieldType, Is.Not.EqualTo(typeof(Money)),
					"CostLedger." + field.Name + " guarda dinero. Todo total se compone en la "
					+ "lectura: uno guardado es una segunda respuesta que puede no coincidir con la "
					+ "primera.");
				Assert.That(field.FieldType, Is.Not.EqualTo(typeof(AnAmortizedShare)),
					"CostLedger." + field.Name + " guarda una parte amortizada.");
				Assert.That(field.FieldType, Is.Not.EqualTo(typeof(ASpendSummary)),
					"CostLedger." + field.Name + " guarda un resumen de periodo. Un resumen es la "
					+ "respuesta a un periodo, y los periodos los pregunta quien consulta.");
			}

			fields.Sort(StringComparer.Ordinal);

			Assert.That(fields, Is.EqualTo(new[] { "List`1 _entries", "Vin _vin" }),
				"CostLedger declara " + string.Join(", ", fields) + ". Guarda el vehiculo y las "
				+ "entradas, y nada mas: lo demas se compone.");
		}

		// Y NINGUN TIPO DEL LIBRO GUARDA UNA PARTE, censado sobre el ensamblado y no sobre una lista
		// escrita a mano: un tipo nuevo que la guarde entra al censo solo.
		[Test]
		public void AndNoTypeInTheDomainStoresAnAmortizedShareAtAll()
		{
			List<string> guilty = new List<string>();

			foreach (Type declared in typeof(CostLedger).Assembly.GetTypes())
			{
				if (declared == typeof(AnAmortizedShare)) continue;

				foreach (FieldInfo field in TheFieldsOf(declared))
					if (field.FieldType == typeof(AnAmortizedShare)
						|| field.FieldType == typeof(AnAmortizedShare[]))
						guilty.Add(declared.Name + "." + field.Name);
			}

			Assert.That(guilty, Is.Empty,
				"Tipos que guardan una parte amortizada: " + string.Join(", ", guilty) + ".");
		}

		private static IEnumerable<FieldInfo> TheFieldsOf(Type subject)
		{
			List<FieldInfo> found = new List<FieldInfo>();

			foreach (FieldInfo field in subject.GetFields(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (!field.IsLiteral) found.Add(field);

			return found;
		}

		private static DateTime TheInstant(string day)
		{
			return DateTime.ParseExact(
				day, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
		}
	}
}
