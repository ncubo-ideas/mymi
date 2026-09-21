using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El periodo no tiene tramo de uso por el que dividir el gasto, asi que no hay costo por
	/// distancia que decir. Es la constancia con nombre, como <see cref="NoCostOnRecord"/> lo es
	/// para un periodo sin gasto.
	/// <para>
	/// El motivo NO vive aqui: es de la serie del odometro, que lo compone en su propia clausula, y
	/// quien redacta pone una dentro de la otra. Este libro no lo escribe ni lo transporta, asi que
	/// no hay manera de que queden dos versiones de una misma razon.
	/// </para>
	/// </summary>
	internal sealed class NoSpanOfUseToDivideBy : ACostPerDistance
	{
		/// <summary>Se llega por <see cref="ACostPerDistance.WithNoSpanOfUse"/>.</summary>
		internal NoSpanOfUseToDivideBy(DateTime from, DateTime to)
			: base(from, to)
		{
		}

		/// <summary>
		/// El periodo y que no hay tramo por el que dividir. TOTAL, y sin ninguna cifra: un cero
		/// por unidad seria una division inventada.
		/// </summary>
		internal override ACostPerDistanceClause TheClauseItComposes()
		{
			return ACostPerDistanceClause.WithNoSpanOfUse(
				TheInstantItStartsAt(), TheInstantItEndsAt());
		}
	}
}
