using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que una <see cref="ReadingSource"/> cruza el wire hacia adentro.
	/// Cada miembro nombra al homonimo del conjunto cerrado; se resuelve con
	/// <see cref="ReadingSource.OfCode"/>.
	/// </summary>
	internal enum ReadingSourceCode
	{
		/// <summary>Vease <see cref="ReadingSource.HumanEntry"/>.</summary>
		HumanEntry,

		/// <summary>Vease <see cref="ReadingSource.Telemetry"/>.</summary>
		Telemetry,

		/// <summary>Vease <see cref="ReadingSource.Inspection"/>.</summary>
		Inspection,

		/// <summary>Vease <see cref="ReadingSource.CustomerDeclared"/>.</summary>
		CustomerDeclared,

		/// <summary>Vease <see cref="ReadingSource.Correction"/>.</summary>
		Correction,
	}

	/// <summary>
	/// De donde vino una lectura del odometro, que es lo que determina cuanta confianza carga.
	/// Conjunto cerrado de cinco miembros. Es un hecho recibido, no deducido.
	/// </summary>
	internal sealed class ReadingSource
	{
		/// <summary>La persona escribio el numero en la app (OUS-09).</summary>
		internal static readonly ReadingSource HumanEntry = new ReadingSource("HumanEntry");

		/// <summary>El vehiculo lo reporto solo.</summary>
		internal static readonly ReadingSource Telemetry = new ReadingSource("Telemetry");

		/// <summary>Lo leyo el taller durante una revision.</summary>
		internal static readonly ReadingSource Inspection = new ReadingSource("Inspection");

		/// <summary>La persona lo declaro en un tramite, no en la pantalla del odometro.</summary>
		internal static readonly ReadingSource CustomerDeclared =
			new ReadingSource("CustomerDeclared");

		/// <summary>Enmienda de una lectura anterior. No borra la que estaba: entra como un hecho mas.</summary>
		internal static readonly ReadingSource Correction = new ReadingSource("Correction");

		/// <summary>El nombre del miembro, para proyectarlo como escalar y para diagnosticar.</summary>
		internal string Name { get; }

		private ReadingSource(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro. Total con excepcion: un codigo desconocido es un
		/// defecto de quien llama y revienta (E32).
		/// </summary>
		internal static ReadingSource OfCode(ReadingSourceCode code)
		{
			switch (code)
			{
				case ReadingSourceCode.HumanEntry: return HumanEntry;
				case ReadingSourceCode.Telemetry: return Telemetry;
				case ReadingSourceCode.Inspection: return Inspection;
				case ReadingSourceCode.CustomerDeclared: return CustomerDeclared;
				case ReadingSourceCode.Correction: return Correction;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna fuente de lectura. El conjunto es cerrado: una fuente "
				+ "que nadie declaro no llega con menos confianza, llega mal.");
		}
	}
}
