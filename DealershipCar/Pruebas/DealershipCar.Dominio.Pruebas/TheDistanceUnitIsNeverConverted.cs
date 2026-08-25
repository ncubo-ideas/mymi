using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// OINV-21 / LEY L7: "Los limites de distancia se evaluan SOLO contra lecturas en la
	// MISMA DistanceUnit del plan. Una discrepancia de unidad suspende la evaluacion de
	// distancia para esa operacion; NUNCA convierte."
	//
	// Lo que hay que proteger no es una cuenta: es una AUSENCIA. El defecto que este slice
	// puede tener no es un factor mal calculado, es un factor que existe. La tentacion es
	// obvia y suena a servicio: "ya que tenemos las dos unidades, un Convert de una linea".
	// El dia que exista, la ley L7 deja de tener sentido -- porque ya nunca habria
	// discrepancia que suspender: siempre se podria convertir.
	//
	// Una ausencia no se puede afirmar con una llamada, asi que se afirma con una LISTA
	// BLANCA: DistanceUnit expone EXACTAMENTE estos miembros y ninguno mas. Cualquier
	// agregado -- Convert, Factor, ToUnitA, EnUnidadesDe, un operador -- pone rojo esto y
	// obliga a leer OINV-21 antes de seguir.
	//
	// La segunda mitad: dos unidades se comparan por IDENTIDAD, no por nombre. Comparar
	// Name seria igualdad de valor disfrazada de string (E23), y es exactamente el reflejo
	// que termina en "si son distintas, convierto".
	[TestFixture]
	public sealed class TheDistanceUnitIsNeverConverted
	{
		// La lista blanca. Los dos miembros del canon, la proyeccion del nombre y el
		// resolutor de frontera. Nada mas.
		private static readonly string[] LoQueDistanceUnitPuedeExponer =
		{
			"UnitA",
			"UnitB",
			"Name",
			"OfCode",
		};

		[Test]
		public void DistanceUnitExposesNothingOutsideTheAllowlist()
		{
			List<string> expuestos = typeof(DistanceUnit)
				.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Select(miembro => miembro.Name)
				.Distinct()
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			List<string> intrusos = expuestos
				.Where(nombre => !LoQueDistanceUnitPuedeExponer.Contains(nombre))
				.ToList();

			Assert.That(intrusos, Is.Empty,
				"DistanceUnit gano miembros: " + string.Join(", ", intrusos) + ". "
				+ "OINV-21 / L7: dos unidades que NUNCA se inter-convierten es el punto entero. "
				+ "Un Convert, un factor, o cualquier aritmetica que mezcle unidades es un "
				+ "DEFECTO, no una comodidad -- con conversion no hay discrepancia que suspender "
				+ "y la ley L7 se queda sin significado.");
		}

		[Test]
		public void TheTwoUnitsAreDistinctInstancesAndCompareByIdentity()
		{
			Assert.That(ReferenceEquals(DistanceUnit.UnitA, DistanceUnit.UnitB), Is.False,
				"UnitA y UnitB son el mismo objeto: entonces no hay discrepancia de unidad posible "
				+ "y L7 nunca se dispararia.");

			// La comparacion que el dominio usa es esta, la del conjunto cerrado: referencia.
			Assert.That(DistanceUnit.OfCode(DistanceUnitCode.UnitA) == DistanceUnit.UnitA, Is.True);
			Assert.That(DistanceUnit.OfCode(DistanceUnitCode.UnitB) == DistanceUnit.UnitA, Is.False);
		}

		// Ni el tipo ni el enum de frontera declaran un valor numerico por unidad. Un factor
		// escondido en el enum (UnitA = 1, UnitB = 1609) seria la conversion entrando por la
		// puerta de atras: el numero esta ahi y alguien lo va a multiplicar.
		[Test]
		public void NeitherTheUnitNorItsCodeCarriesANumberThatInvitesMultiplying()
		{
			IEnumerable<FieldInfo> camposDeDatos = typeof(DistanceUnit)
				.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(campo => campo.FieldType != typeof(DistanceUnit));

			foreach (FieldInfo campo in camposDeDatos)
			{
				Assert.That(EsNumerico(campo.FieldType), Is.False,
					$"DistanceUnit lleva el campo numerico '{campo.Name}' ({campo.FieldType.Name}). "
					+ "Un numero por unidad es un factor de conversion aunque no se llame asi.");
			}

			// El enum de frontera se queda con sus valores implicitos: el journal guarda el
			// NOMBRE del miembro (S2), asi que el numero no significa nada y no debe significarlo.
			int[] valores = Enum.GetValues<DistanceUnitCode>().Cast<int>().OrderBy(v => v).ToArray();
			Assert.That(valores, Is.EqualTo(new[] { 0, 1 }),
				"DistanceUnitCode dejo de usar los valores implicitos. Numerarlo a mano sugiere "
				+ "que el numero significa algo -- y lo unico que viaja es el nombre.");
		}

		private static bool EsExpuesto(MemberInfo miembro)
		{
			if (miembro is FieldInfo campo) return campo.IsPublic || campo.IsAssembly;
			if (miembro is PropertyInfo propiedad)
			{
				MethodInfo lector = propiedad.GetGetMethod(true);
				return lector != null && (lector.IsPublic || lector.IsAssembly);
			}
			if (miembro is MethodInfo metodo)
				return !metodo.IsSpecialName && (metodo.IsPublic || metodo.IsAssembly);
			if (miembro is ConstructorInfo ctor) return ctor.IsPublic || ctor.IsAssembly;
			return false;
		}

		private static bool EsNumerico(Type tipo)
		{
			return tipo == typeof(int) || tipo == typeof(long) || tipo == typeof(short)
				|| tipo == typeof(byte) || tipo == typeof(decimal) || tipo == typeof(double)
				|| tipo == typeof(float);
		}
	}
}
