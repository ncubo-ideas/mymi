using System;

namespace DealershipCar.Dominio
{
	// DE DONDE VINO UNA LECTURA DEL ODOMETRO. Conjunto cerrado del canon (§3).
	//
	// El canon dice para que sirve: "de donde vino una lectura -- determina su confianza". No
	// todas las lecturas valen lo mismo. Un numero que reporta el vehiculo y un numero que la
	// persona escribio de memoria son los dos legitimos y no son el mismo hecho.
	//
	// QUE NO ESTA ACA, a proposito: el ORDEN de confianza. OINV-18…OINV-31 no lo fija, y este
	// slice no lo necesita -- recibe lecturas y las ordena por INSTANTE DE OBSERVACION
	// (OINV-54), no por confianza. Inventar aca una jerarquia seria escribir una regla que
	// nadie pidio, en el tipo mas facil de creerle. Cuando llegue el slice que la necesite
	// (OS3 VehicleSignalStream, con un feed desordenado y a veces viejo), la regla se escribe
	// ahi con su nombre.
	//
	// LO QUE SI IMPORTA YA: la fuente es un hecho RECIBIDO (E44). El dominio no la deduce del
	// numero ni la infiere de quien llamo -- llega como parametro con la lectura, y por eso el
	// replay reconstruye la misma serie.

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
	/// Conjunto cerrado de cinco miembros comparados por identidad. Es un hecho recibido: el
	/// dominio no lo deduce, llega con la lectura.
	/// </summary>
	internal sealed class ReadingSource
	{
		/// <summary>
		/// La persona escribio el numero en la app. Legitimo: OUS-09 pide poder tipearlo cuando
		/// el vehiculo no esta conectado.
		/// </summary>
		internal static readonly ReadingSource HumanEntry = new ReadingSource("HumanEntry");

		/// <summary>El vehiculo lo reporto solo.</summary>
		internal static readonly ReadingSource Telemetry = new ReadingSource("Telemetry");

		/// <summary>Lo leyo el taller durante una revision.</summary>
		internal static readonly ReadingSource Inspection = new ReadingSource("Inspection");

		/// <summary>
		/// La persona lo declaro en un tramite -- al pedir una cita, por ejemplo -- y no
		/// tipeandolo en la pantalla del odometro.
		/// </summary>
		internal static readonly ReadingSource CustomerDeclared =
			new ReadingSource("CustomerDeclared");

		/// <summary>
		/// Enmienda de una lectura anterior equivocada. No borra la que estaba: entra como un
		/// hecho mas en la serie, con su propio instante de observacion.
		/// </summary>
		internal static readonly ReadingSource Correction = new ReadingSource("Correction");

		/// <summary>El nombre del miembro, para proyectarlo como escalar y para diagnosticar.</summary>
		internal string Name { get; }

		private ReadingSource(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro. FUNCION TOTAL CON EXCEPCION: un codigo
		/// desconocido es un defecto de quien llama y revienta (E32), no una fuente nueva.
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
