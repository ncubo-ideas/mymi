using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que un <see cref="FuelGradeStanding"/> cruza el wire hacia adentro. Lo
	/// que viaja es el NOMBRE del miembro; se resuelve con
	/// <see cref="FuelGradeStanding.OfCode"/>.
	/// </summary>
	internal enum FuelGradeStandingCode
	{
		/// <summary>Vease <see cref="FuelGradeStanding.Minimum"/>.</summary>
		Minimum,

		/// <summary>Vease <see cref="FuelGradeStanding.Permitted"/>.</summary>
		Permitted,

		/// <summary>Vease <see cref="FuelGradeStanding.Prohibited"/>.</summary>
		Prohibited,
	}

	/// <summary>
	/// Como esta parado un grado de combustible en lo que el fabricante publico. Conjunto
	/// cerrado de tres miembros, comparados por identidad: la respuesta a "que combustible le
	/// pongo" no es una lista, son tres listas con significados distintos.
	/// </summary>
	internal sealed class FuelGradeStanding
	{
		/// <summary>
		/// El grado minimo. Por debajo de este el fabricante no responde por el motor.
		/// </summary>
		internal static readonly FuelGradeStanding Minimum = new FuelGradeStanding("Minimum", "mínimo");

		/// <summary>Permitido: se puede usar sin consecuencia publicada.</summary>
		internal static readonly FuelGradeStanding Permitted =
			new FuelGradeStanding("Permitted", "permitido");

		/// <summary>
		/// Prohibido. Es la mitad que de verdad importa decir: un grado prohibido callado es un
		/// motor danado.
		/// </summary>
		internal static readonly FuelGradeStanding Prohibited =
			new FuelGradeStanding("Prohibited", "prohibido");

		/// <summary>
		/// El nombre. Es lo unico que sale del conjunto hacia afuera; el objeto no se entrega.
		/// </summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta postura en la frase que alguien lee. NO es lo que cruza la puerta ni
		/// lo que viaja en el wire: eso sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private FuelGradeStanding(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro, una sola vez al entrar. Total con excepcion: un
		/// codigo desconocido es un defecto de quien llama y revienta.
		/// </summary>
		internal static FuelGradeStanding OfCode(FuelGradeStandingCode code)
		{
			switch (code)
			{
				case FuelGradeStandingCode.Minimum: return Minimum;
				case FuelGradeStandingCode.Permitted: return Permitted;
				case FuelGradeStandingCode.Prohibited: return Prohibited;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna postura sobre un grado de combustible. Hay exactamente "
				+ "tres; una desconocida no es una cuarta, es un defecto de quien llama.");
		}
	}
}
