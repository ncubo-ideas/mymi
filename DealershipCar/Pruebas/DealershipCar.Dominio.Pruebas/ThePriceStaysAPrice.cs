using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L10 de SLICE-CostLedger.md, LA MITAD QUE EXISTE -- y el nombre dice cual, a proposito.
	//
	// L10 ENTERA dice: "ninguna entrada del libro se deriva de un precio publicado, y ningun precio
	// publicado lo corrige una entrada del libro. Los dos se leen uno al lado del otro; ninguno es la
	// fuente del otro." §8 del plan la pide como ThePriceStaysAPriceAndTheLedgerStaysALedger, con un
	// precio publicado y una entrada del libro PARA LA MISMA OPERACION, distintos, los dos legibles.
	//
	// ESA PRUEBA NO SE PUEDE ESCRIBIR TODAVIA, Y ESTE NOMBRE ES LA MITAD QUE SI. En CL-2 no existe
	// ningun tipo de entrada del libro: ACostEntry, CostLedger, ARecurringCost y ADurableCost son de
	// CL-3, y se verifico en el arbol antes de escribir esto -- Dominio/ no declara ninguno de los
	// doce nombres del canon. Un precio y una entrada que difieren necesita las DOS cosas; con una
	// sola no hay comparacion que hacer.
	//
	// ASI QUE SE PARTE EN DOS Y SE DICE CUAL QUEDA SOSTENIDA:
	//
	//   SOSTENIDA aca -- el lado del precio, entero: ningun miembro de ServicePriceEntry ni de Money
	//   nombra, recibe ni contesta nada de un costo. El dia que alguien le de al precio un
	//   colaborador del libro, o un miembro que derive uno del otro, esto se pone rojo.
	//
	//   NO SOSTENIDA -- "los dos legibles, distintos, ninguno derivado del otro": es una deuda de
	//   CL-3, y esta escrita como deuda en el bloque de CL-2 en vez de quedar implicita. Nombrar esta
	//   clase con la ley entera seria el defecto que OD-6 §3 midio: una prueba nombrada por una ley
	//   que solo afirma la mitad.
	//
	// Y LA SEGUNDA PRUEBA ES UN CABLE TRAMPA para que la deuda no se pueda pagar en silencio: afirma
	// que ninguno de los tipos del libro existe TODAVIA. Quien agregue el primero se pone rojo en la
	// entrada y tiene que completar la ley aca mismo, que es la forma que MS-3 uso para
	// NoPublishedPrice y que funciono.
	[TestFixture]
	public sealed class ThePriceStaysAPrice
	{
		// §2.2: un costo NO es un precio. Es la prohibicion de DEC-48 y DEC-36 en un tercer sitio --
		// dos hechos de forma distinta, de autores distintos, con vidas distintas. Un precio que
		// llevara un costo adentro se habria vuelto un reporte de variacion contra lo cotizado, y este
		// modulo no tiene tal cosa.
		private static readonly string[] TheWordsOfTheLedger =
			{ "Cost", "Ledger", "Spend", "Spent", "Amortiz", "Incurred" };

		// Los doce nombres del canon de §4 del brief. CL-3 los escribe; en CL-2 no existe ninguno.
		private static readonly string[] TheTypesTheLedgerWillBring =
			{
				"CostLedger", "ACostEntry", "ARecurringCost", "ADurableCost", "AnExpectedLife",
				"ASpendSummary", "AnAmortizedShare", "ASpendComparison", "ACostPerDistance",
				"ASpendNotSaidAcrossCurrencies", "NoCostOnRecord", "CostSubject"
			};

		[TestCase(typeof(ServicePriceEntry))]
		[TestCase(typeof(Money))]
		public void NoMemberOfThePriceSideKnowsAnythingAboutACost(Type subject)
		{
			List<string> read = new List<string>();

			foreach (MemberInfo member in subject.GetMembers(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
			{
				read.Add(member.Name);

				foreach (string word in TheWordsOfTheLedger)
					Assert.That(member.Name, Does.Not.Contain(word),
						"El miembro " + subject.Name + "." + member.Name + " nombra el libro de "
						+ "costos. Un precio es lo que el fabricante PUBLICO y una entrada del libro "
						+ "es lo que de verdad se gasto: dos hechos, y ninguno es la fuente del otro.");
			}

			TestContext.Out.WriteLine(
				"LOS MIEMBROS DE " + subject.Name + " (" + read.Count + ") -- "
				+ string.Join(", ", read));
		}

		// Y NI RECIBEN NI CONTESTAN NINGUNO. El nombre del miembro no alcanza: un colaborador del
		// libro entrando por una ranura llamada 'other' pasaria la prueba de arriba entera.
		[TestCase(typeof(ServicePriceEntry))]
		[TestCase(typeof(Money))]
		public void AndNoMemberOfThePriceSideTakesOrAnswersOneEither(Type subject)
		{
			foreach (MethodBase member in TheMethodsAndConstructorsOf(subject))
			{
				foreach (ParameterInfo slot in member.GetParameters())
					TheTypeIsNotOfTheLedger(
						slot.ParameterType, subject.Name + "." + member.Name + "(" + slot.Name + ")");

				MethodInfo answering = member as MethodInfo;

				if (answering != null)
					TheTypeIsNotOfTheLedger(
						answering.ReturnType, subject.Name + "." + member.Name + " contesta");
			}

			foreach (FieldInfo field in subject.GetFields(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				TheTypeIsNotOfTheLedger(
					field.FieldType, subject.Name + "." + field.Name + " guarda");
		}

		// EL CABLE TRAMPA. No es una prueba de que el libro este bien: es una prueba de que la mitad
		// no sostenida de L10 sigue sin sostener, y de que quien la haga sostenible lo sepa.
		[Test]
		public void AndNoTypeOfTheLedgerExistsYetSoTheOtherHalfOfTheLawIsStillADebt()
		{
			List<string> already = new List<string>();

			foreach (Type declared in typeof(Money).Assembly.GetTypes())
				foreach (string coming in TheTypesTheLedgerWillBring)
					if (declared.Name == coming) already.Add(declared.Name);

			Assert.That(already, Is.Empty,
				"Llegaron los tipos del libro. Entonces la mitad de L10 que CL-2 dejo como deuda YA "
				+ "SE PUEDE afirmar, y hay que afirmarla aca: un precio publicado y una entrada del "
				+ "libro para la misma operacion, distintos, los dos legibles, y ninguno derivado del "
				+ "otro. Este rojo es la deuda venciendo, no una regresion.");
		}

		private static void TheTypeIsNotOfTheLedger(Type found, string where)
		{
			foreach (string coming in TheTypesTheLedgerWillBring)
				Assert.That(found.Name, Is.Not.EqualTo(coming),
					where + " es del libro de costos. §2.2: los dos hechos se leen uno al lado del "
					+ "otro y no se fusionan nunca.");

			foreach (string word in TheWordsOfTheLedger)
				Assert.That(found.Name, Does.Not.Contain(word),
					where + " nombra el libro de costos.");
		}

		private static IEnumerable<MethodBase> TheMethodsAndConstructorsOf(Type subject)
		{
			List<MethodBase> found = new List<MethodBase>();

			foreach (ConstructorInfo built in subject.GetConstructors(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
				| BindingFlags.DeclaredOnly))
				found.Add(built);

			foreach (MethodInfo answering in subject.GetMethods(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				found.Add(answering);

			return found;
		}
	}
}
