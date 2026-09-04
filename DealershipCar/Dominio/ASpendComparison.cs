using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Dos periodos dichos juntos: cada uno nombrado con sus propios limites y con sus dos vistas.
	/// <para>
	/// Nunca una diferencia suelta ni un porcentaje. Las dos cifras quedan una al lado de la otra y la
	/// resta la hace quien lee: entre dos monedas distintas esa resta no seria un numero, y decirla
	/// donde si lo es dejaria dos formas para una misma respuesta.
	/// </para>
	/// </summary>
	internal sealed class ASpendComparison
	{
		private readonly SpendInAPeriod _first;
		private readonly SpendInAPeriod _second;

		private ASpendComparison(SpendInAPeriod first, SpendInAPeriod second)
		{
			_first = first;
			_second = second;
		}

		/// <summary>
		/// La comparacion de esos dos periodos, en ese orden. Con uno solo no hay nada que comparar, y
		/// un periodo ausente no es un periodo sin gasto.
		/// </summary>
		internal static ASpendComparison Of(SpendInAPeriod first, SpendInAPeriod second)
		{
			if (first == null || second == null)
				throw new DomainException(
					"Una comparacion necesita los dos periodos: con uno solo no hay nada que "
					+ "comparar, y un periodo ausente no es un periodo sin gasto.");

			return new ASpendComparison(first, second);
		}

		/// <summary>
		/// Los dos periodos en una frase, cada uno con sus limites y sus dos vistas. TOTAL: el que no
		/// tenga ningun gasto tambien se dice, con su propia constancia.
		/// </summary>
		internal string Describe()
		{
			return "Primer período: " + _first.Describe()
				+ " Segundo período: " + _second.Describe();
		}
	}
}
