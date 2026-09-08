using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L8 del brief, UnitsAreNeverConverted: "una presion y una capacidad se guardan con su
	// unidad y NUNCA se normalizan. Dos valores en unidades distintas no son comparables, y la
	// respuesta a una comparacion cruzada es 'no se puede evaluar' -- nunca un numero
	// convertido."
	//
	// Es la misma disciplina que OINV-21 / L7 de OS2 impuso para la distancia, con unidades
	// nuevas, asi que esta prueba es la hermana de TheDistanceUnitIsNeverConverted y se lee
	// junto con ella. Lo que hay que proteger no es una cuenta: es una AUSENCIA. La tentacion
	// es obvia y suena a servicio -- "ya que tenemos las dos unidades, un Convert de una
	// linea". El dia que exista, L8 deja de significar nada, porque ya no habria discrepancia
	// que suspender: siempre se podria convertir.
	//
	// LA PRESION ES LA PEOR DE LAS TRES. Una distancia mal convertida da una fecha de servicio
	// equivocada; una presion mal convertida es alguien inflando una llanta al numero de la
	// otra unidad, en una bomba de gasolinera, con el manometro en la mano. Por eso esta
	// prueba existe aunque §11 del brief solo pida el test 9.
	//
	// Una ausencia no se puede afirmar con una llamada, asi que se afirma con una LISTA
	// BLANCA: cada tipo expone EXACTAMENTE estos miembros y ninguno mas. Cualquier agregado --
	// Convert, Factor, ToUnitA, EnUnidadesDe, un operador -- pone rojo esto y obliga a leer L8
	// antes de seguir.
	[TestFixture]
	public sealed class ThePressureAndVolumeUnitsAreNeverConverted
	{
		// La lista blanca. Los dos miembros del canon, la proyeccion del nombre y el resolutor
		// de frontera. Nada mas.
		private static readonly string[] LoQueUnaUnidadPuedeExponer =
		{
			"UnitA",
			"UnitB",
			"Name",
			"OfCode",
		};

		[TestCase(typeof(PressureUnit))]
		[TestCase(typeof(VolumeUnit))]
		public void ItExposesNothingOutsideTheAllowlist(Type unidad)
		{
			List<string> expuestos = unidad
				.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Select(miembro => miembro.Name)
				.Distinct()
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			List<string> intrusos = expuestos
				.Where(nombre => !LoQueUnaUnidadPuedeExponer.Contains(nombre))
				.ToList();

			Assert.That(intrusos, Is.Empty,
				$"{unidad.Name} gano miembros: " + string.Join(", ", intrusos) + ". "
				+ "Ley L8: dos unidades que NUNCA se inter-convierten es el punto entero. Un "
				+ "Convert, un factor, o cualquier aritmetica que mezcle unidades es un DEFECTO, no "
				+ "una comodidad -- con conversion no hay discrepancia que suspender y la ley se "
				+ "queda sin significado.");
		}

		[TestCase(typeof(PressureUnit))]
		[TestCase(typeof(VolumeUnit))]
		public void TheTwoUnitsAreDistinctInstancesAndCompareByIdentity(Type unidad)
		{
			object unitA = unidad.GetField("UnitA",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
			object unitB = unidad.GetField("UnitB",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);

			Assert.That(ReferenceEquals(unitA, unitB), Is.False,
				$"UnitA y UnitB de {unidad.Name} son el mismo objeto: entonces no hay discrepancia "
				+ "de unidad posible y L8 nunca se dispararia.");
		}

		// La comparacion que el dominio usa es la del conjunto cerrado: REFERENCIA. Comparar
		// Name seria igualdad de valor disfrazada de string (E23), y es exactamente el reflejo
		// que termina en "si son distintas, convierto".
		[Test]
		public void TheUnitsAreComparedByReferenceAndNotByName()
		{
			Assert.That(PressureUnit.OfCode(PressureUnitCode.UnitA) == PressureUnit.UnitA, Is.True);
			Assert.That(PressureUnit.OfCode(PressureUnitCode.UnitB) == PressureUnit.UnitA, Is.False);
			Assert.That(VolumeUnit.OfCode(VolumeUnitCode.UnitA) == VolumeUnit.UnitA, Is.True);
			Assert.That(VolumeUnit.OfCode(VolumeUnitCode.UnitB) == VolumeUnit.UnitA, Is.False);
		}

		// Una presion y una capacidad no comparten unidad ni por casualidad: una unidad de
		// presion y una de volumen NO son intercambiables, asi que los dos conjuntos son
		// disjuntos y ninguna UnitA es la otra UnitA. Si se cruzaran, el compilador dejaria
		// pasar una capacidad publicada en unidades de presion.
		[Test]
		public void APressureUnitIsNotAVolumeUnit()
		{
			Assert.That(typeof(PressureUnit), Is.Not.EqualTo(typeof(VolumeUnit)));
			Assert.That(ReferenceEquals(PressureUnit.UnitA, VolumeUnit.UnitA), Is.False,
				"Los dos conjuntos son disjuntos: una unidad de presion y una de volumen no se "
				+ "pueden confundir, y menos convertir.");
		}

		// Ni el tipo ni el enum de frontera declaran un valor numerico por unidad. Un factor
		// escondido en el enum (UnitA = 1, UnitB = 145) seria la conversion entrando por la
		// puerta de atras: el numero esta ahi y alguien lo va a multiplicar.
		[TestCase(typeof(PressureUnit), typeof(PressureUnitCode))]
		[TestCase(typeof(VolumeUnit), typeof(VolumeUnitCode))]
		public void NeitherTheUnitNorItsCodeCarriesANumberThatInvitesMultiplying(
			Type unidad, Type codigos)
		{
			IEnumerable<FieldInfo> camposDeDatos = unidad
				.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(campo => campo.FieldType != unidad);

			foreach (FieldInfo campo in camposDeDatos)
			{
				Assert.That(EsNumerico(campo.FieldType), Is.False,
					$"{unidad.Name} lleva el campo numerico '{campo.Name}' "
					+ $"({campo.FieldType.Name}). Un numero por unidad es un factor de conversion "
					+ "aunque no se llame asi.");
			}

			// El enum de frontera se queda con sus valores implicitos: el journal guarda el
			// NOMBRE del miembro, asi que el numero no significa nada y no debe significarlo.
			int[] valores = Enum.GetValues(codigos).Cast<int>().OrderBy(v => v).ToArray();
			Assert.That(valores, Is.EqualTo(new[] { 0, 1 }),
				$"{codigos.Name} dejo de usar los valores implicitos. Numerarlo a mano sugiere que "
				+ "el numero significa algo -- y lo unico que viaja es el nombre.");
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
