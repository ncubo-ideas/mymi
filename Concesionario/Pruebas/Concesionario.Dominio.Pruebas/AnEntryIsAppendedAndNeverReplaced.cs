using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// §10.4 DE SLICE-CostLedger.md PIDE UNA DECISION EXPLICITA: la cuarta fila de la tabla de comandos
	// es "corregir una entrada ya registrada", y dice "es una DECISION si esto existe o no en este
	// slice; si no existe, decirlo".
	//
	// NO EXISTE, Y ESTE ARCHIVO ES DONDE ESO SE AFIRMA EN VEZ DE QUEDAR COMO UN OLVIDO. El argumento
	// es la CLAVE: RecordDelivery es correctivo (DEC-23) porque una entrega es UN hecho de UN vehiculo
	// y la segunda que llega es obviamente la correccion de la primera. Una entrada del libro no tiene
	// clave de ninguna clase -- lleva un instante en que se incurrio y un asunto recibido, y dos
	// gastos del mismo monto en el mismo asunto el mismo dia son dos gastos y no una correccion.
	// Corregir necesitaria una clave, y la unica clave disponible seria una que este modulo INVENTARA:
	// un identificador de entrada, un ordinal, o la terna (instante, asunto, monto). Las tres son
	// hechos que nadie recibe.
	//
	// Y COPIAR LA FORMA DEL PRECIO ES LA FUSION DE DEC-48/DEC-36 EN UN TERCER SITIO. Un precio SI
	// tiene clave -- la operacion y el hito -- y por eso PublishedRevision tiene un
	// ThePlaceOfAPublishedPrice y una publicacion sobre la misma clave reemplaza. Darle al libro un
	// place-of-an-entry seria darle al costo la forma del precio, que es lo que §2.2 existe para
	// prevenir: dos hechos de forma distinta, de autores distintos, con vidas distintas.
	//
	// EL COSTO DE LA DECISION, DICHO: un gasto mal registrado se corrige registrando lo que de verdad
	// paso, y las dos entradas quedan. La alternativa cuesta una clave inventada; esta cuesta que el
	// libro pueda tener una entrada equivocada para siempre. La segunda es la que el journal ya
	// impone: es append-only, y las entradas de un journal no se editan.
	[TestFixture]
	public sealed class AnEntryIsAppendedAndNeverReplaced
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheSubject = "Gasolina de enero";

		private static readonly DateTime TheStart = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheEnd = new DateTime(2026, 12, 31, 0, 0, 0);
		private static readonly DateTime TheSameInstant = new DateTime(2026, 1, 15, 9, 0, 0);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);
		}

		// LA DECISION, AFIRMADA POR VALOR: dos entradas iguales en todo son DOS entradas. Si el libro
		// reemplazara por alguna clave, aca habria una.
		[Test]
		public void TwoEntriesAlikeInEverythingAreTwoEntriesAndNotACorrection()
		{
			ledger.RecordRecurringCost(TheSameInstant, 25000m, "CRC", TheSubject);
			ledger.RecordRecurringCost(TheSameInstant, 25000m, "CRC", TheSubject);

			Assert.That(ledger.CostEntryCountBetween(TheStart, TheEnd), Is.EqualTo(2),
				"Dos gastos del mismo monto el mismo dia en lo mismo son dos gastos. Tratarlos como "
				+ "una correccion pediria una clave que nadie recibio.");
			Assert.That(TheSentenceTheDoorWouldCompose.TheSpend(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord), Does.Contain("₡50.000,00"),
				"Y los dos cuentan en el total, que es la consecuencia y no un efecto secundario.");
		}

		// LA MITAD ESTRUCTURAL: ningun comando corrige, y no hay ningun miembro que busque el sitio de
		// una entrada. Es la forma en que MS-3 fijo su propia negativa -- afirmar la ausencia por
		// reflexion -- porque una negativa no se puede probar llamando.
		[Test]
		public void AndTheLedgerHasNoCorrectionCommandAndNoPlaceOfAnEntry()
		{
			foreach (MethodInfo member in typeof(CostLedger).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
				foreach (string word in new[]
					{
						"Correct", "Amend", "Replace", "Revise", "ThePlaceOf", "Find", "Remove",
						"Delete", "Edit"
					})
					Assert.That(member.Name, Does.Not.Contain(word),
						"CostLedger." + member.Name + " corrige o direcciona una entrada. §10.4: en "
						+ "este slice no existe, y existir pediria una clave que este modulo "
						+ "tendria que inventar. ThePlaceOf es el nombre que PublishedRevision usa "
						+ "para buscar un precio por su clave, y ese es el miembro que un costo no "
						+ "puede tener: no hay clave con que buscarlo. Place a secas SI se permite "
						+ "-- es la insercion en la linea de tiempo, la misma que ShopStay hace.");
		}

		// Y NINGUNA ENTRADA ES REEMPLAZABLE, tampoco desde adentro: todos sus campos son readonly y no
		// hay ningun miembro que mute una. Sin esto, "no hay comando de correccion" seria cierto y una
		// entrada seguiria siendo mutable para quien la tuviera en la mano.
		[TestCase(typeof(ACostEntry))]
		[TestCase(typeof(ARecurringCost))]
		[TestCase(typeof(ADurableCost))]
		public void AndNoEntryIsMutableOnceItIsBuilt(Type entry)
		{
			foreach (FieldInfo field in entry.GetFields(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
				Assert.That(field.IsInitOnly, Is.True,
					entry.Name + "." + field.Name + " se puede mutar. Una entrada del libro es un "
					+ "hecho que paso: cambiarla es reescribir el pasado, y en un registro "
					+ "append-only ademas es contradictorio.");

			List<string> mutating = new List<string>();

			foreach (MethodInfo member in entry.GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
				if (member.ReturnType == typeof(void) && !member.IsSpecialName)
					mutating.Add(member.Name);

			Assert.That(mutating, Is.Empty,
				entry.Name + " declara miembros que no contestan nada: "
				+ string.Join(", ", mutating) + ". Una entrada solo contesta.");
		}

		// Y LAS ENTRADAS QUEDAN EN ORDEN DEL INSTANTE EN QUE SE INCURRIERON, no en el orden en que
		// llegaron: una que llega tarde entra en su sitio, como ShopStay hace con las estadias.
		[Test]
		public void AndAnEntryThatArrivesLateTakesItsPlaceOnTheIncurredTimeline()
		{
			ledger.RecordRecurringCost(
				new DateTime(2026, 3, 20, 9, 0, 0), 40000m, "CRC", "Cambio de aceite");
			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 25000m, "CRC", TheSubject);

			Assert.That(ledger.CostEntryInstantsBetween(TheStart, TheEnd),
				Is.EqualTo(new[] { "2026-01-15 09:00:00", "2026-03-20 09:00:00" }),
				"La de enero llego segunda y sale primera: las filas dicen el registro, no el orden "
				+ "en que la sucursal se acordo de mandarlo.");
		}
	}
}
