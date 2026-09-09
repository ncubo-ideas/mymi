using System;

namespace Concesionario.Dominio
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
	/// De donde vino una lectura del odometro. Conjunto cerrado de cinco miembros. Es un hecho
	/// recibido, no deducido.
	/// <para>
	/// Se DICE en cada fila del registro y nada se pesa, se promedia ni se descarta por ella:
	/// decirla es la obligacion entera. Una serie que mostrara un punto declarado por la persona
	/// como si fuera medido seria un registro que se tergiversa a si mismo, y eso es lo que hace
	/// falta evitar; ponderar la lectura seria el dominio juzgando el registro de la persona.
	/// </para>
	/// </summary>
	internal sealed class ReadingSource
	{
		/// <summary>La escribio una persona a mano, en el sistema que la recibio.</summary>
		internal static readonly ReadingSource HumanEntry =
			new ReadingSource("HumanEntry", "anotada a mano");

		/// <summary>El vehiculo lo reporto solo.</summary>
		internal static readonly ReadingSource Telemetry =
			new ReadingSource("Telemetry", "reportada por el vehículo");

		/// <summary>Lo leyo el taller durante una revision.</summary>
		internal static readonly ReadingSource Inspection =
			new ReadingSource("Inspection", "leída en una revisión");

		/// <summary>La persona lo declaro en un tramite, no en la pantalla del odometro.</summary>
		internal static readonly ReadingSource CustomerDeclared =
			new ReadingSource("CustomerDeclared", "declarada por la persona");

		/// <summary>Enmienda de una lectura anterior. No borra la que estaba: entra como un hecho mas.</summary>
		internal static readonly ReadingSource Correction =
			new ReadingSource("Correction", "corrección de una lectura anterior");

		/// <summary>El nombre del miembro, para proyectarlo como escalar y para diagnosticar.</summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta fuente en la frase que alguien lee. NO es lo que cruza la puerta: eso
		/// sigue siendo <see cref="Name"/>. Vive aqui para que ninguna pantalla tenga que llevar su
		/// propia tabla y quedar en desacuerdo con esta.
		/// </summary>
		internal string HowItIsSaid { get; }

		private ReadingSource(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro. Total con excepcion: un codigo desconocido es un
		/// defecto de quien llama y revienta.
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
				+ "que nadie declaro no es una fuente menos creible, es un defecto de quien llama.");
		}
	}
}
