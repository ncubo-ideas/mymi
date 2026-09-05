using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L4 de SLICE-CostLedger.md: "duradero o recurrente LLEGA CON LA ENTRADA. El dominio nunca lo
	// infiere del asunto, del monto ni de la operacion -- inferirlo lo volveria autor de la
	// distincion que ES la respuesta (§2.1), y es la primera invencion prohibida con una heuristica
	// puesta."
	//
	// ESTA PRUEBA ES ESTRUCTURAL Y NO SE PUEDE ESCRIBIR DE OTRA FORMA. §7.2 del plan lo dice: la
	// clasificacion llega POR CUAL COMANDO SE LLAMO, asi que no hay ningun parametro de clasificacion
	// que omitir y ninguna vida en el camino recurrente. Los tests 4 y 6 del brief -- "una entrada
	// sin clasificacion es rechazada" y "una recurrente con vida: decidir y probar la decision" --
	// quedan satisfechos ESTRUCTURALMENTE y no por dos guardas, y este archivo es donde eso se dice
	// en vez de fingir que las guardas existen. No se puede pasar una clasificacion que contradiga al
	// comando porque no hay ranura donde ponerla, y no se puede dar una vida a una recurrente porque
	// el comando recurrente no la toma.
	//
	// C57 ES LA REGLA DEL CENSO: se enumeran los SITIOS DE LLAMADA de la regla y no los sustantivos
	// de los que la regla habla. Los sitios son (a) los comandos del agregado, (b) los constructores
	// de las dos entradas, (c) el conjunto cerrado en si. Los tres se enumeran por reflexion, asi que
	// un cuarto comando o un tipo nuevo entra al censo sin que nadie edite una lista.
	[TestFixture]
	public sealed class TheClassificationIsReceivedByWhichCommandWasCalled
	{
		private const string TheVin = "1FTFW1ET5DFC10312";

		private static readonly string[] TheWordsThatWouldNameAClassificationSlot =
			{ "classification", "clasificacion", "kind", "type", "durable", "recurring" };

		// (a) NINGUN COMANDO DEL AGREGADO TOMA UNA CLASIFICACION. Los comandos son los miembros void.
		[Test]
		public void NoCommandOfTheLedgerTakesAClassificationOfAnyShape()
		{
			List<string> read = new List<string>();

			foreach (MethodInfo command in TheCommandsOf(typeof(CostLedger)))
				foreach (ParameterInfo slot in command.GetParameters())
				{
					read.Add(command.Name + "(" + slot.ParameterType.Name + " " + slot.Name + ")");

					Assert.That(slot.ParameterType, Is.Not.EqualTo(typeof(CostClassification)),
						command.Name + " recibe la clasificacion como dato. L4: llega por CUAL "
						+ "comando se llamo, no por una ranura que alguien puede llenar mal.");

					foreach (string word in TheWordsThatWouldNameAClassificationSlot)
						Assert.That(slot.Name.ToLowerInvariant(), Does.Not.Contain(word),
							command.Name + " tiene una ranura llamada '" + slot.Name + "'. Una "
							+ "clasificacion entrando con otro nombre es la misma invencion con un "
							+ "esquema puesto.");
				}

			Assert.That(read, Is.Not.Empty, "El agregado no declara ningun comando que censar.");

			TestContext.Out.WriteLine(
				"LAS RANURAS DE LOS COMANDOS (" + read.Count + ") -- " + string.Join(", ", read));
		}

		// (b) Y NINGUNA DE LAS DOS ENTRADAS LA RECIBE TAMPOCO. Cada subtipo CONTESTA la suya, que es
		// otra cosa: es su comportamiento y por eso gana su tipo (E11, E39).
		[TestCase(typeof(ARecurringCost))]
		[TestCase(typeof(ADurableCost))]
		public void NeitherEntryTakesItsOwnClassificationAsAnArgument(Type entry)
		{
			foreach (ConstructorInfo built in entry.GetConstructors(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
				| BindingFlags.DeclaredOnly))
				foreach (ParameterInfo slot in built.GetParameters())
					Assert.That(slot.ParameterType, Is.Not.EqualTo(typeof(CostClassification)),
						entry.Name + " recibe su clasificacion. El tipo ES la clasificacion: si "
						+ "llegara por parametro, una recurrente podria construirse diciendo que es "
						+ "duradera.");
		}

		[Test]
		public void EachEntryAnswersTheClassificationItsOwnTypeIs()
		{
			CostLedger ledger = new CostLedger(TheVin);

			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 25000m, "CRC", "Gasolina de enero");
			ledger.RecordDurableCost(
				new DateTime(2026, 1, 20, 9, 0, 0), 240000m, "CRC", "Llantas nuevas", 48);

			string[] said = ledger.CostEntryClassificationsBetween(
				new DateTime(2026, 1, 1, 0, 0, 0), new DateTime(2026, 1, 31, 0, 0, 0));

			Assert.That(said, Is.EqualTo(new[] { "Recurring", "Durable" }),
				"La columna lleva el NOMBRE del miembro, que es la mitad direccionable, y en el "
				+ "orden en que se incurrieron.");
		}

		// (c) LA VIDA NO ESTA EN EL CAMINO RECURRENTE, y esto es el test 6 del brief resuelto por
		// estructura: no es que una vida en una recurrente se rechace o se ignore -- es que NO HAY
		// DONDE PONERLA. Se afirma por reflexion en los dos sitios donde podria estar.
		[Test]
		public void ThereIsNoPlaceToPutAnExpectedLifeOnTheRecurringPath()
		{
			MethodInfo recurring = typeof(CostLedger).GetMethod("RecordRecurringCost",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(recurring, Is.Not.Null);

			foreach (ParameterInfo slot in recurring.GetParameters())
				Assert.That(slot.ParameterType, Is.Not.EqualTo(typeof(AnExpectedLife)),
					"El comando recurrente toma una vida util. Una recurrente se consume en su "
					+ "periodo: una vida ahi seria un dato que el dominio tendria que ignorar, y un "
					+ "dato ignorado es un dato que alguien va a creer.");

			Assert.That(typeof(ARecurringCost).GetFields(
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
					| BindingFlags.DeclaredOnly),
				Is.Empty,
				"ARecurringCost declara un campo propio. No tiene ninguno que declarar: todo lo que "
				+ "una recurrente tiene es lo que toda entrada tiene.");
		}

		// Y EL CONJUNTO TIENE EXACTAMENTE DOS, porque §11 del brief lo dice: "una ACostEntry tiene una
		// CLASIFICACION, no un ciclo de vida", y un tercer miembro seria un estado con otra ropa.
		[Test]
		public void TheClassificationHasExactlyTheTwoTheCanonNames()
		{
			Assert.That(CostClassification.Durable.Name, Is.EqualTo("Durable"));
			Assert.That(CostClassification.Recurring.Name, Is.EqualTo("Recurring"));

			Assert.That(
				ReferenceEquals(CostClassification.Durable, CostClassification.Recurring),
				Is.False);
		}

		private static IEnumerable<MethodInfo> TheCommandsOf(Type aggregate)
		{
			List<MethodInfo> found = new List<MethodInfo>();

			foreach (MethodInfo member in aggregate.GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
				if (member.ReturnType == typeof(void) && !member.IsSpecialName
					&& (member.IsPublic || member.IsAssembly))
					found.Add(member);

			return found;
		}
	}
}
