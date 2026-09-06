using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using DealershipCar.App.Events;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L1 de SLICE-CostLedger.md, APLICADA AL PRECIO PUBLICADO: el monto y la moneda son un solo
	// valor y no se pueden tener aparte. §1.1 del brief encontro la forma contraria ya escrita en este
	// modulo -- un decimal y un string sueltos en ServicePriceEntry, con la pareja guardada a mano en
	// el constructor y la regla de que las monedas no se convierten dicha SOLO como prosa adentro de
	// un mensaje de guarda. DEC-85 decidio migrarlo; esto es la migracion afirmada.
	//
	// LA MIGRACION ES ADENTRO Y SOLO ADENTRO, y las dos mitades se afirman por separado porque son dos
	// leyes distintas. Adentro: ninguna de las dos mitades sueltas queda en el tipo. Afuera: las TRES
	// firmas que las entradas ya escritas nombran siguen letra por letra como estaban, que es la
	// condicion ENTERA de DEC-85 y lo que hace que ningun journal viejo corra riesgo. Sin la segunda
	// mitad, la primera se podria satisfacer migrando el wire, que es la opcion (c) -- medida en
	// PROBE 7 y que pierde la revision entera y no un precio.
	//
	// EL PRECEDENTE ES SU PROPIO HERMANO, en el mismo agregado y publicado por un miembro de la misma
	// forma: ServiceIntervalEntry recibe DOS TIPOS DEL DOMINIO ya armados -- MaintenanceOperationKind
	// y PublishedInterval -- y ningun primitivo, PublishServiceInterval escala los tres primitivos en
	// su propio cuerpo, y las guardas de VALOR viven en PublishedInterval, que es quien tiene los
	// valores, con la regla de que las unidades no se convierten en SU mensaje. El precio queda igual:
	// Money es a la moneda lo que PublishedInterval es a la unidad.
	[TestFixture]
	public sealed class ThePublishedPriceHoldsOneMoneyAndNotTwoFieldsThatCanDisagree
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0);
		private static readonly DateTime ValidUntil = new DateTime(2025, 12, 31, 0, 0, 0);
		private static readonly DateTime WhileItIsInForce = new DateTime(2025, 6, 15, 0, 0, 0);

		// LA MITAD DE ADENTRO. Un decimal suelto o un string suelto en este tipo son exactamente lo
		// que §1.1 encontro, y son dos campos que pueden discrepar: nada los ataba.
		[Test]
		public void TheEntryDeclaresOneMoneyAndNeitherHalfOnItsOwn()
		{
			List<string> declared = new List<string>();

			foreach (FieldInfo field in typeof(ServicePriceEntry).GetFields(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
				declared.Add(field.FieldType.Name + " " + field.Name);

			TestContext.Out.WriteLine("LOS CAMPOS DEL PRECIO -- " + string.Join(", ", declared));

			Assert.That(ThoseOfType(typeof(ServicePriceEntry), typeof(Money)), Is.EqualTo(1),
				"UN dinero, entero. Es el tipo que ServicePriceEntry ya necesitaba y no tenia.");
			Assert.That(ThoseOfType(typeof(ServicePriceEntry), typeof(decimal)), Is.EqualTo(0),
				"Y NINGUN monto suelto. Un decimal aca es medio precio, y medio precio no se puede "
				+ "leer: es la forma que §1.1 encontro.");
			Assert.That(ThoseOfType(typeof(ServicePriceEntry), typeof(string)), Is.EqualTo(0),
				"Y NINGUNA moneda suelta. Con la moneda en un string, la regla de que no se "
				+ "convierten solo se podia decir en prosa adentro de un mensaje.");
		}

		// Y TAMPOCO ENTRAN SUELTAS. Un constructor que recibe las dos mitades y las junta adentro
		// deja el mismo par a un paso de distancia, y §4 del brief prohibe un monto suelto o una
		// moneda suelta como campo O COMO PARAMETRO en cualquier sitio nuevo.
		[Test]
		public void AndNeitherHalfArrivesOnItsOwnEither()
		{
			ConstructorInfo[] built = typeof(ServicePriceEntry).GetConstructors(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			Assert.That(built.Length, Is.EqualTo(1), "Una sola forma de armar un precio.");

			List<string> slots = new List<string>();

			foreach (ParameterInfo slot in built[0].GetParameters())
				slots.Add(slot.ParameterType.Name + " " + slot.Name);

			TestContext.Out.WriteLine("LAS RANURAS DEL CONSTRUCTOR -- " + string.Join(", ", slots));

			foreach (ParameterInfo slot in built[0].GetParameters())
			{
				Assert.That(slot.ParameterType, Is.Not.EqualTo(typeof(decimal)),
					"La ranura '" + slot.Name + "' seria un monto suelto entrando.");
				Assert.That(slot.ParameterType, Is.Not.EqualTo(typeof(string)),
					"La ranura '" + slot.Name + "' seria una moneda suelta entrando.");
			}

			Assert.That(ThoseSlotsOfType(built[0], typeof(Money)), Is.EqualTo(1),
				"El dinero entra ARMADO, que es donde la escalada de E12 pertenece: un solo sitio "
				+ "valida, y adentro nadie vuelve a chequear.");
		}

		// LA GUARDA QUE LA MIGRACION AGREGA, y es la unica que queda del par: un colaborador
		// obligatorio que falta es un defecto de quien llama, y su mensaje NOMBRA LA OPERACION, que es
		// lo que el mensaje del dinero no puede hacer. Es la misma forma que su hermano: el segundo
		// guard de ServiceIntervalEntry tambien nombra {kind.Name}.
		[Test]
		public void APriceWithNoMoneyAtAllIsRefusedAndTheRefusalNamesTheOperation()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => new ServicePriceEntry(
					MaintenanceOperationKind.OfCode(OperationKindCode.Inspection),
					new PublishedMilestone(10000, DistanceUnit.OfCode(DistanceUnitCode.UnitA)),
					null, ValidFrom, ValidUntil));

			Assert.That(error.Message, Does.Contain("Inspection"),
				"El mensaje dice DE QUE OPERACION es el precio que no se pudo publicar. El del "
				+ "dinero nombra el monto y la moneda, que es todo lo que el dinero tiene: en un "
				+ "lote de diez precios, el que dice cual fallo es este.");
			Assert.That(error.Message, Does.Contain("dinero"));
		}

		// LA MITAD DE AFUERA, PRIMERA FIRMA: la del dominio, que es la que el TEXTO de cada entrada ya
		// escrita nombra. Se afirman los ocho tipos EN ORDEN: cambiar el orden es tan fatal como
		// cambiar un tipo, y una cuenta de ocho no lo veria.
		[Test]
		public void TheDomainMembersEightSlotsDidNotMove()
		{
			Assert.That(
				TheSlotsOf(typeof(ModelSpecification), "PublishServicePrice"),
				Is.EqualTo(new[]
				{
					"String revisionCode", "OperationKindCode kind",
					"Int32 milestoneDistance", "DistanceUnitCode milestoneUnit",
					"Decimal amount", "String currency",
					"DateTime validFrom", "DateTime validUntil"
				}),
				"Es la condicion ENTERA de DEC-85. PROBE 7 midio que renombrar este miembro deja el "
				+ "precio Y el sello de su revision fallando en cada arranque futuro, asi que la "
				+ "perdida es la revision completa y no un precio.");
		}

		// SEGUNDA FIRMA: la de la puerta. Los codigos viajan como texto y el monto como decimal, tal
		// como estaban.
		[Test]
		public void TheDoorsEightSlotsDidNotMoveEither()
		{
			Assert.That(
				TheSlotsOf(typeof(DealershipCarAPI), "PublishServicePrice"),
				Is.EqualTo(new[]
				{
					"String revisionCode", "String kind",
					"Int32 milestoneDistance", "String milestoneUnit",
					"Decimal amount", "String currency",
					"DateTime validFrom", "DateTime validUntil"
				}));
		}

		// TERCERA FIRMA: el mensaje del feed. Sus dos mitades del dinero siguen siendo dos campos, y
		// eso es CORRECTO: es lo que llega por el wire, y el wire no se migro. Quien las junta es
		// ModelSpecification, un nivel adentro.
		[Test]
		public void AndTheFeedMessageStillCarriesTheTwoHalvesSeparately()
		{
			Assert.That(
				typeof(ServicePricePublished).GetProperty("Amount").PropertyType,
				Is.EqualTo(typeof(decimal)));
			Assert.That(
				typeof(ServicePricePublished).GetProperty("Currency").PropertyType,
				Is.EqualTo(typeof(string)));
		}

		// UN SOLO AUTOR DE LA FRASE DEL MONTO. El precio no dice el numero: se lo pregunta al dinero,
		// que se lo pregunta a su moneda, que es quien tiene el simbolo y los decimales. Sin esto la
		// migracion podria haber dejado el precio formateando por su cuenta y las dos frases
		// discreparian el dia que una de las dos cambiara.
		[Test]
		public void ThePriceSaysTheAmountWithTheMoneysOwnWordsAndComposesNothingOfItsOwn()
		{
			ModelSpecification specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishServicePrice(
				TheRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", ValidFrom, ValidUntil);
			specification.SealRevision(TheRevision);

			string said = specification.DescribeServicePrice(
				OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, WhileItIsInForce);

			Assert.That(said, Does.Contain("₡120.50"),
				"EL ANCLA, contra el literal y no contra la otra respuesta: dos consumidores de un "
				+ "mismo miembro concuerdan por construccion, incluso cuando el miembro esta mal "
				+ "(E49). El literal es lo unico que mide al miembro.");
			Assert.That(
				said, Does.Contain(new Money(120.50m, Currency.CRC).Expressed()),
				"Y la mitad estructural: la frase del precio lleva la del dinero adentro, entera.");
		}

		private static int ThoseOfType(Type declaring, Type wanted)
		{
			int found = 0;

			foreach (FieldInfo field in declaring.GetFields(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
				if (field.FieldType == wanted) found = found + 1;

			return found;
		}

		private static int ThoseSlotsOfType(ConstructorInfo built, Type wanted)
		{
			int found = 0;

			foreach (ParameterInfo slot in built.GetParameters())
				if (slot.ParameterType == wanted) found = found + 1;

			return found;
		}

		private static string[] TheSlotsOf(Type declaring, string member)
		{
			MethodInfo found = declaring.GetMethod(
				member, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			Assert.That(found, Is.Not.Null, "No existe " + declaring.Name + "." + member + ".");

			List<string> slots = new List<string>();

			foreach (ParameterInfo slot in found.GetParameters())
				slots.Add(slot.ParameterType.Name + " " + slot.Name);

			TestContext.Out.WriteLine(
				declaring.Name + "." + member + " -- " + string.Join(", ", slots));

			return slots.ToArray();
		}
	}
}
