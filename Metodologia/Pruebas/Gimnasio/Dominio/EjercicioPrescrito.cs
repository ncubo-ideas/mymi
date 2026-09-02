using System;

namespace Gimnasio.Dominio
{
	// UN EJERCICIO DEL PLAN, con el peso que el gimnasio INDICA levantar.
	//
	// Ojo con lo que este tipo NO es: no es lo que Sonia levanto. Eso nadie lo registra
	// -decision de cero aporte- y por eso la pregunta original "aumente el peso que
	// levanto" murio. Lo que sobrevive es la progresion de LO PRESCRITO, que si es un
	// hecho observado porque viene en el plan.
	//
	// Que el tipo se llame Prescrito y no a secas es deliberado: el nombre tiene que
	// impedir que alguien lo lea como ejecucion dentro de seis meses.
	internal sealed class EjercicioPrescrito
	{
		internal string Nombre { get; }
		internal decimal PesoIndicado { get; }

		internal EjercicioPrescrito(string nombre, decimal pesoIndicado)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			if (pesoIndicado < 0)
				throw new ArgumentOutOfRangeException(nameof(pesoIndicado), pesoIndicado,
					"Un peso indicado no puede ser negativo.");

			Nombre = nombre;
			PesoIndicado = pesoIndicado;
		}

		internal bool EsElMismo(string nombreDelEjercicio)
		{
			return !string.IsNullOrWhiteSpace(nombreDelEjercicio)
				&& string.Equals(Nombre, nombreDelEjercicio, StringComparison.OrdinalIgnoreCase);
		}
	}
}
