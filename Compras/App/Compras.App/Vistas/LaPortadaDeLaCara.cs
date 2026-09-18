using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Compras.App.Vistas
{
	/// <summary>
	/// LO QUE ABRE LA PANTALLA DE INICIO: el carrusel de senales, la linea de estado y el mes.
	/// <para>
	/// ES UN PUERTO, NO UN DISENO NUEVO. Cada regla de aca sale de <c>MisCompras.razor</c> --
	/// misma condicion, mismo texto, misma foto -- y las dos tienen que moverse juntas. Se escribio
	/// como clase del dominio y NO en el javascript de la cara porque las tres decisiones que toma
	/// son del producto, no de la pintura:
	/// </para>
	/// <list type="bullet">
	/// <item>una NOVEDAD solo cuenta si el quiebre es de los ultimos tres meses -- si no, la
	/// pantalla seguiria diciendo «llego algo nuevo» ocho meses despues;</item>
	/// <item>una PROMO solo cuenta si su sku esta entre lo que ella compra de verdad -- «no forzar
	/// promociones» fue instruccion explicita;</item>
	/// <item>una OPORTUNIDAD es la primera bajada despues de la primera fila, que es la que el
	/// dominio ya ordeno.</item>
	/// </list>
	/// <para>
	/// Dejarlas en la cara habria puesto reglas de producto del lado que se puede reemplazar sin
	/// tocar el dominio, y habria hecho que la app suelta y la cara hibrida pudieran contestar
	/// distinto sobre los mismos hechos.
	/// </para>
	/// <para>
	/// LA FOTO VIAJA RESUELTA, como ruta: <c>Fotos</c> vive al lado y la resuelve una sola vez para
	/// las dos caras. La cara no lleva su propia tabla de sku a imagen.
	/// </para>
	/// </summary>
	public static class LaPortadaDeLaCara
	{
		/// <summary>
		/// Un slide del carrusel. Trae los INGREDIENTES, no la frase.
		/// <para>
		/// Hasta el 2026-09-18 traia <c>Kicker</c>, <c>Titulo</c>, <c>Secundario</c> y <c>Cta</c> ya
		/// escritos, en espanol duro: «NUEVO», «Llegó algo nuevo de tus …», «último día». Una cara
		/// en ingles pintaba el carrusel en espanol y el resto de la pantalla en ingles. La app
		/// suelta nunca tuvo el problema porque arma los suyos con su catalogo
		/// (<c>ArmarHeroBanners</c>, claves <c>hero.*</c>), asi que la cara hace lo mismo con las
		/// MISMAS claves y este tipo viaja con lo que no se traduce.
		/// </para>
		/// <para>
		/// <c>Gancho</c> y <c>Detalle</c> SI son texto y no llevan clave a proposito: los escribe la
		/// institucion para esa promocion, como el nombre de un producto. Traducirlos seria
		/// inventarle palabras al supermercado.
		/// </para>
		/// </summary>
		public sealed class Banner
		{
			/// <summary>nuevo | habitual | promo | neutro. Elige la clase y, en la cara, la frase.</summary>
			public string Variante { get; init; } = "";

			/// <summary>El nombre corto del producto, para «… de tus {producto}». Vacio si no aplica.</summary>
			public string Producto { get; init; } = "";

			/// <summary>De la promocion, tal como la escribio la institucion.</summary>
			public string Gancho { get; init; } = "";

			/// <summary>De la promocion, tal como la escribio la institucion.</summary>
			public string Detalle { get; init; } = "";

			/// <summary>Como aaaa-mm-dd. La cara arma «válida hasta el viernes 25» con su catalogo.</summary>
			public string ValidaHasta { get; init; } = "";

			/// <summary>Cero el ultimo dia, igual que en la promocion.</summary>
			public int DiasQueQuedan { get; init; }

			/// <summary>Solo el neutro: cuantos ritmos cambiaron, para elegir singular o plural.</summary>
			public int CuantosRitmos { get; init; }

			/// <summary>Solo el neutro: separa «así vienen tus compras» de la bienvenida.</summary>
			public bool HayCompras { get; init; }

			public decimal? PrecioAntes { get; init; }
			public decimal? PrecioAhora { get; init; }
			public string Foto { get; init; } = "";
		}

		/// <summary>
		/// Los banners vigentes, en el orden en que la app los pone: novedad, oportunidad, promo.
		/// Nunca vacio -- sin ninguna senal cae al mismo respaldo neutro de la app.
		/// </summary>
		public static IReadOnlyList<Banner> Banners(
			CambioDeLaCanasta cambio,
			IReadOnlyList<PrecioQueCambio> precios,
			IReadOnlyList<PromocionVista> promociones,
			FirmaDeLaSemana firma,
			IReadOnlyList<RitmoQueCambio> ritmos,
			bool hayCompras,
			DateTime alDia)
		{
			var banners = new List<Banner>();

			ProductoQueEntroVista novedad = cambio is not null && cambio.Entraron.Count > 0
				&& EsRecienteElMes(cambio.Entraron[0].Desde, alDia)
				? cambio.Entraron[0]
				: null;

			if (novedad is not null)
				banners.Add(new Banner
				{
					Variante = "nuevo",
					Producto = Corto(novedad.Descripcion).ToLowerInvariant(),
					Foto = Fotos.De(novedad.Sku),
				});

			PrecioQueCambio oportunidad = Oportunidad(precios);
			if (oportunidad is not null)
				banners.Add(new Banner
				{
					Variante = "habitual",
					Producto = Corto(oportunidad.Descripcion).ToLowerInvariant(),
					PrecioAntes = oportunidad.Antes,
					PrecioAhora = oportunidad.Ahora,
					Foto = Fotos.De(oportunidad.Sku),
				});

			PromocionVista promo = promociones?.FirstOrDefault(p =>
				p.Sku.Length > 0
				&& ((firma is not null && firma.Habituales.Any(h => h.Sku == p.Sku))
					|| (precios is not null && precios.Any(pr => pr.Sku == p.Sku))));

			if (promo is not null)
				banners.Add(new Banner
				{
					Variante = "promo",
					Gancho = promo.Gancho,
					Detalle = promo.Detalle,
					ValidaHasta = promo.ValidaHasta,
					DiasQueQuedan = promo.DiasQueQuedan,
					Foto = Fotos.De(promo.Sku),
				});

			if (banners.Count == 0)
				banners.Add(new Banner
				{
					Variante = "neutro",
					HayCompras = hayCompras,
					CuantosRitmos = hayCompras && ritmos is not null ? ritmos.Count : 0,
				});

			return banners;
		}

		// AQUI VIVIAN `DesdeCuando` y `MesLegible`, que armaban «enero de 2025» y «enero» para la
		// franja y para «cambió desde …». Se fueron el 2026-09-18 junto con el ultimo llamador:
		// la superficie ahora manda `primerMes` y `desdeMes` CRUDOS, en aaaa-mm, y el mes lo dice
		// la carátula con su catalogo. Componer el mes aca escribia espanol en una respuesta que
		// la persona podia estar leyendo en ingles, y ninguna otra pieza lo pedia -- la app suelta
		// tiene sus propias copias en MisCompras.razor.
		//
		// CON EL CARRUSEL YA CONVERTIDO, ESTE ARCHIVO NO ESCRIBE NI UNA PALABRA PARA LA PERSONA, y
		// esa es la linea que conviene no cruzar de nuevo: acá se decide QUE se muestra y en que
		// orden -- reglas de producto, que las dos caras tienen que compartir -- y el COMO se dice
		// es de quien sabe en que idioma esta mirando. Lo unico que sale de aca como texto son
		// `Gancho` y `Detalle`, que los escribio la institucion.

		// LA OPORTUNIDAD ARRANCA EN 1 Y NO EN 0, igual que la app: la fila cero es la que ya
		// protagoniza la carta de precios, asi que el hero toma la siguiente bajada.
		/// <summary>La bajada que protagoniza el mini-afiche «TU HABITUAL». La misma que el banner.</summary>
		public static PrecioQueCambio Oportunidad(IReadOnlyList<PrecioQueCambio> precios)
		{
			if (precios is null) return null;

			for (int i = 1; i < precios.Count; i++)
				if (!precios[i].Subio) return precios[i];

			return null;
		}

		// EL NOMBRE CORTO PARA EL TITULAR. «Leche semidescremada 1L» no entra; la primera palabra
		// de la descripcion del super es justamente lo que el producto ES, y no hace falta un
		// diccionario aparte que se desincronice con el catalogo.
		private static string Corto(string descripcion)
		{
			int espacio = descripcion.IndexOf(' ');
			return espacio > 2 ? descripcion.Substring(0, espacio) : descripcion;
		}

		// «RECIENTE» es este mes, el pasado o el anterior a ese: tres meses de ventana.
		private static bool EsRecienteElMes(string aaaaMm, DateTime alDia)
		{
			string[] partes = aaaaMm.Split('-');
			if (partes.Length < 2
				|| !int.TryParse(partes[0], out int anio)
				|| !int.TryParse(partes[1], out int mes)) return false;

			int meses = (alDia.Year - anio) * 12 + (alDia.Month - mes);
			return meses >= 0 && meses <= 2;
		}

		// AQUI VIVIAN `Vigencia` y `DiaCorto`, que escribian «último día», «válida hasta el
		// viernes 25» y los siete dias de la semana en espanol. Se fueron con el resto del
		// carrusel el 2026-09-18: el banner de promocion ahora viaja con `validaHasta` en
		// aaaa-mm-dd y `diasQueQuedan`, y la frase la arma la cara con `inicio.ultimoDia`,
		// `inicio.validaHasta` y `comun.dia.*` -- las MISMAS claves con que la app suelta arma
		// esa linea. La regla (cero dias = ultimo dia; sin fecha legible = «quedan N dias») no se
		// perdio: viaja en los dos campos, que es lo que la regla mira.
	}
}
