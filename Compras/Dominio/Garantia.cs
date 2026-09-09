using System;

namespace Compras.Dominio
{
	/// <summary>
	/// La cobertura de garantia de un producto comprado: el par presente/ausente. No es
	/// un int de meses ni un campo nullable — la ausencia tiene tipo (Ninguna), y la
	/// cobertura real sabe desde cuando corre (la fecha de la compra) y contesta si
	/// esta vigente en una fecha RECIBIDA (el dominio no lee el reloj).
	/// </summary>
	internal abstract class Garantia
	{
		/// <summary>Ausencia con nombre: lo que no trae cobertura porta esto, nunca null.</summary>
		internal static readonly Garantia Ninguna = new SinGarantia();

		internal static Garantia De(int mesesDeCobertura) => new ConGarantia(mesesDeCobertura);

		/// <summary>Consulta de presencia, total: preguntar primero, leer despues.</summary>
		internal abstract bool Existe();

		internal abstract int MesesDeCobertura { get; }

		/// <summary>Corre desde la fecha de la compra: la factura es el respaldo que el comercio honra.</summary>
		internal abstract DateTime VenceEl(DateTime fechaDeCompra);

		internal abstract bool EstaVigenteEl(DateTime fechaDeCompra, DateTime fechaDeConsulta);

		private sealed class SinGarantia : Garantia
		{
			internal override bool Existe() => false;

			internal override int MesesDeCobertura =>
				throw new InvalidOperationException("El producto no tiene garantia. Preguntar Existe() primero.");

			internal override DateTime VenceEl(DateTime fechaDeCompra) =>
				throw new InvalidOperationException("El producto no tiene garantia. Preguntar Existe() primero.");

			internal override bool EstaVigenteEl(DateTime fechaDeCompra, DateTime fechaDeConsulta) => false;
		}

		private sealed class ConGarantia : Garantia
		{
			private readonly int meses;

			internal ConGarantia(int mesesDeCobertura)
			{
				if (mesesDeCobertura <= 0)
					throw new ArgumentOutOfRangeException(nameof(mesesDeCobertura), mesesDeCobertura,
						"Una garantia sin meses de cobertura no es una garantia; lo sin cobertura es Garantia.Ninguna.");

				meses = mesesDeCobertura;
			}

			internal override bool Existe() => true;

			internal override int MesesDeCobertura => meses;

			internal override DateTime VenceEl(DateTime fechaDeCompra) => fechaDeCompra.AddMonths(meses);

			internal override bool EstaVigenteEl(DateTime fechaDeCompra, DateTime fechaDeConsulta) =>
				fechaDeConsulta <= VenceEl(fechaDeCompra);
		}
	}
}
