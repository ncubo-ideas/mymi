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
		/// <summary>Un slide del carrusel, con los mismos campos que el HeroBanner de la app.</summary>
		public sealed class Banner
		{
			public string Variante { get; init; } = "";
			public string Kicker { get; init; } = "";
			public string Titulo { get; init; } = "";
			public string Secundario { get; init; } = "";
			public decimal? PrecioAntes { get; init; }
			public decimal? PrecioAhora { get; init; }
			public string Cta { get; init; } = "";
			public string Foto { get; init; } = "";
		}

		private static readonly string[] Meses =
		{
			"enero", "febrero", "marzo", "abril", "mayo", "junio",
			"julio", "agosto", "setiembre", "octubre", "noviembre", "diciembre",
		};

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
					Kicker = "NUEVO",
					Titulo = $"Llegó algo nuevo de tus {Corto(novedad.Descripcion).ToLowerInvariant()}",
					Cta = "Verlo",
					Foto = Fotos.De(novedad.Sku),
				});

			PrecioQueCambio oportunidad = Oportunidad(precios);
			if (oportunidad is not null)
				banners.Add(new Banner
				{
					Variante = "habitual",
					Kicker = "BAJÓ DE PRECIO",
					Titulo = $"El {Corto(oportunidad.Descripcion).ToLowerInvariant()} que comprás siempre bajó de precio",
					PrecioAntes = oportunidad.Antes,
					PrecioAhora = oportunidad.Ahora,
					Cta = "Verlo",
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
					Kicker = "PUEDE INTERESARTE",
					Titulo = promo.Gancho,
					Secundario = $"{promo.Detalle} · {Vigencia(promo)}",
					Foto = Fotos.De(promo.Sku),
				});

			if (banners.Count == 0)
				banners.Add(hayCompras
					? new Banner
					{
						Variante = "neutro",
						Titulo = "Así vienen tus compras",
						Secundario = ritmos is not null && ritmos.Count > 0
							? $"Casi todo habitual · {ritmos.Count} {(ritmos.Count == 1 ? "cambio" : "cambios")} de ritmo"
							: "Todo como siempre por acá.",
					}
					: new Banner
					{
						Variante = "neutro",
						Titulo = "Bienvenida a La Guaria",
						Secundario = "Descubrí lo que siempre vuelve y cómo cambia tu carrito.",
					});

			return banners;
		}

		/// <summary>«enero de 2025» desde el primer mes del rango, para «87 compras tuyas desde…».</summary>
		public static string DesdeCuando(RangoDelHistorialVista rango)
		{
			if (rango is null || rango.PrimerMes.Length < 7) return "";

			string[] partes = rango.PrimerMes.Split('-');
			if (partes.Length < 2 || !int.TryParse(partes[1], out int mes) || mes < 1 || mes > 12) return "";

			return $"{Meses[mes - 1]} de {partes[0]}";
		}

		/// <summary>«enero» desde el aaaa-mm del corte, para «cambió desde enero».</summary>
		public static string MesLegible(string aaaaMm)
		{
			if (aaaaMm is null) return "";

			string[] partes = aaaaMm.Split('-');
			if (partes.Length < 2 || !int.TryParse(partes[1], out int mes) || mes < 1 || mes > 12) return aaaaMm;

			return Meses[mes - 1];
		}

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

		private static string Vigencia(PromocionVista promo)
		{
			if (promo.DiasQueQuedan == 0) return "último día";

			if (DateTime.TryParse(promo.ValidaHasta, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime hasta))
				return $"válida hasta el {DiaCorto(hasta)} {hasta.Day}";

			return $"quedan {promo.DiasQueQuedan} días";
		}

		private static string DiaCorto(DateTime cuando) => cuando.DayOfWeek switch
		{
			DayOfWeek.Monday => "lunes",
			DayOfWeek.Tuesday => "martes",
			DayOfWeek.Wednesday => "miércoles",
			DayOfWeek.Thursday => "jueves",
			DayOfWeek.Friday => "viernes",
			DayOfWeek.Saturday => "sábado",
			_ => "domingo",
		};
	}
}
