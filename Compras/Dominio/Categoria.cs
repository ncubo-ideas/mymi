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
	internal abstract class Categoria
	{
		internal static readonly Categoria Proteinas =
			new Comestible("Proteinas", "proteínas", "una proteína", diasDeReabastecimiento: 10);
		internal static readonly Categoria FrutasYVerduras =
			new Comestible("FrutasYVerduras", "frutas y verduras", "una fruta o verdura", diasDeReabastecimiento: 10);
		internal static readonly Categoria LacteosYHuevos =
			new Comestible("LacteosYHuevos", "lácteos y huevos", "un lácteo", diasDeReabastecimiento: 12);
		internal static readonly Categoria GranosYAbarrotes =
			new Comestible("GranosYAbarrotes", "granos y abarrotes", "un grano o abarrote", diasDeReabastecimiento: 35);
		internal static readonly Categoria AceitesYGrasas =
			new Comestible("AceitesYGrasas", "aceites y grasas", "un aceite o una grasa", diasDeReabastecimiento: 0);
		internal static readonly Categoria Azucarados =
			new Comestible("Azucarados", "dulces y postres", "algo dulce", diasDeReabastecimiento: 0);
		internal static readonly Categoria Snacks =
			new Comestible("Snacks", "snacks", "un snack", diasDeReabastecimiento: 0);
		// ENTRAN EN ENERO DE 2026, y por eso existen: el guion de la wiki dice que "embutidos
		// y preparadas entran en ene 2026", y sin departamento propio el cambio se
		// escondia dentro de proteinas. La nutricionista de Sonia le pide "cambiar
		// embutidos por proteina fresca": son dos cosas distintas para quien la aconseja,
		// asi que son dos departamentos.
		//
		// NINGUNO DE LOS DOS SE REABASTECE. Es la misma regla que los azucarados: la
		// periodicidad existe para lo que se ACABA y hay que reponer, no para lo que
		// alguien puede haber dejado de comprar a proposito. Decirle "hace un mes que no
		// compra embutidos" seria justo el reclamo que este modulo no hace.
		internal static readonly Categoria Embutidos =
			new Comestible("Embutidos", "embutidos", "un embutido", diasDeReabastecimiento: 0);
		internal static readonly Categoria ComidasPreparadas =
			new Comestible("ComidasPreparadas", "comidas preparadas", "una comida preparada", diasDeReabastecimiento: 0);

		// LA COMIDA DE LA PERRA NO ES COMESTIBLE, y la palabra engaña: EsComestible
		// significa "comida de la persona", que es lo que sostiene el "de eso, comida" de
		// la canasta. La comida de Luna es un gasto del hogar, no de la dieta de nadie.
		//
		// Y SI SE REABASTECE: Luna come todos los meses -₡25.000 en el carrito, dice el
		// invariante 14- asi que un hueco aca es un hueco de verdad. Es lo contrario de los
		// azucarados: nadie deja de comprarle comida al perro a proposito.
		internal static readonly Categoria Mascotas =
			new NoComestible("Mascotas", "mascotas", "comida para la mascota", diasDeReabastecimiento: 30);

		internal static readonly Categoria Limpieza =
			new NoComestible("Limpieza", "limpieza", "algo de limpieza", diasDeReabastecimiento: 45);
		internal static readonly Categoria Electrohogar =
			new NoComestible("Electrohogar", "electrodomésticos", "un electrodoméstico", diasDeReabastecimiento: 0);

		private readonly int diasDeReabastecimiento;

		/// <summary>La LLAVE del departamento, la que viaja en el evento. No se le muestra a nadie.</summary>
		internal string Nombre { get; private set; }

		/// <summary>El nombre del departamento como lo lee una persona: "frutas y verduras".</summary>
		internal string Titulo { get; private set; }

		/// <summary>Como se menciona UNO de sus productos dentro de una frase: "una fruta o verdura".</summary>
		internal string Mencion { get; private set; }

		/// <summary>Si el departamento es de comida. Es donde esta la gondola, no un juicio.</summary>
		internal abstract bool EsComestible { get; }

		/// <summary>Consulta de presencia, total: si este departamento espera reponerse cada tanto.</summary>
		internal bool SeReabastece => diasDeReabastecimiento > 0;

		/// <summary>Cada cuantos dias se repone normalmente. Preguntar SeReabastece primero.</summary>
		internal int DiasDeReabastecimiento
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
		internal static Categoria Desde(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			foreach (Categoria categoria in Todas)
				if (string.Equals(categoria.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
					return categoria;

			throw new ArgumentException($"No existe el departamento '{nombre}'.", nameof(nombre));
		}

		/// <summary>Reconocimiento total: la pareja de Desde, para poder preguntar sin reventar.</summary>
		internal static bool Existe(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;

			foreach (Categoria categoria in Todas)
				if (string.Equals(categoria.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
					return true;

			return false;
		}

		/// <summary>
		/// Reconocimiento total sobre la LISTA de una compra: true solo si el departamento de
		/// cada linea existe.
		/// </summary>
		///
		/// Existe para que el Check de ComprasAPI pueda preguntarlo DESDE EL SCRIPT. La
		/// frontera recibe las categorias como un `string[]` del wire y no puede recorrerlo
		/// preguntando en C# sin nombrar esta clase -- y nombrarla obligaria al dominio
		/// entero a ser internal. Con esto el nombre viaja adentro del texto del script, que el
		/// motor resuelve por reflexion, y el reconocimiento pasa DENTRO del actor.
		///
		/// Sigue valiendo lo de Existe: el conjunto lo posee el dominio. Una lista espejo
		/// mantenida a mano en la frontera ya se desincronizo una vez y descarto 47 de 87
		/// compras en silencio.
		internal static bool TodasExisten(string[] nombres)
		{
			if (nombres == null) return false;

			foreach (string nombre in nombres)
				if (!Existe(nombre)) return false;

			return true;
		}

		internal static readonly Categoria[] Todas =
		{
			Proteinas, FrutasYVerduras, LacteosYHuevos, GranosYAbarrotes,
			AceitesYGrasas, Azucarados, Snacks, Embutidos, ComidasPreparadas,
			Mascotas, Limpieza, Electrohogar,
		};

		private sealed class Comestible : Categoria
		{
			internal Comestible(string nombre, string titulo, string mencion, int diasDeReabastecimiento)
				: base(nombre, titulo, mencion, diasDeReabastecimiento) { }

			internal override bool EsComestible => true;
		}

		private sealed class NoComestible : Categoria
		{
			internal NoComestible(string nombre, string titulo, string mencion, int diasDeReabastecimiento)
				: base(nombre, titulo, mencion, diasDeReabastecimiento) { }

			internal override bool EsComestible => false;
		}
	}
}
