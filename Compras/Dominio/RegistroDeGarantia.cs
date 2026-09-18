using System;

namespace Compras.Dominio
{
	/// <summary>
	/// El resultado de buscar la garantia de un producto: el respaldo completo — en que
	/// compra fue, cuando, cuanta cobertura y si esta vigente en la fecha de consulta.
	/// Total: buscar algo que no esta devuelve el registro Ausente, no una excepcion.
	/// </summary>
	internal abstract class RegistroDeGarantia
	{
		/// <summary>Consulta de presencia, total.</summary>
		internal abstract bool Encontrada { get; }

		internal string TextoBuscado { get; private set; }

		internal abstract string Sku { get; }
		internal abstract string Descripcion { get; }
		internal abstract string NumeroDeCompra { get; }
		internal abstract DateTime FechaDeCompra { get; }
		internal abstract int MesesDeCobertura { get; }
		internal abstract DateTime VenceEl { get; }
		internal abstract bool Vigente { get; }

		/// <summary>Cuantos dias queda de ventana para reclamar. Con signo: negativo si ya vencio.</summary>
		internal abstract int DiasParaVencer { get; }

		/// <summary>
		/// Que hacer, si hay algo que hacer. Una cobertura que se cierra el mes que viene
		/// no es lo mismo que una que corre dos anos mas, y la pantalla no tiene de donde
		/// sacar esa distincion.
		/// </summary>
		internal abstract Severidad Severidad { get; }

		internal string NombreDeLaSeveridad => Severidad.Nombre;

		/// <summary>
		/// QUE FRASE dice este registro, por su nombre: `no-encontrada`, `vencida`, `por-vencer` o
		/// `vigente`. Los dos escalones de la ventana los decide el dominio, no quien redacta.
		/// </summary>
		internal abstract string Clave { get; }

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

			internal override bool Encontrada => false;

			internal override string Sku => "";
			internal override string Descripcion => "";
			internal override string NumeroDeCompra => "";
			internal override DateTime FechaDeCompra => default;
			internal override int MesesDeCobertura => 0;
			internal override DateTime VenceEl => default;
			internal override bool Vigente => false;
			internal override int DiasParaVencer => 0;
			internal override Severidad Severidad => Severidad.Informativo;

			internal override string Clave => "no-encontrada";
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

			internal override bool Encontrada => true;

			internal override string Sku => item.Sku;
			internal override string Descripcion => item.Descripcion;
			internal override string NumeroDeCompra => compra.Numero;
			internal override DateTime FechaDeCompra => compra.Fecha;
			internal override int MesesDeCobertura => item.Garantia.MesesDeCobertura;
			internal override DateTime VenceEl => item.Garantia.VenceEl(compra.Fecha);
			internal override bool Vigente => item.Garantia.EstaVigenteEl(compra.Fecha, fechaDeConsulta);

			internal override int DiasParaVencer => (int)(VenceEl.Date - fechaDeConsulta.Date).TotalDays;

			// La ventana avisa cuando se esta cerrando, no antes: un aviso permanente es
			// ruido que entrena a la persona a ignorarlo.
			internal override Severidad Severidad
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

			internal override string Clave =>
				!Vigente ? "vencida"
				: DiasParaVencer <= DiasEnQueConvieneRevisar ? "por-vencer"
				: "vigente";
		}

		// Los dos escalones de la ventana. Un mes es "movete"; tres meses es "tenelo
		// presente".
		private const int DiasEnQueYaHayQueMoverse = 30;
		private const int DiasEnQueConvieneRevisar = 90;
	}
}
