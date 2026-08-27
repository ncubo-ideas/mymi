using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LOS SIETE CONJUNTOS CERRADOS DE ModelSpecification (§5 del brief, tabla §3.1 del plan),
	// y lo unico que los vuelve CERRADOS: exactamente los miembros que el canon nombra, cada
	// uno una instancia distinta, y un constructor privado para que nadie fabrique un octavo.
	//
	// Ni uno de mas -- un miembro que el canon no nombra es vocabulario inventado, y en este
	// modulo inventar vocabulario es inventar un hecho del fabricante (§7 del brief) -- ni dos
	// nombres apuntando al mismo objeto, porque con comparacion por referencia y sin igualdad
	// escrita (E23) eso los vuelve indistinguibles.
	//
	// La cuenta esta escrita a mano a proposito. Un test que derive el numero del propio tipo
	// no puede fallar: diria "declara los que declara". El numero viene del canon.
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
		public void ItIsInternalLikeTheRestOfTheDomain(Type conjunto)
		{
			Assert.That(conjunto.IsPublic, Is.False,
				$"{conjunto.Name} es public. El dominio es internal de punta a punta.");
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
