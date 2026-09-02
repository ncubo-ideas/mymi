using System;
using System.Collections.Generic;

namespace Gimnasio.Dominio
{
	// EL PLAN QUE EL GIMNASIO LE ARMA. Es la unidad de tiempo de este dominio, y eso es
	// el juicio 3: el ritmo NO es del calendario, es del plan. "Se toman medidas al
	// final de cada plan; los planes podrian ser trimestrales pero no es fijo".
	//
	// De ahi sale la ausencia que ninguna pantalla puede mostrar: no es "hace tres
	// meses que no te miden" sino "tu plan termino y nadie te midio".
	//
	// El plan lleva fechas porque el gimnasio se las manda, pero NO las lee del reloj:
	// terminar es un hecho que entra, no algo que el dominio deduzca del dia de hoy.
	internal sealed class Plan
	{
		private readonly List<EjercicioPrescrito> ejercicios = new List<EjercicioPrescrito>();

		internal DateTime Inicio { get; }
		internal bool Termino { get; private set; }
		internal DateTime Fin { get; private set; }
		internal int DiasEsperadosPorSemana { get; }

		internal IReadOnlyList<EjercicioPrescrito> Ejercicios { get { return ejercicios; } }

		internal Plan(DateTime inicio, int diasEsperadosPorSemana)
		{
			if (diasEsperadosPorSemana <= 0 || diasEsperadosPorSemana > 7)
				throw new ArgumentOutOfRangeException(nameof(diasEsperadosPorSemana), diasEsperadosPorSemana,
					"Un plan espera entre 1 y 7 dias por semana.");

			Inicio = inicio;
			DiasEsperadosPorSemana = diasEsperadosPorSemana;
		}

		internal void Prescribir(EjercicioPrescrito ejercicio)
		{
			ArgumentNullException.ThrowIfNull(ejercicio);
			foreach (EjercicioPrescrito ya in ejercicios)
				if (ya.EsElMismo(ejercicio.Nombre))
					throw new InvalidOperationException(
						$"El plan ya prescribe '{ejercicio.Nombre}'. Dos pesos indicados para el mismo "
						+ "ejercicio en el mismo plan no se pueden distinguir.");
			ejercicios.Add(ejercicio);
		}

		internal void Terminar(DateTime fin)
		{
			if (Termino)
				throw new InvalidOperationException("Este plan ya habia terminado.");
			if (fin < Inicio)
				throw new ArgumentOutOfRangeException(nameof(fin), fin,
					"Un plan no puede terminar antes de empezar.");

			Termino = true;
			Fin = fin;
		}

		// PREDICADOS TOTALES.
		internal bool Prescribe(string nombreDelEjercicio)
		{
			foreach (EjercicioPrescrito e in ejercicios)
				if (e.EsElMismo(nombreDelEjercicio)) return true;
			return false;
		}

		internal decimal PesoIndicadoDe(string nombreDelEjercicio)
		{
			foreach (EjercicioPrescrito e in ejercicios)
				if (e.EsElMismo(nombreDelEjercicio)) return e.PesoIndicado;
			throw new KeyNotFoundException(
				$"Este plan no prescribe '{nombreDelEjercicio}'. Preguntar Prescribe antes.");
		}

		// EL DOMINIO DICE DURACIONES, NO FECHAS -regla 14 del substrato-. Las semanas
		// son lo que la persona entiende; la fecha queda en su campo para quien la
		// necesite formatear, que es trabajo de la superficie.
		internal int SemanasQueDuro()
		{
			if (!Termino)
				throw new InvalidOperationException(
					"Este plan no ha terminado, asi que no duro nada todavia. Preguntar Termino antes.");
			int dias = (int)(Fin - Inicio).TotalDays;
			return dias / 7;
		}

		internal int DiasEsperadosEnTodoElPlan()
		{
			return SemanasQueDuro() * DiasEsperadosPorSemana;
		}
	}
}
