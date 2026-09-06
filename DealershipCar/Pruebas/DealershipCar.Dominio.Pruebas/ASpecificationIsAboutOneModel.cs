using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA IDENTIDAD DEL AGREGADO DE ESTE SLICE (§2 y canon §4 del brief).
	//
	// Todo lo demas en los dos documentos de dominio se identifica por Vin. Esto NO: se
	// identifica por Model, y muchos vehiculos comparten un modelo. Esa es la decision que
	// §2 del brief toma antes de escribir el primer tipo, y Model es donde queda dicha.
	//
	// Es el espejo exacto de Vin (§3.3 del plan): ctor que valida, sin igualdad escrita. Y la
	// razon de no tener igualdad es la misma que en Vin -- nada en este slice compara dos
	// modelos, y si algun dia hiciera falta preguntar "son el mismo", eso es una OPERACION CON
	// NOMBRE y no un Equals (E23).
	//
	// Lo que Model NO lleva, y es deliberado: nada del vehiculo. La especificacion sabe que
	// requiere un modelo y no sabe nada de ningun carro en particular (§8 del brief). Un campo
	// de vehiculo aca seria el modulo empezando a guardar estado por vehiculo, que es
	// exactamente lo que lo mantiene compartible.
	[TestFixture]
	public sealed class ASpecificationIsAboutOneModel
	{
		private const string TheModel = "YARIS-2019";

		[Test]
		public void ASpecificationWithoutAModelHasNoModelToBeAbout()
		{
			Assert.Throws<DomainException>(() => new Model(null));
			Assert.Throws<DomainException>(() => new Model(""));
			Assert.Throws<DomainException>(() => new Model("   "),
				"La especificacion es UNA POR MODELO. Una identidad en blanco no es un modelo sin "
				+ "nombre todavia: es una especificacion que no se puede decir de que modelo es.");
		}

		[Test]
		public void TheModelKeepsTheCodeItWasCreatedWith()
		{
			Assert.That(new Model(TheModel).Value, Is.EqualTo(TheModel));
		}

		// Dos modelos con el mismo codigo son dos objetos distintos, y eso es lo correcto: la
		// comparacion que este dominio tiene es la de referencia, la que viene de fabrica. Si
		// esto diera true, alguien escribio igualdad de valor -- y un diccionario con clave de
		// objeto de dominio viene detras (E23).
		[Test]
		public void TwoModelsWithTheSameCodeAreNotTheSameObject()
		{
			Model uno = new Model(TheModel);
			Model otro = new Model(TheModel);

			Assert.That(uno.Equals(otro), Is.False,
				"E23: iguales solo si son el MISMO objeto. Un Equals escrito aca abre la puerta a "
				+ "indexar por objeto de dominio, que se rompe en silencio despues de una "
				+ "rehidratacion.");
			Assert.That(ReferenceEquals(uno, otro), Is.False);
		}

		// EL CONSTRUCTOR ES LA CREACION (E20): no hay operacion de creacion aparte, y lo que
		// recibe es el codigo del modelo y nada mas. Todo lo demas -- revisiones, items
		// publicados -- llega despues por sus propios comandos.
		[Test]
		public void TheConstructorIsCreationAndItTakesTheModelCode()
		{
			ModelSpecification specification = new ModelSpecification(TheModel);

			Assert.That(specification.ReadModel(), Is.EqualTo(TheModel));
		}

		[Test]
		public void ASpecificationWithoutAModelCannotBeOpenedEither()
		{
			Assert.Throws<DomainException>(() => new ModelSpecification(null));
			Assert.Throws<DomainException>(() => new ModelSpecification(""));
			Assert.Throws<DomainException>(() => new ModelSpecification("   "));
		}

		// LO QUE MANTIENE AL AGREGADO COMPARTIBLE, mecanizado en vez de prometido (§8 del brief):
		// no guarda NADA de ningun vehiculo en particular. Un solo campo de vehiculo aca y la
		// especificacion deja de poder ser una por modelo -- pasaria a ser una por carro, que es
		// justo lo que la decision de topologia evito.
		//
		// Los hechos del vehiculo que las lecturas necesitan (L7) LLEGAN COMO PARAMETROS y se van
		// con la respuesta; nunca se quedan. Por eso la prueba mira los campos, y no las firmas:
		// recibir un odometro esta bien, guardarlo no.
		[Test]
		public void TheSpecificationHoldsNothingAboutAnyParticularCar()
		{
			List<string> delVehiculo = typeof(ModelSpecification)
				.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(campo => EsDelVehiculo(campo.FieldType))
				.Select(campo => campo.Name + " : " + campo.FieldType.Name)
				.ToList();

			Assert.That(delVehiculo, Is.Empty,
				"La especificacion guarda hechos de un vehiculo: " + string.Join(", ", delVehiculo)
				+ ". Sabe lo que un MODELO requiere y no sabe nada de ningun carro: eso es lo que "
				+ "deja que muchos vehiculos compartan una sola.");
		}

		// Y tampoco lo guarda con el nombre puesto en un string. Un 'vin' de tipo string pasaria
		// la prueba de arriba sin problema, y seria exactamente el mismo estado por vehiculo.
		[Test]
		public void NoMemberOfTheSpecificationIsNamedAfterAVehicle()
		{
			string[] palabrasDelVehiculo = { "Vin", "Vehicle", "Odometer", "Reading", "Owner" };

			List<string> sospechosos = typeof(ModelSpecification)
				.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Select(miembro => miembro.Name)
				.Where(nombre => palabrasDelVehiculo.Any(
					palabra => nombre.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0))
				.Distinct()
				.ToList();

			Assert.That(sospechosos, Is.Empty,
				"Miembros que nombran un vehiculo: " + string.Join(", ", sospechosos) + ". "
				+ "Si un hecho del carro tiene que entrar, entra como PARAMETRO de la lectura y "
				+ "sale con la respuesta.");
		}

		private static bool EsDelVehiculo(Type tipo)
		{
			Type[] delVehiculo =
			{
				typeof(Vin), typeof(OdometerReading), typeof(MaintenanceSchedule),
				typeof(ScheduledOperation), typeof(Logbook), typeof(UsableReading),
				typeof(AnchorReading),
			};

			if (delVehiculo.Contains(tipo)) return true;

			return tipo.IsGenericType && tipo.GetGenericArguments().Any(EsDelVehiculo);
		}
	}
}
