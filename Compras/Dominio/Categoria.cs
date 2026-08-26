using System;

namespace Compras.Dominio
{
	/// <summary>
	/// El departamento de un producto del supermercado: el conocimiento que convierte una
	/// linea de ticket en algo analizable. Conjunto cerrado, comparado por referencia;
	/// el string del proveedor se resuelve UNA vez en la frontera con Desde().
	/// </summary>
	// Es un hecho de gondola, no una valoracion: Azucarados existe porque el super tiene
	// ese pasillo. Lo que un departamento SIGNIFIQUE para la salud de la persona no se
	// decide aca -ni en ninguna parte de este modulo-: el super no sabe nada del
	// colesterol, y el cruce es del asistente.
	//
	// Nombre es la LLAVE -la que viene en el evento y ya esta escrita en los diarios- y
	// Titulo es lo que lee una persona. Son distintos a proposito: la llave no puede
	// cambiar sin romper lo ya escrito, y "FrutasYVerduras" no se le muestra a nadie.
	//
	// Una pregunta que NO contesta todo el conjunto, y por eso es guardada:
	//
	//   DiasDeReabastecimiento solo lo que se repone con regularidad. Los antojos NO
	//                          tienen periodicidad esperada A PROPOSITO: este modulo no
	//                          existe para decirle a nadie que hace un mes que no compra
	//                          helado. Y un electrodomestico no se reabastece.
	public abstract class Categoria
	{
		public static readonly Categoria Proteinas =
			new Comestible("Proteinas", "proteínas", "una proteína", diasDeReabastecimiento: 10);
		public static readonly Categoria FrutasYVerduras =
			new Comestible("FrutasYVerduras", "frutas y verduras", "una fruta o verdura", diasDeReabastecimiento: 10);
		public static readonly Categoria LacteosYHuevos =
			new Comestible("LacteosYHuevos", "lácteos y huevos", "un lácteo", diasDeReabastecimiento: 12);
		public static readonly Categoria GranosYAbarrotes =
			new Comestible("GranosYAbarrotes", "granos y abarrotes", "un grano o abarrote", diasDeReabastecimiento: 35);
		public static readonly Categoria AceitesYGrasas =
			new Comestible("AceitesYGrasas", "aceites y grasas", "un aceite o una grasa", diasDeReabastecimiento: 0);
		public static readonly Categoria Azucarados =
			new Comestible("Azucarados", "dulces y postres", "algo dulce", diasDeReabastecimiento: 0);
		public static readonly Categoria Snacks =
			new Comestible("Snacks", "snacks", "un snack", diasDeReabastecimiento: 0);
		public static readonly Categoria Limpieza =
			new NoComestible("Limpieza", "limpieza", "algo de limpieza", diasDeReabastecimiento: 45);
		public static readonly Categoria Electrohogar =
			new NoComestible("Electrohogar", "electrodomésticos", "un electrodoméstico", diasDeReabastecimiento: 0);

		private readonly int diasDeReabastecimiento;

		/// <summary>La LLAVE del departamento, la que viaja en el evento. No se le muestra a nadie.</summary>
		public string Nombre { get; private set; }

		/// <summary>El nombre del departamento como lo lee una persona: "frutas y verduras".</summary>
		public string Titulo { get; private set; }

		/// <summary>Como se menciona UNO de sus productos dentro de una frase: "una fruta o verdura".</summary>
		public string Mencion { get; private set; }

		/// <summary>Si el departamento es de comida. Es donde esta la gondola, no un juicio.</summary>
		public abstract bool EsComestible { get; }

		/// <summary>Consulta de presencia, total: si este departamento espera reponerse cada tanto.</summary>
		public bool SeReabastece => diasDeReabastecimiento > 0;

		/// <summary>Cada cuantos dias se repone normalmente. Preguntar SeReabastece primero.</summary>
		public int DiasDeReabastecimiento
		{
			get
			{
				if (!SeReabastece)
					throw new InvalidOperationException(
						$"El departamento '{Nombre}' no tiene periodicidad de reabastecimiento. "
						+ "Preguntar SeReabastece antes.");
				return diasDeReabastecimiento;
			}
		}

		private Categoria(string nombre, string titulo, string mencion, int diasDeReabastecimiento)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(titulo);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(mencion);
			if (diasDeReabastecimiento < 0)
				throw new ArgumentOutOfRangeException(nameof(diasDeReabastecimiento), diasDeReabastecimiento,
					"Los dias de reabastecimiento no pueden ser negativos; cero significa que no se reabastece.");

			Nombre = nombre;
			Titulo = titulo;
			Mencion = mencion;
			this.diasDeReabastecimiento = diasDeReabastecimiento;
		}

		/// <summary>El resolver de la frontera: un nombre desconocido es un error de quien llama.</summary>
		public static Categoria Desde(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			foreach (Categoria categoria in Todas)
				if (string.Equals(categoria.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
					return categoria;

			throw new ArgumentException($"No existe el departamento '{nombre}'.", nameof(nombre));
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
			internal Comestible(string nombre, string titulo, string mencion, int diasDeReabastecimiento)
				: base(nombre, titulo, mencion, diasDeReabastecimiento) { }

			public override bool EsComestible => true;
		}

		private sealed class NoComestible : Categoria
		{
			internal NoComestible(string nombre, string titulo, string mencion, int diasDeReabastecimiento)
				: base(nombre, titulo, mencion, diasDeReabastecimiento) { }

			public override bool EsComestible => false;
		}
	}
}
