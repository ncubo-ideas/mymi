using System;

namespace DealershipCar.Dominio
{
	// LA UNIDAD EN LA QUE ESTA EXPRESADA UNA DISTANCIA. Dos unidades, y NUNCA SE CONVIERTEN.
	//
	// ESA AUSENCIA ES EL TIPO ENTERO. OINV-21 / ley L7: los limites de distancia se evaluan
	// solo contra lecturas en la MISMA unidad del plan; una discrepancia SUSPENDE la
	// evaluacion de distancia para esa operacion y la deja cayendo a solo-tiempo. No convierte.
	//
	// Asi que aca no hay Convert, no hay factor, no hay ninguna aritmetica que mezcle unidades
	// -- y no por minimalismo: UN CONVERT ES UN DEFECTO, no una comodidad. Con conversion nunca
	// habria discrepancia que suspender, porque siempre se podria convertir, y L7 se quedaria
	// sin significado. La ley existe justamente para que el sistema DIGA "no puedo comparar
	// esto" en vez de inventar un numero. Un numero inventado en una agenda de mantenimiento
	// es una operacion que parece al dia y no lo esta.
	//
	// Hay una prueba con LISTA BLANCA sobre este tipo: expone exactamente UnitA, UnitB, Name y
	// OfCode. Cualquier agregado la pone roja, porque una ausencia no se puede afirmar
	// llamando a algo.
	//
	// UnitA y UnitB son los nombres del canon (§3) y son ABSTRACTOS a proposito. No dice
	// kilometros ni millas, y el canon prohibe "mileage", "km" y "miles" como nombres de
	// dominio: en el momento en que el tipo dice "km" alguien sabe cuanto vale en millas y la
	// conversion se escribe sola.
	//
	// LAS UNIDADES SE COMPARAN POR IDENTIDAD, no por Name. Comparar el nombre seria igualdad
	// de valor disfrazada de string (E23), y es el mismo reflejo que termina en "si son
	// distintas, convierto". Name existe para proyectar y para diagnosticar, no para decidir.

	/// <summary>
	/// El codigo con el que una <see cref="DistanceUnit"/> cruza el wire hacia adentro.
	/// Los valores son implicitos a proposito: lo que viaja es el NOMBRE del miembro, asi que
	/// el numero no significa nada. Se resuelve con <see cref="DistanceUnit.OfCode"/>.
	/// </summary>
	internal enum DistanceUnitCode
	{
		/// <summary>Vease <see cref="DistanceUnit.UnitA"/>.</summary>
		UnitA,

		/// <summary>Vease <see cref="DistanceUnit.UnitB"/>.</summary>
		UnitB,
	}

	/// <summary>
	/// La unidad en la que esta expresada una distancia -- la de un limite del plan o la de una
	/// lectura del odometro. Conjunto cerrado de dos miembros, comparados por identidad, que
	/// NUNCA se inter-convierten: esa ausencia es OINV-21 / ley L7, y un metodo de conversion
	/// aca es un defecto y no una comodidad.
	/// </summary>
	internal sealed class DistanceUnit
	{
		/// <summary>
		/// Una de las dos unidades. Abstracta a proposito: el dominio no necesita saber cual es
		/// para saber que no la puede comparar con la otra.
		/// </summary>
		internal static readonly DistanceUnit UnitA = new DistanceUnit("UnitA");

		/// <summary>La otra. Incomparable con <see cref="UnitA"/> por definicion, no por falta de codigo.</summary>
		internal static readonly DistanceUnit UnitB = new DistanceUnit("UnitB");

		/// <summary>
		/// El nombre del miembro, para proyectarlo en el motivo que se le muestra a la persona
		/// (ReadDueReason tiene que poder decir que hubo discrepancia de unidad) y para
		/// diagnosticar. NO es una clave de comparacion: dos unidades se comparan por
		/// identidad.
		/// </summary>
		internal string Name { get; }

		private DistanceUnit(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro. FUNCION TOTAL CON EXCEPCION: un codigo que
		/// no nombra ninguna unidad es un defecto de quien llama y revienta (E32). Se resuelve
		/// una sola vez al entrar.
		/// </summary>
		internal static DistanceUnit OfCode(DistanceUnitCode code)
		{
			switch (code)
			{
				case DistanceUnitCode.UnitA: return UnitA;
				case DistanceUnitCode.UnitB: return UnitB;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna unidad de distancia. Hay exactamente dos y no se "
				+ "convierten entre si: un codigo desconocido no es una tercera unidad, es un "
				+ "defecto de quien llama.");
		}
	}
}
