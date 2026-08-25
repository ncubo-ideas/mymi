using System;

namespace Compras.Dominio
{
	/// <summary>
	/// La cobertura de garantia de un producto comprado: el par presente/ausente. No es
	/// un int de meses ni un campo nullable — la ausencia tiene tipo (Ninguna), y la
	/// cobertura real sabe desde cuando corre (la fecha de la compra) y contesta si
	/// esta vigente en una fecha RECIBIDA (el dominio no lee el reloj).
	/// </summary>
	public abstract class Garantia
	{
		/// <summary>Ausencia con nombre: lo que no trae cobertura porta esto, nunca null.</summary>
		public static readonly Garantia Ninguna = new SinGarantia();

		public static Garantia De(int mesesDeCobertura) => new ConGarantia(mesesDeCobertura);

		/// <summary>Consulta de presencia, total: preguntar primero, leer despues.</summary>
		public abstract bool Existe();

		public abstract int MesesDeCobertura { get; }

		/// <summary>Corre desde la fecha de la compra: la factura es el respaldo que el comercio honra.</summary>
		public abstract DateTime VenceEl(DateTime fechaDeCompra);

		public abstract bool EstaVigenteEl(DateTime fechaDeCompra, DateTime fechaDeConsulta);

		private sealed class SinGarantia : Garantia
		{
			public override bool Existe() => false;

			public override int MesesDeCobertura =>
				throw new InvalidOperationException("El producto no tiene garantia. Preguntar Existe() primero.");

			public override DateTime VenceEl(DateTime fechaDeCompra) =>
				throw new InvalidOperationException("El producto no tiene garantia. Preguntar Existe() primero.");

			public override bool EstaVigenteEl(DateTime fechaDeCompra, DateTime fechaDeConsulta) => false;
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

			public override bool Existe() => true;

			public override int MesesDeCobertura => meses;

			public override DateTime VenceEl(DateTime fechaDeCompra) => fechaDeCompra.AddMonths(meses);

			public override bool EstaVigenteEl(DateTime fechaDeCompra, DateTime fechaDeConsulta) =>
				fechaDeConsulta <= VenceEl(fechaDeCompra);
		}
	}
}
