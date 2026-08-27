using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Lo que Sonia pago por litro en una carga, con el surtidor que lo desplegaba. La
	/// serie de estas filas es la historia del precio EN LOS SURTIDORES DONDE ELLA
	/// CARGO -por eso el precio vive en el surtidor y no en la carga (§6.3.2)-, no una
	/// lista de numeros sueltos.
	/// </summary>
	public sealed class PrecioEnElTiempo
	{
		public DateTime Fecha { get; private set; }
		public decimal PrecioPorLitro { get; private set; }
		public Moneda Moneda { get; private set; }
		public string Surtidor { get; private set; }

		internal PrecioEnElTiempo(DateTime fecha, decimal precioPorLitro, Moneda moneda,
			string surtidor)
		{
			ArgumentNullException.ThrowIfNull(moneda);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(surtidor);

			Fecha = fecha;
			PrecioPorLitro = precioPorLitro;
			Moneda = moneda;
			Surtidor = surtidor;
		}

		public string CodigoDeMoneda => Moneda.Codigo;
	}
}
