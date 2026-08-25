using System;

namespace Compras.Dominio
{
	/// <summary>
	/// La categoria de un producto del supermercado: el conocimiento que convierte una
	/// linea de ticket en algo analizable. Conjunto cerrado, comparado por referencia;
	/// el string del proveedor se resuelve UNA vez en la frontera con Desde().
	/// La ausencia de perfil no es un null: es el subtipo NoComestible — un detergente
	/// no tiene perfil de consumo, y preguntarselo revienta.
	/// </summary>
	public abstract class Categoria
	{
		public static readonly Categoria Proteinas = new Comestible("Proteinas", PerfilDeConsumo.Recomendado);
		public static readonly Categoria FrutasYVerduras = new Comestible("FrutasYVerduras", PerfilDeConsumo.Recomendado);
		public static readonly Categoria LacteosYHuevos = new Comestible("LacteosYHuevos", PerfilDeConsumo.Neutro);
		public static readonly Categoria GranosYAbarrotes = new Comestible("GranosYAbarrotes", PerfilDeConsumo.Neutro);
		public static readonly Categoria AceitesYGrasas = new Comestible("AceitesYGrasas", PerfilDeConsumo.Ocasional);
		public static readonly Categoria Azucarados = new Comestible("Azucarados", PerfilDeConsumo.Ocasional);
		public static readonly Categoria Snacks = new Comestible("Snacks", PerfilDeConsumo.Ocasional);
		public static readonly Categoria Limpieza = new NoComestible("Limpieza");
		public static readonly Categoria Electrohogar = new NoComestible("Electrohogar");

		public string Nombre { get; private set; }

		public abstract bool EsComestible { get; }

		/// <summary>El papel en la canasta. Solo lo comestible lo tiene: preguntar EsComestible primero.</summary>
		public abstract PerfilDeConsumo Perfil { get; }

		private Categoria(string nombre)
		{
			Nombre = nombre;
		}

		/// <summary>El resolver de la frontera: total-con-throw, un nombre desconocido es un error de quien llama.</summary>
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

		private static readonly Categoria[] Todas =
		{
			Proteinas, FrutasYVerduras, LacteosYHuevos, GranosYAbarrotes,
			AceitesYGrasas, Azucarados, Snacks, Limpieza, Electrohogar,
		};

		private sealed class Comestible : Categoria
		{
			private readonly PerfilDeConsumo perfil;

			internal Comestible(string nombre, PerfilDeConsumo perfil) : base(nombre)
			{
				ArgumentNullException.ThrowIfNull(perfil);
				this.perfil = perfil;
			}

			public override bool EsComestible => true;

			public override PerfilDeConsumo Perfil => perfil;
		}

		private sealed class NoComestible : Categoria
		{
			internal NoComestible(string nombre) : base(nombre) { }

			public override bool EsComestible => false;

			// Un perfil inventado contaminaria las proporciones de la canasta.
			public override PerfilDeConsumo Perfil =>
				throw new InvalidOperationException(
					$"La categoria '{Nombre}' no es comestible: no tiene perfil de consumo. "
					+ "Preguntar EsComestible antes de preguntar el perfil.");
		}
	}
}
