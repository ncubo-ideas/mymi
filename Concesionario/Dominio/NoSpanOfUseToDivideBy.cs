using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El periodo no tiene tramo de uso por el que dividir el gasto, asi que no hay costo por
	/// distancia que decir. Es la constancia con nombre, como <see cref="NoCostOnRecord"/> lo es
	/// para un periodo sin gasto.
	/// <para>
	/// El motivo llega DICHO por la serie del odometro, que es de quien es esa ley, y se pasa
	/// entero. Escribirlo aqui de nuevo dejaria dos versiones de una misma razon que pueden
	/// discrepar sin que nada lo vea.
	/// </para>
	/// </summary>
	internal sealed class NoSpanOfUseToDivideBy : ACostPerDistance
	{
		private readonly string _whyTheRateCannotBeSaid;

		/// <summary>Se llega por <see cref="ACostPerDistance.WithNoSpanOfUse"/>.</summary>
		internal NoSpanOfUseToDivideBy(DateTime from, DateTime to, string whyTheRateCannotBeSaid)
			: base(from, to)
		{
			_whyTheRateCannotBeSaid = whyTheRateCannotBeSaid;
		}

		/// <summary>
		/// El periodo, que no hay tramo por el que dividir, y el motivo tal como la serie del
		/// odometro lo dijo. TOTAL, y sin ninguna cifra: un cero por unidad seria una division
		/// inventada.
		/// </summary>
		internal override ACostPerDistanceClause TheClauseItComposes()
		{
			return ACostPerDistanceClause.WithNoSpanOfUse(
				TheInstantItStartsAt(), TheInstantItEndsAt(), _whyTheRateCannotBeSaid);
		}
	}
}
