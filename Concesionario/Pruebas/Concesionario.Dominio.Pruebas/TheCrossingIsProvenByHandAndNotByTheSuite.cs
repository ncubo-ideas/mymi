using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBA 12 DE §12, Y ES LA UNICA QUE UNA SUITE VERDE NO PRUEBA. "Sacar la llave de union por la
	// tool MCP y confirmar que un lector que no sabe nada de este modulo la puede cruzar contra la
	// placa publicada del OTRO modulo para el mismo carro." Es toda la rebanada, y es la leccion de
	// SLICE-ShopStay.md L3 en un segundo sitio.
	//
	// POR QUE LA SUITE NO PUEDE. Los modulos no se hablan: Dominio lleva cero referencias, ningun
	// modulo referencia el ensamblado de otro, y ESTE proyecto de pruebas tampoco. Asi que no hay nada
	// aca que pueda preguntarle al otro lado que publico. La comparacion contra su regla se hace con
	// una TRANSCRIPCION -- TheJoinKeyIsNormalizedTheSameWayOnBothSides -- y una transcripcion prueba
	// que las dos reglas coinciden HOY en trece entradas, no que el cruce ocurra.
	//
	// Y ESO NO SE DEJA COMO UNA PROMESA EN UN COMENTARIO. La razon por la que la suite no puede verlo
	// es FALSIFICABLE y esta afirmada abajo: el dia que alguien meta una referencia al otro modulo,
	// esta prueba se pone roja y la obligacion a mano cambia -- pero tambien se rompio "ningun modulo
	// referencia el ensamblado de otro", que es una regla de todo el repo.
	//
	// LO QUE LP-6 CORRE A MANO, escrito aca para que este donde se busca:
	//
	//   1. levantar el servidor MCP contra un journal con la placa registrada;
	//   2. tools/list y confirmar que la tool de la placa aparece -- un renglon .WithTools<>() que
	//      falta no lo atrapa NADA de la suite, medido dos veces;
	//   3. pedir la placa por la tool y quedarse con la LLAVE;
	//   4. cruzarla contra la placa publicada del otro modulo para el mismo carro;
	//   5. y confirmar que la descripcion de la tool alcanzo para que el asistente supiera que las dos
	//      son la misma clase de cosa, SIN nombrar al otro modulo (G4).
	[TestFixture]
	public sealed class TheCrossingIsProvenByHandAndNotByTheSuite
	{
		private const string TheVin = ADealershipNarrative.TheVin;

		// La placa del canon del concesionario, y la llave que el otro modulo produce para ella.
		private const string ThePlateTheCanonWrites = "BQR-482";
		private const string TheKeyTheOtherModulePublishes = "BQR482";

		private static readonly DateTime TheDayItWasAssigned =
			new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItAsks =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		// LO MAS FUERTE QUE LA SUITE PUEDE DECIR: la llave que este modulo entregaria es, caracter por
		// caracter, la que el otro publica para el mismo carro.
		[Test]
		public void TheJoinKeyThisModuleHandsOutIsTheOneTheOtherModulePublishesForTheSameCar()
		{
			using (AScheduleOnARealJournal journal =
				new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				APlateOnTheDoorAndInTheDomain.RecordAssignment(
					journal.Api, ThePlateTheCanonWrites, TheDayItWasAssigned);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.JoinKeyAsOf(journal.Api, TheDayItAsks),
					Is.EqualTo(TheKeyTheOtherModulePublishes),
					"La llave que sale de esta puerta no es la que el otro modulo publica para "
					+ "BQR-482. UN CRUCE QUE NO COINCIDE NO DA ERROR: devuelve nada.");

				// Y LAS DOS FORMAS SON DISTINTAS, o el cruce no estaria probando nada: la publicada
				// lleva guion y la llave no.
				Assert.That(
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, TheDayItAsks),
					Is.EqualTo(ThePlateTheCanonWrites));

				Assert.That(ThePlateTheCanonWrites, Is.Not.EqualTo(TheKeyTheOtherModulePublishes));
			}
		}

		// Y ESTO ES LO QUE LA SUITE NO PUEDE VER, dicho como PRUEBA y no como omision silenciosa. La
		// razon esta afirmada: nada de lo que este ensamblado referencia es el otro modulo.
		[Test]
		public void AndTheSuiteCannotSeeTheOtherModuleAtAllWhichIsWhyTestTwelveIsRunByHand()
		{
			var referenced = new StringBuilder();

			foreach (AssemblyName each in
				typeof(TheCrossingIsProvenByHandAndNotByTheSuite).Assembly.GetReferencedAssemblies())
			{
				Assert.That(each.Name, Does.Not.StartWith("Cargas"),
					"Este proyecto referencia '" + each.Name + "'. Si el otro modulo se volvio "
					+ "alcanzable desde aca, se rompio 'ningun modulo referencia el ensamblado de "
					+ "otro' -- y ademas el cruce dejaria de necesitar la corrida a mano, asi que "
					+ "esta prueba y el §12 punto 12 hay que reescribirlos los dos.");

				referenced.Append(each.Name).Append(' ');
			}

			Assert.That(referenced.Length, Is.GreaterThan(0),
				"Y el barrido tiene que haber leido algo: cero referencias serian cero aciertos.");
		}
	}
}
