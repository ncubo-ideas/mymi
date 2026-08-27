using System;
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
		private const string TheModel = "SM-SEDAN-2025";

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
	}
}
