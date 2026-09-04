using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LOS SIETE CONJUNTOS CERRADOS DE ModelSpecification (§5 del brief, tabla §3.1 del plan) MAS
	// EnvelopeStanding, el primero de WarrantyEnvelope (§3.1 de su plan, T1), y lo unico que los
	// vuelve CERRADOS: exactamente los miembros que el canon nombra, cada uno una instancia
	// distinta, y un constructor privado para que nadie fabrique uno de mas.
	//
	// ENVELOPE STANDING TIENE TRES Y NO CUATRO, y el cuarto que falta es la decision D1 del plan
	// de su slice: "no se puede saber" NO es miembro -- es una de las tres variantes suspendidas
	// de EnvelopeAnswer (E47). Lo que si es miembro es NotPublished, exactamente como en
	// CoverageStanding, porque "nadie publico un sobre" el registro lo decide con certeza.
	//
	// Ni uno de mas -- un miembro que el canon no nombra es vocabulario inventado, y en este
	// modulo inventar vocabulario es inventar un hecho del fabricante (§7 del brief) -- ni dos
	// nombres apuntando al mismo objeto, porque con comparacion por referencia y sin igualdad
	// escrita (E23) eso los vuelve indistinguibles.
	//
	// La cuenta esta escrita a mano a proposito. Un test que derive el numero del propio tipo
	// no puede fallar: diria "declara los que declara". El numero viene del canon.
	//
	// CL-1 AGREGA Currency CON LOS DOS DE P-8, y es el primer conjunto cerrado de este modulo que
	// es TAMBIEN una base polimorfica, asi que entra en dos de los cuatro metodos y no en los otros
	// dos. Las razones, escritas donde el proximo lector las va a buscar:
	//
	//   ItDeclaresExactlyItsMembers        SI. Declara dos static readonly Currency, CRC y USD.
	//   ItIsInternalLikeTheRestOfTheDomain SI. Como todo el dominio.
	//
	//   ItCannotBeInstantiatedFromOutside  NO: su ctor no puede ser privado, porque sus dos
	//                                      variantes lo llaman. Lo que cierra el conjunto aca no es
	//                                      el ctor sino el resolutor -- Of y OfPublished son los
	//                                      unicos caminos desde un codigo, y jamas entregan una
	//                                      moneda nombrada que no sea CRC o USD.
	//   ItIsSealedBecauseNothingExtendsAClosedSet
	//                                      NO: E43 dice que una base polimorfica se deja ABIERTA, y
	//                                      la cabecera de este fixture ya lo reconoce. Currency
	//                                      tiene DOS variantes que se portan distinto -- una
	//                                      nombrada sabe su simbolo y sus decimales, una recibida y
	//                                      sin nombrar no los sabe y no se compara con ninguna --
	//                                      asi que ganan su tipo por comportamiento (E11, E39) y la
	//                                      base no puede sellarse. La ley del sellado se afirma
	//                                      sobre las HOJAS, mas abajo.
	[TestFixture]
	public sealed class EachClosedSetDeclaresExactlyTheMembersTheCanonNames
	{
		[TestCase(typeof(FluidKind), 6)]
		[TestCase(typeof(TyrePosition), 3)]
		[TestCase(typeof(FuelGradeStanding), 3)]
		[TestCase(typeof(PressureUnit), 2)]
		[TestCase(typeof(VolumeUnit), 2)]
		[TestCase(typeof(RevisionState), 3)]
		[TestCase(typeof(CoverageStanding), 5)]
		[TestCase(typeof(EnvelopeStanding), 3)]
		[TestCase(typeof(Currency), 2)]
		public void ItDeclaresExactlyItsMembers(Type conjunto, int cuantos)
		{
			List<object> miembros = MiembrosDe(conjunto);

			Assert.That(miembros.Count, Is.EqualTo(cuantos),
				$"{conjunto.Name} declara {miembros.Count} miembros y el canon nombra {cuantos}.");

			for (int i = 0; i < miembros.Count; i++)
			{
				for (int j = i + 1; j < miembros.Count; j++)
				{
					Assert.That(ReferenceEquals(miembros[i], miembros[j]), Is.False,
						$"Dos miembros de {conjunto.Name} son el mismo objeto. Con comparacion por "
						+ "referencia y sin igualdad escrita (E23), eso los vuelve indistinguibles.");
				}
			}
		}

		// El ctor privado es lo que hace CERRADO al conjunto: nadie puede fabricar un miembro
		// que el canon no nombra.
		[TestCase(typeof(FluidKind))]
		[TestCase(typeof(TyrePosition))]
		[TestCase(typeof(FuelGradeStanding))]
		[TestCase(typeof(PressureUnit))]
		[TestCase(typeof(VolumeUnit))]
		[TestCase(typeof(RevisionState))]
		[TestCase(typeof(CoverageStanding))]
		[TestCase(typeof(EnvelopeStanding))]
		public void ItCannotBeInstantiatedFromOutside(Type conjunto)
		{
			ConstructorInfo[] ctores = conjunto.GetConstructors(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(ctores, Is.Not.Empty,
				$"{conjunto.Name} no declara ningun constructor propio.");

			foreach (ConstructorInfo ctor in ctores)
			{
				Assert.That(ctor.IsPrivate, Is.True,
					$"{conjunto.Name} tiene un constructor no privado. Un conjunto cerrado cuyo ctor "
					+ "se alcanza deja de ser cerrado: aparecen miembros que el canon no nombra.");
			}
		}

		// Sellado (E43): estos siete son hojas, nadie los extiende. Lo que queda ABIERTO en
		// este slice son las bases polimorficas de las respuestas, que no son estas.
		[TestCase(typeof(FluidKind))]
		[TestCase(typeof(TyrePosition))]
		[TestCase(typeof(FuelGradeStanding))]
		[TestCase(typeof(PressureUnit))]
		[TestCase(typeof(VolumeUnit))]
		[TestCase(typeof(RevisionState))]
		[TestCase(typeof(CoverageStanding))]
		[TestCase(typeof(EnvelopeStanding))]
		public void ItIsSealedBecauseNothingExtendsAClosedSet(Type conjunto)
		{
			Assert.That(conjunto.IsSealed, Is.True,
				$"{conjunto.Name} no esta sellado. Un conjunto cerrado que se puede heredar admite "
				+ "un miembro que el canon no nombra por la puerta de atras.");
		}

		// Y son internal, como todo el dominio (E2, E42). El DSL alcanza internal igual que
		// public por reflexion, asi que ensanchar a public no compra nada y engana: sugiere que
		// alguien de afuera de la puerta los va a usar.
		[TestCase(typeof(FluidKind))]
		[TestCase(typeof(TyrePosition))]
		[TestCase(typeof(FuelGradeStanding))]
		[TestCase(typeof(PressureUnit))]
		[TestCase(typeof(VolumeUnit))]
		[TestCase(typeof(RevisionState))]
		[TestCase(typeof(CoverageStanding))]
		[TestCase(typeof(EnvelopeStanding))]
		[TestCase(typeof(Currency))]
		public void ItIsInternalLikeTheRestOfTheDomain(Type conjunto)
		{
			Assert.That(conjunto.IsPublic, Is.False,
				$"{conjunto.Name} es public. El dominio es internal de punta a punta.");
		}

		// LA LEY DEL SELLADO SOBRE LAS HOJAS, para el conjunto cerrado que es tambien una base
		// polimorfica. Los ocho de arriba son hojas y se afirman directo; Currency no puede serlo
		// (E43, y la cabecera dice por que), asi que lo que se afirma es que sus VARIANTES lo son y
		// que son exactamente dos: la nombrada y la recibida-y-sin-nombrar.
		//
		// Una tercera variante que apareciera seria una tercera razon para no poder decir una
		// moneda, y este modulo tiene una sola. Eso se ve aca antes que en produccion.
		[Test]
		public void TheCurrencysVariantsAreSealedLeavesAndThereAreExactlyTwo()
		{
			List<Type> variantes = typeof(Currency).Assembly
				.GetTypes()
				.Where(tipo => tipo != typeof(Currency) && typeof(Currency).IsAssignableFrom(tipo))
				.OrderBy(tipo => tipo.Name, StringComparer.Ordinal)
				.ToList();

			Assert.That(variantes.Select(tipo => tipo.Name),
				Is.EqualTo(new[] { "ACurrencyThisModuleDoesNotName", "ANamedCurrency" }),
				"Las variantes de Currency son dos y se portan distinto. Una tercera es una tercera "
				+ "razon para no poder decir una moneda, y no hay ninguna mas.");

			Assert.That(typeof(Currency).IsAbstract, Is.True,
				"Currency es la base y no se instancia: una moneda es una de sus dos variantes.");

			foreach (Type variante in variantes)
			{
				Assert.That(variante.IsSealed, Is.True,
					$"{variante.Name} no esta sellada. Es una hoja y nadie la extiende (E43).");
				Assert.That(variante.IsPublic, Is.False,
					$"{variante.Name} es public. El dominio es internal de punta a punta.");
			}
		}

		private static List<object> MiembrosDe(Type conjunto)
		{
			return conjunto
				.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(campo => campo.IsInitOnly && campo.FieldType == conjunto)
				.Select(campo => campo.GetValue(null))
				.ToList();
		}
	}
}
