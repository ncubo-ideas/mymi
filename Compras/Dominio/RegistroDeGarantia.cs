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

		/// <summary>Cuantos dias queda de ventana para reclamar. Con signo: negativo si ya vencio.</summary>
		public abstract int DiasParaVencer { get; }

		/// <summary>
		/// Que hacer, si hay algo que hacer. Una cobertura que se cierra el mes que viene
		/// no es lo mismo que una que corre dos anos mas, y la pantalla no tiene de donde
		/// sacar esa distincion.
		/// </summary>
		public abstract Severidad Severidad { get; }

		public string NombreDeLaSeveridad => Severidad.Nombre;

		public string QueHacer => Severidad.QueHacer;

		/// <summary>La linea, escrita por el dominio con SUS hechos.</summary>
		public abstract string ParaElAsistente { get; }

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
			public override int DiasParaVencer => 0;
			public override Severidad Severidad => Severidad.Informativo;

			public override string ParaElAsistente =>
				$"no hay ninguna compra con garantía que coincida con «{TextoBuscado}»";
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

			public override int DiasParaVencer => (int)(VenceEl.Date - fechaDeConsulta.Date).TotalDays;

			// La ventana avisa cuando se esta cerrando, no antes: un aviso permanente es
			// ruido que entrena a la persona a ignorarlo.
			public override Severidad Severidad
			{
				get
				{
					if (!Vigente) return Severidad.Informativo;
					if (DiasParaVencer <= DiasEnQueYaHayQueMoverse) return Severidad.AntesDeQueSeCierre;
					return DiasParaVencer <= DiasEnQueConvieneRevisar
						? Severidad.ConvieneRevisar
						: Severidad.Informativo;
				}
			}

			public override string ParaElAsistente
			{
				get
				{
					string producto = item.Descripcion;
					if (!Vigente)
						return $"la garantía de {producto} venció el {VenceEl:dd/MM/yyyy}";

					if (DiasParaVencer <= DiasEnQueConvieneRevisar)
						return $"a la garantía de {producto} le quedan {DiasParaVencer} días: "
							+ $"vence el {VenceEl:dd/MM/yyyy}";

					return $"{producto} tiene garantía hasta el {VenceEl:dd/MM/yyyy} "
						+ $"(la compraste el {FechaDeCompra:dd/MM/yyyy})";
				}
			}
		}

		// Los dos escalones de la ventana. Un mes es "movete"; tres meses es "tenelo
		// presente".
		private const int DiasEnQueYaHayQueMoverse = 30;
		private const int DiasEnQueConvieneRevisar = 90;
	}
}
