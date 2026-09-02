using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que un <see cref="FluidKind"/> cruza el wire hacia adentro. Lo que viaja
	/// es el NOMBRE del miembro, asi que el numero no significa nada; se resuelve con
	/// <see cref="FluidKind.OfCode"/>.
	/// </summary>
	internal enum FluidKindCode
	{
		/// <summary>Vease <see cref="FluidKind.EngineOil"/>.</summary>
		EngineOil,

		/// <summary>Vease <see cref="FluidKind.BrakeFluid"/>.</summary>
		BrakeFluid,

		/// <summary>Vease <see cref="FluidKind.Coolant"/>.</summary>
		Coolant,

		/// <summary>Vease <see cref="FluidKind.TransmissionFluid"/>.</summary>
		TransmissionFluid,

		/// <summary>Vease <see cref="FluidKind.PowerSteeringFluid"/>.</summary>
		PowerSteeringFluid,

		/// <summary>Vease <see cref="FluidKind.WasherFluid"/>.</summary>
		WasherFluid,
	}

	/// <summary>
	/// De que fluido habla una especificacion publicada. Conjunto cerrado de seis miembros,
	/// comparados por identidad: son las seis clases de fluido sobre las que el fabricante
	/// publica grado, capacidad e intervalo de cambio.
	/// </summary>
	internal sealed class FluidKind
	{
		/// <summary>El aceite del motor. Es el que la persona pregunta primero.</summary>
		internal static readonly FluidKind EngineOil = new FluidKind("EngineOil", "Aceite de motor");

		/// <summary>El liquido de frenos.</summary>
		internal static readonly FluidKind BrakeFluid = new FluidKind("BrakeFluid", "Líquido de frenos");

		/// <summary>El refrigerante del motor.</summary>
		internal static readonly FluidKind Coolant = new FluidKind("Coolant", "Refrigerante");

		/// <summary>El liquido de la transmision.</summary>
		internal static readonly FluidKind TransmissionFluid =
			new FluidKind("TransmissionFluid", "Líquido de transmisión");

		/// <summary>El liquido de la direccion asistida.</summary>
		internal static readonly FluidKind PowerSteeringFluid =
			new FluidKind("PowerSteeringFluid", "Líquido de dirección asistida");

		/// <summary>El liquido del limpiaparabrisas.</summary>
		internal static readonly FluidKind WasherFluid = new FluidKind("WasherFluid", "Líquido de limpiaparabrisas");

		/// <summary>
		/// El nombre. Es lo unico que sale del conjunto hacia afuera; el objeto no se entrega.
		/// </summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta clase de fluido en la frase que alguien lee. NO es lo que cruza la
		/// puerta ni lo que viaja en el wire: eso sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private FluidKind(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro, una sola vez al entrar. Total con excepcion: un
		/// codigo desconocido es un defecto de quien llama y revienta.
		/// </summary>
		internal static FluidKind OfCode(FluidKindCode code)
		{
			switch (code)
			{
				case FluidKindCode.EngineOil: return EngineOil;
				case FluidKindCode.BrakeFluid: return BrakeFluid;
				case FluidKindCode.Coolant: return Coolant;
				case FluidKindCode.TransmissionFluid: return TransmissionFluid;
				case FluidKindCode.PowerSteeringFluid: return PowerSteeringFluid;
				case FluidKindCode.WasherFluid: return WasherFluid;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna clase de fluido. El conjunto es cerrado: afuera no hay "
				+ "miembro, asi que esto no es una respuesta vacia sino un defecto de quien llama.");
		}
	}
}
