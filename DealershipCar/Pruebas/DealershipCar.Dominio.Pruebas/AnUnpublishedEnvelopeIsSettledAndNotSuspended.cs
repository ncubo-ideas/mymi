using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// DECISION D1 DEL PLAN (§4.3), y esta prueba es lo que la sostiene despues de tomarla.
	//
	// EL BRIEF (§4) LISTA CUATRO AUSENCIAS y una de ellas se retira:
	// AnEnvelopeSuspendedByAnUnpublishedEnvelope no existe. "Nadie publico un sobre" NO es algo
	// que el registro no pueda decidir: es algo que decide con certeza, y por lo tanto es una
	// respuesta DECIDIDA con el miembro NotPublished, no una suspension.
	//
	// EL PRECEDENTE DEL MODULO ES INEQUIVOCO, y se re-derivo contra el codigo antes de escribir
	// un tipo (C69, y el plan §7 lo pide explicitamente):
	//
	//   CoverageStanding declara NotPublished como MIEMBRO de sus cinco -- "No nos lo dijeron"
	//   -- y su /// dice que la diferencia con NotCovered "es el punto entero del conjunto";
	//
	//   CoverageAnswer tiene EXACTAMENTE UNA variante suspendida,
	//   ACoverageSuspendedByAUnitMismatch, y ninguna por falta de publicacion;
	//
	//   WarrantyCoverage.TheStandingFor devuelve un ASettledCoverageAnswer con
	//   CoverageStanding.NotPublished cuando el vocabulario del fabricante no nombra el sistema.
	//   O sea: lo no publicado sale por la puerta de las respuestas decididas, no por la de las
	//   suspendidas.
	//
	// LA CONSECUENCIA QUE SE LEE RARA UNA VEZ Y ES CORRECTA: CanBeEvaluated contesta TRUE para un
	// vehiculo sin ningun sobre. Se pudo evaluar; lo que se evaluo es que no hay nada publicado.
	//
	// LA SEGUNDA MITAD DE LA PRUEBA ES LA QUE DE VERDAD DEFIENDE D1: que el cuarto tipo NO EXISTE.
	// Una ausencia no se puede afirmar con una llamada, asi que se afirma por reflexion -- el
	// mismo idioma que AnInternallySelectedSetIsNotReachedFromTheBoundary usa, y por la misma
	// razon: el brief es canon para los nombres, asi que alguien va a volver a leerlo y a agregar
	// el tipo sin pasar por el plan.
	[TestFixture]
	public sealed class AnUnpublishedEnvelopeIsSettledAndNotSuspended
	{
		private const string TheWithdrawnType = "AnEnvelopeSuspendedByAnUnpublishedEnvelope";

		[Test]
		public void NotPublishedIsAMemberOfTheStandingAndNotAReasonForSuspending()
		{
			EnvelopeAnswer answer = new ASettledEnvelopeAnswer(
				EnvelopeStanding.NotPublished, string.Empty);

			Assert.That(answer.CanBeEvaluated(), Is.True,
				"Se pudo evaluar: lo que se evaluo es que la agencia no publico ningun sobre. "
				+ "El registro lo sabe con certeza, y por eso no es una suspension.");
			Assert.That(answer.Standing(), Is.SameAs(EnvelopeStanding.NotPublished));
		}

		// Y no esta suspendida por NINGUNO de los tres motivos. Sin esto, un tipo que contestara
		// que si a la vez que decide pasaria: seria decidida y suspendida al mismo tiempo.
		[Test]
		public void ASettledUnpublishedAnswerIsSuspendedByNothing()
		{
			EnvelopeAnswer answer = new ASettledEnvelopeAnswer(
				EnvelopeStanding.NotPublished, string.Empty);

			Assert.That(answer.SuspendedByAnAbsentDelivery(), Is.False);
			Assert.That(answer.SuspendedByAnAbsentReading(), Is.False);
			Assert.That(answer.SuspendedByAUnitMismatch(), Is.False);
		}

		// La frase lo dice CON PALABRAS, no devolviendo un vacio que la vista renderiza en blanco.
		[Test]
		public void TheUnpublishedAnswerNamesItselfInWords()
		{
			string said = new ASettledEnvelopeAnswer(EnvelopeStanding.NotPublished, string.Empty)
				.Describe();

			Assert.That(said, Does.Contain("Sin publicar"));
			Assert.That(said, Does.Contain("sobre de garantía"),
				"La frase nombra de que habla. Si no lo nombrara, la misma serviria para "
				+ "cualquier pregunta y nadie sabria de cual es la respuesta.");
		}

		// ===== LA MITAD QUE DEFIENDE LA DECISION ===========================================

		[Test]
		public void TheFourthAbsenceTypeOfTheBriefDoesNotExist()
		{
			IEnumerable<string> tipos = typeof(EnvelopeAnswer).Assembly.GetTypes()
				.Select(tipo => tipo.Name);

			Assert.That(tipos, Does.Not.Contain(TheWithdrawnType),
				"D1: aparecio " + TheWithdrawnType + ". El brief §4 lo lista y el plan §4.3 lo "
				+ "retira, porque 'nadie publico un sobre' es algo que el registro decide con "
				+ "certeza -- y lo decidido va como miembro de EnvelopeStanding, exactamente como "
				+ "CoverageStanding.NotPublished. Una suspension ahi diria que no se pudo saber "
				+ "algo que si se supo.");
		}

		// Y las suspensiones son TRES, contadas. Sin este numero, retirar la cuarta y agregar una
		// quinta por otro motivo se veria igual: la lista es corta en los dos casos (C66).
		[Test]
		public void ThereAreExactlyThreeSuspensionsAndTheyAreTheThreeOfThePlan()
		{
			List<string> suspensiones = LasVariantesDeEnvelopeAnswer()
				.Where(tipo => tipo != typeof(ASettledEnvelopeAnswer))
				.Select(tipo => tipo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(suspensiones, Is.EqualTo(new[]
			{
				"AnEnvelopeSuspendedByAUnitMismatch",
				"AnEnvelopeSuspendedByAnAbsentDelivery",
				"AnEnvelopeSuspendedByAnAbsentReading",
			}));
		}

		// El precedente que sostiene D1 sigue siendo el que se leyo. Si CoverageAnswer creciera
		// una suspension por falta de publicacion, el argumento de D1 se cayo y hay que rehacerlo:
		// esto es lo que lo pone rojo el dia que pase, en vez de dejarlo escrito en un plan.
		[Test]
		public void TheCoveragePrecedentThisDecisionRestsOnStillHolds()
		{
			List<string> variantes = typeof(CoverageAnswer).Assembly.GetTypes()
				.Where(tipo => tipo.IsSubclassOf(typeof(CoverageAnswer)) && !tipo.IsAbstract)
				.Select(tipo => tipo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(variantes, Is.EqualTo(new[]
			{
				"ACoverageSuspendedByAUnitMismatch",
				"ASettledCoverageAnswer",
			}),
				"D1 se tomo leyendo que la cobertura tiene UNA sola suspension y lleva lo no "
				+ "publicado como miembro del conjunto. Si eso cambio, D1 hay que volver a "
				+ "decidirlo, no heredarlo.");

			Assert.That(CoverageStanding.NotPublished.Name, Is.EqualTo("NotPublished"));
		}

		private static List<Type> LasVariantesDeEnvelopeAnswer()
		{
			return typeof(EnvelopeAnswer).Assembly.GetTypes()
				.Where(tipo => tipo.IsSubclassOf(typeof(EnvelopeAnswer)) && !tipo.IsAbstract)
				.ToList();
		}
	}
}
