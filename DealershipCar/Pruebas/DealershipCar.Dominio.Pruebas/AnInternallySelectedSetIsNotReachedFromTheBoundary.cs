using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LOS DOS CONJUNTOS CERRADOS DE ModelSpecification QUE **NO** SE ELIGEN DESDE EL WIRE
	// (tabla §3.1 del plan): RevisionState y CoverageStanding. Y desde T1 de WarrantyEnvelope,
	// EnvelopeStanding, que es interno POR LA MISMA RAZON QUE CoverageStanding: es una RESPUESTA
	// -- donde esta parado el vehiculo contra el sobre que la agencia publico -- y nunca una
	// entrada. Un codigo de frontera que lo nombrara dejaria que alguien de afuera DIJERA que un
	// carro esta todavia en garantia, que es lo que la ley L11 de ese slice prohibe: este modulo
	// dice donde esta parado el vehiculo, no adjudica.
	//
	// La tabla los marca "internally selected", y no tener enum de frontera ni OfCode es LA
	// DECISION, no un olvido. Una ausencia no se puede afirmar con una llamada, asi que se
	// afirma por reflexion -- exactamente como DueStateIsNotReachedFromTheBoundary lo hace en
	// OS2, y por la misma razon: el dia que alguien le agregue un OfCode habra abierto una
	// puerta que el slice no quiso, y nadie lo va a notar leyendo el diff.
	//
	// POR QUE CADA UNO ES INTERNO, y son razones distintas:
	//
	//   RevisionState lo mueven las transiciones de la maquina (§10 del brief). Nadie de
	//   afuera dice "poneme esta revision en Sealed": dice "sella esta revision", y el estado
	//   es la consecuencia. Un OfCode dejaria entrar el estado como dato.
	//
	//   CoverageStanding es una RESPUESTA, nunca una entrada (ley L9). Un codigo de frontera
	//   que lo nombre significaria que alguien de afuera puede DECIR que algo esta cubierto,
	//   que es justo lo que L3 prohibe: este modulo dice el alcance publicado, no adjudica.
	//
	//   CostClassification -- de CL-3 -- es el caso MAS interno de los cuatro, y por una razon que los
	//   otros tres no tienen: llega POR CUAL COMANDO SE LLAMO. No hay ningun parametro de
	//   clasificacion en ningun sitio (§7.2 del plan), asi que no existe la pregunta "que codigo de
	//   frontera resuelve esto": nunca entra, solo sale como columna. Un XxxCode aca no seria una
	//   puerta que el slice no quiso -- seria una puerta que no lleva a ningun lado, y ademas un
	//   cuarto enum en la namespace unica que C52 midio, donde el nombre del miembro es lo unico que
	//   cruza. El argumento entero esta en DECISIONS.md; la ley de la proyeccion se afirma en
	//   TheProjectedNameIsTheMembersOwnName y la membresia en
	//   EachClosedSetDeclaresExactlyTheMembersTheCanonNames.
	[TestFixture]
	public sealed class AnInternallySelectedSetIsNotReachedFromTheBoundary
	{
		[TestCase(typeof(RevisionState), "RevisionStateCode")]
		[TestCase(typeof(CoverageStanding), "CoverageStandingCode")]
		[TestCase(typeof(EnvelopeStanding), "EnvelopeStandingCode")]
		[TestCase(typeof(CostClassification), "CostClassificationCode")]
		public void ItHasNoResolverAndNoBoundaryEnum(Type conjunto, string enumQueNoDebeExistir)
		{
			Assert.That(conjunto.GetMethod("OfCode",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				Is.Null,
				$"{conjunto.Name} gano un OfCode. Segun la tabla §3.1 se elige adentro y nunca "
				+ "cruza el wire hacia adentro; un resolutor le abre una puerta que el slice no "
				+ "quiso.");

			IEnumerable<string> enumsDeFrontera = conjunto.Assembly.GetTypes()
				.Where(tipo => tipo.IsEnum)
				.Select(tipo => tipo.Name);

			Assert.That(enumsDeFrontera, Does.Not.Contain(enumQueNoDebeExistir),
				$"Aparecio un {enumQueNoDebeExistir}. {conjunto.Name} sale solo como proyeccion "
				+ "escalar de su nombre (E24), nunca como objeto ni como dato de entrada.");
		}

		[Test]
		public void TheRevisionStatesAreTheThreeOfTheMachine()
		{
			Assert.That(RevisionState.Draft.Name, Is.EqualTo("Draft"));
			Assert.That(RevisionState.Sealed.Name, Is.EqualTo("Sealed"));
			Assert.That(RevisionState.Superseded.Name, Is.EqualTo("Superseded"));
		}

		// Las cinco respuestas de la ley L9, que existe justamente para que no sean dos. Un
		// booleano tiene dos y "esta cubierto" tiene cinco respuestas honestas: colapsarlas es
		// como una app termina diciendole a alguien que esta cubierto cuando la respuesta
		// verdadera era "hay una exclusion que deberia leer" o "no nos lo dijeron".
		[Test]
		public void CoverageHasTheFiveAnswersOfTheCanonAndNotTwo()
		{
			Assert.That(CoverageStanding.Covered.Name, Is.EqualTo("Covered"));
			Assert.That(CoverageStanding.CoveredWithExclusion.Name,
				Is.EqualTo("CoveredWithExclusion"));
			Assert.That(CoverageStanding.NotCovered.Name, Is.EqualTo("NotCovered"));
			Assert.That(CoverageStanding.OutsideEnvelope.Name, Is.EqualTo("OutsideEnvelope"));
			Assert.That(CoverageStanding.NotPublished.Name, Is.EqualTo("NotPublished"));
		}

		// La diferencia entre NotCovered y NotPublished es el punto entero de L9, asi que no
		// pueden ser el mismo objeto: "no nos lo dijeron" y "no lo cubre" son dos respuestas.
		[Test]
		public void NotPublishedAndNotCoveredAreNotTheSameAnswer()
		{
			Assert.That(
				ReferenceEquals(CoverageStanding.NotPublished, CoverageStanding.NotCovered),
				Is.False,
				"Ley L9: 'no nos lo dijeron' no es 'no esta cubierto'. Si fueran el mismo miembro, "
				+ "la app contestaria que algo no esta cubierto cuando lo unico cierto es que "
				+ "nadie publico nada al respecto.");
		}
	}
}
