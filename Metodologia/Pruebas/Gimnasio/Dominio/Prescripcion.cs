using System;

namespace Gimnasio.Dominio
{
	// COMO SE PRESCRIBE UN EJERCICIO. El papel lo dice de dos formas y NO son la misma
	// cosa: "3x 12 a 15" para la fuerza y "15 minutos" para el cardio. Es la pregunta de
	// la pasada 1 -¿significa lo mismo en todas las filas?- contestada que no, igual que
	// el Periodo del recibo municipal que era mes en unas filas y trimestre en otras.
	//
	// Dos clases y no una con un `tipo` adentro porque OPINAN distinto: cada una sabe que
	// numero es su magnitud para la progresion (N8) y como se llama. Un switch por tipo
	// en cada lectura es la porosidad que el capitulo 4 manda evitar.
	//
	// LO QUE NINGUNA TRAE: EL PESO. El v1 y el v2 lo tenian en cada ejercicio; el papel no
	// lo muestra. Vive aparte, opcional, en EjercicioPrescrito -hueco 17-.
	internal abstract class Prescripcion
	{
		// El numero sobre el que se lee la progresion, y como se llama. N8, sin firmar:
		// volumen para fuerza, minutos para cardio.
		internal abstract decimal Magnitud { get; }
		internal abstract string EnQueSeMide { get; }

		// La hoja para el actor, con las palabras del papel (regla 13).
		internal abstract string Etiqueta { get; }

		internal bool EsDeLaMismaFormaQue(Prescripcion otra)
		{
			return otra != null && otra.EnQueSeMide == EnQueSeMide;
		}
	}

	// FUERZA: series por un rango de repeticiones, con descanso entre series.
	internal sealed class PrescripcionDeFuerza : Prescripcion
	{
		internal int Series { get; }
		internal int RepeticionesDesde { get; }
		internal int RepeticionesHasta { get; }
		internal int DescansoEnSegundos { get; }

		internal PrescripcionDeFuerza(int series, int repeticionesDesde, int repeticionesHasta, int descansoEnSegundos)
		{
			if (series <= 0)
				throw new ArgumentOutOfRangeException(nameof(series), series, "Las series tienen que ser positivas.");
			if (repeticionesDesde <= 0)
				throw new ArgumentOutOfRangeException(nameof(repeticionesDesde), repeticionesDesde,
					"Las repeticiones tienen que ser positivas.");
			if (repeticionesHasta < repeticionesDesde)
				throw new ArgumentOutOfRangeException(nameof(repeticionesHasta), repeticionesHasta,
					"El rango de repeticiones esta invertido.");
			if (descansoEnSegundos < 0)
				throw new ArgumentOutOfRangeException(nameof(descansoEnSegundos), descansoEnSegundos,
					"El descanso no puede ser negativo.");

			Series = series;
			RepeticionesDesde = repeticionesDesde;
			RepeticionesHasta = repeticionesHasta;
			DescansoEnSegundos = descansoEnSegundos;
		}

		// EL VOLUMEN: series por el tope del rango. Es la propuesta de N8 y esta sin
		// firmar; si se firma otra ley -el promedio del rango, el piso- cambia aca y en
		// ningun otro lado.
		internal override decimal Magnitud { get { return Series * RepeticionesHasta; } }
		internal override string EnQueSeMide { get { return "volumen"; } }

		internal override string Etiqueta
		{
			get { return Series + "x " + RepeticionesDesde + " a " + RepeticionesHasta; }
		}
	}

	// CARDIO: una duracion, y nada mas.
	internal sealed class PrescripcionDeCardio : Prescripcion
	{
		internal int Minutos { get; }

		internal PrescripcionDeCardio(int minutos)
		{
			if (minutos <= 0)
				throw new ArgumentOutOfRangeException(nameof(minutos), minutos, "Los minutos tienen que ser positivos.");
			Minutos = minutos;
		}

		internal override decimal Magnitud { get { return Minutos; } }
		internal override string EnQueSeMide { get { return "minutos"; } }
		internal override string Etiqueta { get { return Minutos + " minutos"; } }
	}
}
