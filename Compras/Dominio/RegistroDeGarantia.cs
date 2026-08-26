using System;

namespace Compras.Dominio
{
	/// <summary>
	/// El resultado de buscar la garantia de un producto: el respaldo completo — en que
	/// compra fue, cuando, cuanta cobertura y si esta vigente en la fecha de consulta.
	/// Total: buscar algo que no esta devuelve el registro Ausente, no una excepcion.
	/// </summary>
	public abstract class RegistroDeGarantia
	{
		/// <summary>Consulta de presencia, total.</summary>
		public abstract bool Encontrada { get; }

		public string TextoBuscado { get; private set; }

		public abstract string Sku { get; }
		public abstract string Descripcion { get; }
		public abstract string NumeroDeCompra { get; }
		public abstract DateTime FechaDeCompra { get; }
		public abstract int MesesDeCobertura { get; }
		public abstract DateTime VenceEl { get; }
		public abstract bool Vigente { get; }

		private RegistroDeGarantia(string textoBuscado)
		{
			ArgumentNullException.ThrowIfNull(textoBuscado);
			TextoBuscado = textoBuscado;
		}

		internal static RegistroDeGarantia NoEncontrada(string textoBuscado) =>
			new Ausente(textoBuscado);

		internal static RegistroDeGarantia De(string textoBuscado, Compra compra, Item item,
			DateTime fechaDeConsulta)
		{
			ArgumentNullException.ThrowIfNull(compra);
			ArgumentNullException.ThrowIfNull(item);

			return new Hallada(textoBuscado, compra, item, fechaDeConsulta);
		}

		private sealed class Ausente : RegistroDeGarantia
		{
			internal Ausente(string textoBuscado) : base(textoBuscado) { }

			public override bool Encontrada => false;

			public override string Sku => "";
			public override string Descripcion => "";
			public override string NumeroDeCompra => "";
			public override DateTime FechaDeCompra => default;
			public override int MesesDeCobertura => 0;
			public override DateTime VenceEl => default;
			public override bool Vigente => false;
		}

		private sealed class Hallada : RegistroDeGarantia
		{
			private readonly Compra compra;
			private readonly Item item;
			private readonly DateTime fechaDeConsulta;

			internal Hallada(string textoBuscado, Compra compra, Item item, DateTime fechaDeConsulta)
				: base(textoBuscado)
			{
				this.compra = compra;
				this.item = item;
				this.fechaDeConsulta = fechaDeConsulta;
			}

			public override bool Encontrada => true;

			public override string Sku => item.Sku;
			public override string Descripcion => item.Descripcion;
			public override string NumeroDeCompra => compra.Numero;
			public override DateTime FechaDeCompra => compra.Fecha;
			public override int MesesDeCobertura => item.Garantia.MesesDeCobertura;
			public override DateTime VenceEl => item.Garantia.VenceEl(compra.Fecha);
			public override bool Vigente => item.Garantia.EstaVigenteEl(compra.Fecha, fechaDeConsulta);
		}
	}
}
