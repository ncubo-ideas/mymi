using System;

namespace Gimnasio.Dominio
{
	// UN EJERCICIO DEL PLAN, tal como el papel lo trae: un nombre y COMO se prescribe
	// -series por repeticiones con descanso, o minutos-.
	//
	// EL PESO ES OPCIONAL, Y NO POR DUDA: el plan lo trae SOLO cuando la maquina lo
	// requiere -Jorge, 2026-09-08, hueco 17-. Va como bool mas valor con el getter
	// guardado, y entra por un comando aparte. La progresion no depende de el (N8).
	//
	// EL NUMERO DE MAQUINA SE QUITO. El papel lo traia en unas filas; Jorge dijo que no
	// se use (hueco 21). Lo que la persona no quiere en su modelo no entra por estar en
	// el papel.
	//
	// Ojo con lo que este tipo NO es: no es lo que Sonia hizo. La casilla para marcar el
	// ejercicio hecho se queda en el telefono y no llega al gimnasio (hueco 16). Que el
	// tipo se llame Prescrito y no a secas es deliberado.
	internal sealed class EjercicioPrescrito
	{
		private decimal pesoIndicado;

		internal string Nombre { get; }
		internal Prescripcion Prescripcion { get; }
		internal bool TienePesoIndicado { get; private set; }

		internal EjercicioPrescrito(string nombre, Prescripcion prescripcion)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			ArgumentNullException.ThrowIfNull(prescripcion);
			Nombre = nombre;
			Prescripcion = prescripcion;
		}

		// LAS DOS FORMAS DEL PAPEL, alcanzables desde un script (regla 11): un ternario
		// no unifica dos subclases de Prescripcion, asi que cada forma tiene su puerta.
		internal static EjercicioPrescrito DeFuerza(string nombre, int series, int repeticionesDesde,
			int repeticionesHasta, int descansoEnSegundos)
		{
			return new EjercicioPrescrito(nombre,
				new PrescripcionDeFuerza(series, repeticionesDesde, repeticionesHasta, descansoEnSegundos));
		}

		internal static EjercicioPrescrito DeCardio(string nombre, int minutos)
		{
			return new EjercicioPrescrito(nombre, new PrescripcionDeCardio(minutos));
		}

		// El peso se indica UNA vez (regla 18).
		internal void IndicarPeso(decimal peso)
		{
			if (peso <= 0)
				throw new ArgumentOutOfRangeException(nameof(peso), peso, "Un peso indicado tiene que ser positivo.");
			if (TienePesoIndicado)
				throw new InvalidOperationException(
					$"'{Nombre}' ya tenia peso indicado ({pesoIndicado}). Preguntar TienePesoIndicado antes.");
			pesoIndicado = peso;
			TienePesoIndicado = true;
		}

		internal decimal PesoIndicado
		{
			get
			{
				if (!TienePesoIndicado)
					throw new InvalidOperationException(
						$"'{Nombre}' no indica peso: su maquina no lo requiere, o no llego. Preguntar TienePesoIndicado antes.");
				return pesoIndicado;
			}
		}

		internal bool EsElMismo(string nombreDelEjercicio)
		{
			return !string.IsNullOrWhiteSpace(nombreDelEjercicio)
				&& string.Equals(Nombre, nombreDelEjercicio, StringComparison.OrdinalIgnoreCase);
		}
	}
}
