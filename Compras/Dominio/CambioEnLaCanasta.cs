using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// QUÉ CAMBIÓ entre dos períodos: los productos que ENTRARON a la canasta y se
	/// quedaron, y los que DEJARON de estar. "Algo cambió en enero."
	/// </summary>
	// Es la otra lectura con la que la app abre. Y se resuelve sin estadistica: no se
	// declara un quiebre porque un promedio se movio, se declara porque HAY PRODUCTOS
	// NUEVOS QUE SE VOLVIERON HABITUALES y otros que se cayeron. Cada uno con el mes en
	// que aparecio por primera vez, que es lo que le pone fecha al cambio.
	//
	// NO JUZGA, y esto es explicito en la especificacion de marca: "que el helado pasara de
	// mensual a semanal es un dato, no una falta. La app lo senala y se calla; el juicio no
	// es suyo." Por eso aca no hay veredicto: hay lo que entro, lo que salio y lo que se
	// mantuvo.
	public class CambioEnLaCanasta
	{
		private readonly List<ProductoQueEntro> entraron = new List<ProductoQueEntro>();
		private readonly List<ProductoHabitual> salieron = new List<ProductoHabitual>();
		private readonly List<ProductoHabitual> siguen = new List<ProductoHabitual>();

		public FirmaDeLaCanasta FirmaAntes { get; private set; }
		public FirmaDeLaCanasta FirmaDespues { get; private set; }

		private readonly DateTime corte;

		/// <summary>
		/// La fecha por la que se partió el historial para comparar. Preguntar
		/// HayConQueComparar primero.
		/// </summary>
		// NO ES LO MISMO QUE MesDelCambioExpresado, y conviene no confundirlos: el CORTE es
		// donde se partio la historia -lo elige quien pregunta, o lo encuentra el dominio-;
		// el MES DEL CAMBIO es el mes mas viejo entre los productos que entraron, o sea un
		// hecho de los datos. Suelen caer juntos o pegados, y cuando no, la diferencia es
		// informacion: quiere decir que lo nuevo tardo en aparecer despues del corte.
		public DateTime Corte
		{
			get
			{
				if (!HayConQueComparar)
					throw new InvalidOperationException(
						"No hay dos lados que comparar, asi que no hubo corte que valga. "
						+ "Preguntar HayConQueComparar primero.");

				return corte;
			}
		}

		/// <summary>El corte como aaaa-mm. Vacío cuando no hay con qué comparar: lectura total para el cable.</summary>
		public string CorteExpresado =>
			HayConQueComparar ? $"{corte.Year:0000}-{corte.Month:00}" : "";

		internal CambioEnLaCanasta(FirmaDeLaCanasta antes, FirmaDeLaCanasta despues,
			IReadOnlyDictionary<string, DateTime> primeraVezPorSku, DateTime corte)
		{
			ArgumentNullException.ThrowIfNull(antes);
			ArgumentNullException.ThrowIfNull(despues);
			ArgumentNullException.ThrowIfNull(primeraVezPorSku);

			FirmaAntes = antes;
			FirmaDespues = despues;
			this.corte = corte;

			var eranHabituales = new Dictionary<string, ProductoHabitual>(StringComparer.OrdinalIgnoreCase);
			foreach (ProductoHabitual habitual in antes.Habituales) eranHabituales[habitual.Sku] = habitual;

			var sonHabituales = new Dictionary<string, ProductoHabitual>(StringComparer.OrdinalIgnoreCase);
			foreach (ProductoHabitual habitual in despues.Habituales) sonHabituales[habitual.Sku] = habitual;

			foreach (ProductoHabitual ahora in despues.Habituales)
			{
				if (eranHabituales.ContainsKey(ahora.Sku)) { siguen.Add(ahora); continue; }

				// El mes en que se lo vio por primera vez: es lo que le pone fecha al
				// cambio. Sin eso, "entraron cosas nuevas" no dice cuando.
				DateTime desde = primeraVezPorSku.TryGetValue(ahora.Sku, out DateTime primera)
					? primera
					: ahora.UltimaVez;

				entraron.Add(new ProductoQueEntro(ahora, desde));
			}

			foreach (ProductoHabitual antesHabitual in antes.Habituales)
				if (!sonHabituales.ContainsKey(antesHabitual.Sku)) salieron.Add(antesHabitual);

			entraron.Sort((uno, otro) => uno.Desde.CompareTo(otro.Desde));
		}

		public IReadOnlyList<ProductoQueEntro> Entraron => entraron;
		public IReadOnlyList<ProductoHabitual> Salieron => salieron;
		public IReadOnlyList<ProductoHabitual> Siguen => siguen;

		/// <summary>
		/// Si los DOS lados tienen una firma con la que comparar. Sin eso no se afirma
		/// nada: que de un lado no haya compras no es que la canasta cambió.
		/// </summary>
		public bool HayConQueComparar => FirmaAntes.Existe() && FirmaDespues.Existe();

		/// <summary>Si hubo algún cambio de fondo: algo entró o algo se cayó.</summary>
		public bool HayCambio => HayConQueComparar && (entraron.Count > 0 || salieron.Count > 0);

		/// <summary>
		/// El mes desde el que se nota el cambio: el más viejo entre los productos que
		/// entraron. Preguntar HayCambio primero.
		/// </summary>
		public string MesDelCambioExpresado
		{
			get
			{
				if (!HayCambio || entraron.Count == 0)
					throw new InvalidOperationException(
						"No entro ningun producto nuevo, asi que no hay un mes de cambio que dar. "
						+ "Preguntar HayCambio y mirar los que salieron.");

				DateTime desde = entraron[0].Desde;
				return $"{desde.Year:0000}-{desde.Month:00}";
			}
		}

		/// <summary>La línea de la portada. Dice qué pasó y se calla: el juicio no es del súper.</summary>
		public string ParaElAsistente
		{
			get
			{
				if (!HayConQueComparar)
					return "todavía no hay compras de los dos lados con las que ver si algo cambió";

				if (!HayCambio) return "su canasta viene igual: no entró ni se cayó nada de lo habitual";

				var partes = new List<string>();

				if (entraron.Count > 0)
				{
					partes.Add($"desde {MesDelCambioExpresado} entraron {Nombres(Descripciones(entraron))}");
				}

				if (salieron.Count > 0)
				{
					var nombres = new List<string>();
					foreach (ProductoHabitual quedo in salieron) nombres.Add(quedo.Descripcion);
					partes.Add($"dejó de aparecer {Nombres(nombres)}");
				}

				return string.Join(", y ", partes);
			}
		}

		private static List<string> Descripciones(IReadOnlyList<ProductoQueEntro> productos)
		{
			var nombres = new List<string>();
			foreach (ProductoQueEntro entro in productos) nombres.Add(entro.Descripcion);
			return nombres;
		}

		private static string Nombres(List<string> nombres)
		{
			int cuantos = Math.Min(4, nombres.Count);
			var recortados = nombres.GetRange(0, cuantos);

			string lista = cuantos == 1
				? recortados[0]
				: string.Join(", ", recortados.GetRange(0, cuantos - 1)) + " y " + recortados[cuantos - 1];

			return nombres.Count > cuantos ? $"{lista} (y {nombres.Count - cuantos} más)" : lista;
		}
	}

	/// <summary>Un producto que se volvió habitual, con el mes en que apareció por primera vez.</summary>
	public class ProductoQueEntro
	{
		private readonly ProductoHabitual habitual;

		public DateTime Desde { get; private set; }

		internal ProductoQueEntro(ProductoHabitual habitual, DateTime desde)
		{
			ArgumentNullException.ThrowIfNull(habitual);

			this.habitual = habitual;
			Desde = desde;
		}

		public string Sku => habitual.Sku;
		public string Descripcion => habitual.Descripcion;
		public int ComprasEnQueAparece => habitual.ComprasEnQueAparece;
		public decimal Proporcion => habitual.Proporcion;

		public string DesdeExpresado => $"{Desde.Year:0000}-{Desde.Month:00}";
	}
}
