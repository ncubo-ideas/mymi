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
	internal class CambioEnLaCanasta
	{
		private readonly List<ProductoQueEntro> entraron = new List<ProductoQueEntro>();
		private readonly List<ProductoHabitual> salieron = new List<ProductoHabitual>();
		private readonly List<ProductoHabitual> siguen = new List<ProductoHabitual>();

		internal FirmaDeLaCanasta FirmaAntes { get; private set; }
		internal FirmaDeLaCanasta FirmaDespues { get; private set; }

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
		internal DateTime Corte
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
		internal string CorteExpresado =>
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

		internal IReadOnlyList<ProductoQueEntro> Entraron => entraron;
		internal IReadOnlyList<ProductoHabitual> Salieron => salieron;
		internal IReadOnlyList<ProductoHabitual> Siguen => siguen;

		/// <summary>
		/// Si los DOS lados tienen una firma con la que comparar. Sin eso no se afirma
		/// nada: que de un lado no haya compras no es que la canasta cambió.
		/// </summary>
		internal bool HayConQueComparar => FirmaAntes.Existe() && FirmaDespues.Existe();

		/// <summary>Si hubo algún cambio de fondo: algo entró o algo se cayó.</summary>
		internal bool HayCambio => HayConQueComparar && (entraron.Count > 0 || salieron.Count > 0);

		/// <summary>
		/// El mes desde el que se nota el cambio: el más viejo entre los productos que
		/// entraron. Preguntar HayCambio primero.
		/// </summary>
		internal string MesDelCambioExpresado
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

		/// <summary>
		/// QUE FRASE dice la portada, por su nombre: `sin-comparar`, `sin-cambio` o `hay-cambio`. Los
		/// nombres que van en sus huecos son los de Entraron y Salieron, que ya se publican; el mes del
		/// cambio es el del primero que entro, que ya viene ordenado por fecha.
		/// </summary>
		internal string Clave =>
			!HayConQueComparar ? "sin-comparar" : !HayCambio ? "sin-cambio" : "hay-cambio";
	}

	/// <summary>Un producto que se volvió habitual, con el mes en que apareció por primera vez.</summary>
	internal class ProductoQueEntro
	{
		private readonly ProductoHabitual habitual;

		internal DateTime Desde { get; private set; }

		internal ProductoQueEntro(ProductoHabitual habitual, DateTime desde)
		{
			ArgumentNullException.ThrowIfNull(habitual);

			this.habitual = habitual;
			Desde = desde;
		}

		internal string Sku => habitual.Sku;
		internal string Descripcion => habitual.Descripcion;
		internal int ComprasEnQueAparece => habitual.ComprasEnQueAparece;
		internal decimal Proporcion => habitual.Proporcion;

		internal string DesdeExpresado => $"{Desde.Year:0000}-{Desde.Month:00}";
	}
}
