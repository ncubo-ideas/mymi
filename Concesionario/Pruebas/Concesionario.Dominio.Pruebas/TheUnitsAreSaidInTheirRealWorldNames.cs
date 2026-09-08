using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LOS TRES CONJUNTOS DE UNIDADES SON EL UNICO SITIO DEL MODULO donde el nombre que el
	// dominio proyecta NO es el nombre del miembro: el miembro se llama UnitA y la frase dice
	// km, L o bar. En todos los demas -- FluidKind, TyrePosition, FuelGradeStanding -- los dos
	// coinciden, y eso lo defiende TheProjectedNameIsTheMembersOwnName, de la que estos tres
	// quedaron excluidos a proposito.
	//
	// POR QUE SE SEPARAN. DEALERSHIP-DOMAIN §6 dice de DistanceUnit que sus dos miembros son
	// "deliberately unnamed: their real-world identity is a display concern", y OWNER-APP-DOMAIN
	// §2 pide que la cadena que se muestra se elija en el borde y que el mapeo sea EXHAUSTIVO y
	// probado como tal. Esta prueba es ese mapeo probado: los seis nombres, uno por uno.
	//
	// DOS VOCABULARIOS, Y NINGUNO PISA AL OTRO. El wire sigue llevando el NOMBRE DEL MIEMBRO
	// (S2, C12, C25): el journal guarda 'UnitA' y la coercion del call site del script se hace
	// contra el enum, no contra la etiqueta. Si alguien "arreglara" el wire para que lleve km,
	// todo journal ya escrito dejaria de rehidratar. Por eso la segunda mitad de esta prueba
	// vuelve a atar el enum de frontera al nombre del miembro, que es la mitad que el cambio de
	// etiqueta podria haber aflojado sin que nada se pusiera rojo.
	//
	// Y LO QUE NO CAMBIA ES LO QUE IMPORTA: nombrarlas no las hace convertibles. La ausencia de
	// Convert la siguen defendiendo TheDistanceUnitIsNeverConverted y
	// ThePressureAndVolumeUnitsAreNeverConverted con sus listas blancas; aca solo se fija COMO
	// SE DICEN. Una etiqueta no es un factor.
	[TestFixture]
	public sealed class TheUnitsAreSaidInTheirRealWorldNames
	{
		// El mapeo entero, clavado. Un miembro que cambie de nombre sin que alguien lo decida
		// pone rojo esto, que es el punto: la persona lee esta palabra en su pantalla.
		[TestCase(typeof(DistanceUnit), "UnitA", "km")]
		[TestCase(typeof(DistanceUnit), "UnitB", "mi")]
		[TestCase(typeof(VolumeUnit), "UnitA", "L")]
		[TestCase(typeof(VolumeUnit), "UnitB", "gal")]
		[TestCase(typeof(PressureUnit), "UnitA", "bar")]
		[TestCase(typeof(PressureUnit), "UnitB", "psi")]
		public void EachMemberIsSaidWithItsRealWorldName(
			Type conjunto, string miembro, string comoSeDice)
		{
			Assert.That(NombreProyectadoDe(conjunto, miembro), Is.EqualTo(comoSeDice));
		}

		// EL MAPEO ES EXHAUSTIVO. No alcanza con probar los seis que uno se acordo de escribir
		// arriba: el dia que entre una tercera unidad a cualquiera de los tres conjuntos, nadie
		// le pone nombre y la pantalla muestra lo que quede. Esto se pone rojo ese dia.
		[TestCase(typeof(DistanceUnit))]
		[TestCase(typeof(VolumeUnit))]
		[TestCase(typeof(PressureUnit))]
		public void EveryMemberOfTheSetHasARealWorldName(Type conjunto)
		{
			List<string> miembros = MiembrosDe(conjunto).Select(campo => campo.Name).ToList();

			Assert.That(miembros, Is.EqualTo(new[] { "UnitA", "UnitB" }),
				$"{conjunto.Name} cambio de miembros y los casos de arriba ya no los cubren "
				+ "todos. Un miembro sin nombre del mundo real sale a la pantalla como se llame "
				+ "en el codigo, y la persona lee eso.");
		}

		// Y NINGUNO SE DICE COMO SE LLAMA. Sin esto, dejar un miembro en 'UnitA' por olvido
		// pasaria como si fuera una decision.
		[TestCase(typeof(DistanceUnit))]
		[TestCase(typeof(VolumeUnit))]
		[TestCase(typeof(PressureUnit))]
		public void NoMemberIsSaidByItsCodeName(Type conjunto)
		{
			foreach (FieldInfo campo in MiembrosDe(conjunto))
				Assert.That(NombreProyectadoDe(conjunto, campo.Name), Is.Not.EqualTo(campo.Name),
					$"{conjunto.Name}.{campo.Name} se dice con su propio nombre de codigo. "
					+ "'UnitA' es el vocabulario del wire, no una unidad que alguien pueda leer "
					+ "en un manometro o en un odometro.");
		}

		// LOS TRES CONJUNTOS SE DICEN DISTINTO ENTRE SI. Una capacidad y una presion comparten
		// el nombre de miembro -- las dos declaran UnitA -- y esa colision es la que hizo que
		// apuntar un resolutor al conjunto equivocado fuera indetectable (C52). Ahora las frases
		// SI los distinguen, y esta prueba es la que dice que siguen distinguiendolos.
		[Test]
		public void TheThreeSetsDoNotShareASaidName()
		{
			List<string> todos = new[] { typeof(DistanceUnit), typeof(VolumeUnit), typeof(PressureUnit) }
				.SelectMany(conjunto => MiembrosDe(conjunto)
					.Select(campo => NombreProyectadoDe(conjunto, campo.Name)))
				.ToList();

			Assert.That(todos.Distinct().Count(), Is.EqualTo(todos.Count),
				"Dos unidades de dimensiones distintas se dicen igual: " + string.Join(", ", todos)
				+ ". Entonces una frase con las dos adentro no se puede leer, y una prueba que "
				+ "cruce los dos conjuntos queda verde por casualidad.");
		}

		// LA MITAD QUE EL CAMBIO DE ETIQUETA PODRIA HABER AFLOJADO. El enum de frontera y el
		// miembro del conjunto se siguen llamando IGUAL: es el nombre del miembro lo que viaja
		// (S2), asi que despegarlos rompe la coercion en runtime y deja entradas permanentes en
		// el journal de una persona (S5). La etiqueta nueva no toca esto y hay que probar que
		// no lo toco.
		[TestCase(typeof(DistanceUnitCode), typeof(DistanceUnit))]
		[TestCase(typeof(VolumeUnitCode), typeof(VolumeUnit))]
		[TestCase(typeof(PressureUnitCode), typeof(PressureUnit))]
		public void TheWireStillTravelsTheMemberNameAndNotTheSaidName(Type codigos, Type conjunto)
		{
			List<string> nombresDelEnum = Enum.GetNames(codigos)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			List<string> nombresDelConjunto = MiembrosDe(conjunto)
				.Select(campo => campo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(nombresDelConjunto, Is.EqualTo(nombresDelEnum),
				$"{codigos.Name} y {conjunto.Name} dejaron de nombrar lo mismo. El wire lleva el "
				+ "NOMBRE DEL MIEMBRO, no la etiqueta que se muestra: si alguien movio el enum "
				+ "hacia km, todo journal ya escrito deja de rehidratar.");
		}

		// Y EL RESOLUTOR SIGUE ENTRANDO POR EL CODIGO, no por la etiqueta. Un OfCode que
		// aceptara 'km' seria la etiqueta convertida en clave, y la clave es del wire.
		[Test]
		public void TheResolverStillEntersByTheCodeAndNotByTheSaidName()
		{
			Assert.That(DistanceUnit.OfCode(DistanceUnitCode.UnitA), Is.SameAs(DistanceUnit.UnitA));
			Assert.That(VolumeUnit.OfCode(VolumeUnitCode.UnitB), Is.SameAs(VolumeUnit.UnitB));
			Assert.That(PressureUnit.OfCode(PressureUnitCode.UnitA), Is.SameAs(PressureUnit.UnitA));
		}

		private static string NombreProyectadoDe(Type conjunto, string miembro)
		{
			FieldInfo campo = conjunto.GetField(miembro,
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(campo, Is.Not.Null, $"{conjunto.Name} no declara {miembro}.");

			PropertyInfo nombre = conjunto.GetProperty("Name",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(nombre, Is.Not.Null,
				$"{conjunto.Name} no proyecta Name. Es lo unico que cruza hacia afuera.");

			return (string)nombre.GetValue(campo.GetValue(null));
		}

		private static List<FieldInfo> MiembrosDe(Type conjunto)
		{
			return conjunto
				.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(campo => campo.IsInitOnly && campo.FieldType == conjunto)
				.ToList();
		}
	}
}
