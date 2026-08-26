using System;

namespace Compras.Dominio
{
	/// <summary>
	/// La categoria de un producto del supermercado: el conocimiento que convierte una
	/// linea de ticket en algo analizable. Conjunto cerrado, comparado por referencia;
	/// el string del proveedor se resuelve UNA vez en la frontera con Desde().
	/// </summary>
	// Dos preguntas que NO contesta todo el conjunto, y por eso son guardadas:
	//
	//   Perfil                 solo lo comestible lo tiene. Un detergente no tiene perfil
	//                          de consumo, y devolver "neutro" mentiria en las
	//                          proporciones de la canasta.
	//   DiasDeReabastecimiento solo lo que se repone con regularidad. Lo ocasional NO
	//                          tiene periodicidad esperada A PROPOSITO: este modulo no
	//                          existe para decirle a nadie que hace un mes que no compra
	//                          helado. Y un electrodomestico no se reabastece.
	public abstract class Categoria
	{
		public static readonly Categoria Proteinas =
			new Comestible("Proteinas", PerfilDeConsumo.Recomendado, diasDeReabastecimiento: 10);
		public static readonly Categoria FrutasYVerduras =
			new Comestible("FrutasYVerduras", PerfilDeConsumo.Recomendado, diasDeReabastecimiento: 10);
		public static readonly Categoria LacteosYHuevos =
			new Comestible("LacteosYHuevos", PerfilDeConsumo.Neutro, diasDeReabastecimiento: 12);
		public static readonly Categoria GranosYAbarrotes =
			new Comestible("GranosYAbarrotes", PerfilDeConsumo.Neutro, diasDeReabastecimiento: 35);
		public static readonly Categoria AceitesYGrasas =
			new Comestible("AceitesYGrasas", PerfilDeConsumo.Ocasional, diasDeReabastecimiento: 0);
		public static readonly Categoria Azucarados =
			new Comestible("Azucarados", PerfilDeConsumo.Ocasional, diasDeReabastecimiento: 0);
		public static readonly Categoria Snacks =
			new Comestible("Snacks", PerfilDeConsumo.Ocasional, diasDeReabastecimiento: 0);
		public static readonly Categoria Limpieza =
			new NoComestible("Limpieza", diasDeReabastecimiento: 45);
		public static readonly Categoria Electrohogar =
			new NoComestible("Electrohogar", diasDeReabastecimiento: 0);

		private readonly int diasDeReabastecimiento;

		public string Nombre { get; private set; }

		public abstract bool EsComestible { get; }

		/// <summary>El papel en la canasta. Solo lo comestible lo tiene: preguntar EsComestible primero.</summary>
		public abstract PerfilDeConsumo Perfil { get; }

		/// <summary>Consulta de presencia, total: si esta categoria espera reponerse cada tanto.</summary>
		public bool SeReabastece => diasDeReabastecimiento > 0;

		/// <summary>Cada cuantos dias se repone normalmente. Preguntar SeReabastece primero.</summary>
		public int DiasDeReabastecimiento
		{
			get
			{
				if (!SeReabastece)
					throw new InvalidOperationException(
						$"La categoria '{Nombre}' no tiene periodicidad de reabastecimiento. "
						+ "Preguntar SeReabastece antes.");
				return diasDeReabastecimiento;
			}
		}

		private Categoria(string nombre, int diasDeReabastecimiento)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);
			if (diasDeReabastecimiento < 0)
				throw new ArgumentOutOfRangeException(nameof(diasDeReabastecimiento), diasDeReabastecimiento,
					"Los dias de reabastecimiento no pueden ser negativos; cero significa que no se reabastece.");

			Nombre = nombre;
			this.diasDeReabastecimiento = diasDeReabastecimiento;
		}

		/// <summary>El resolver de la frontera: un nombre desconocido es un error de quien llama.</summary>
		public static Categoria Desde(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			foreach (Categoria categoria in Todas)
				if (string.Equals(categoria.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
					return categoria;

			throw new ArgumentException($"No existe la categoria '{nombre}'.", nameof(nombre));
		}

		/// <summary>Reconocimiento total: la pareja de Desde, para poder preguntar sin reventar.</summary>
		public static bool Existe(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;

			foreach (Categoria categoria in Todas)
				if (string.Equals(categoria.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
					return true;

			return false;
		}

		public static readonly Categoria[] Todas =
		{
			Proteinas, FrutasYVerduras, LacteosYHuevos, GranosYAbarrotes,
			AceitesYGrasas, Azucarados, Snacks, Limpieza, Electrohogar,
		};

		private sealed class Comestible : Categoria
		{
			private readonly PerfilDeConsumo perfil;

			internal Comestible(string nombre, PerfilDeConsumo perfil, int diasDeReabastecimiento)
				: base(nombre, diasDeReabastecimiento)
			{
				ArgumentNullException.ThrowIfNull(perfil);
				this.perfil = perfil;
			}

			public override bool EsComestible => true;

			public override PerfilDeConsumo Perfil => perfil;
		}

		private sealed class NoComestible : Categoria
		{
			internal NoComestible(string nombre, int diasDeReabastecimiento)
				: base(nombre, diasDeReabastecimiento) { }

			public override bool EsComestible => false;

			public override PerfilDeConsumo Perfil =>
				throw new InvalidOperationException(
					$"La categoria '{Nombre}' no es comestible: no tiene perfil de consumo. "
					+ "Preguntar EsComestible antes de preguntar el perfil.");
		}
	}
}
