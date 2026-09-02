using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Una promoción que el súper publicó: el gancho, el detalle, hasta cuándo vale, y
	/// opcionalmente el producto al que apunta.
	/// </summary>
	// LLEGA COMO EVENTO POR URL2, igual que las compras, y esa fue la decision de diseño
	// que el equipo aprobo sobre la maqueta: las promociones son COPIA DE ELLA en su
	// journal. Funcionan sin conexion, la app puede decir "vence el viernes" porque la
	// fecha vino en el evento, y el super NUNCA se entera de cuales vio — un banner que
	// llama a un servidor en vivo seria la institucion empujando contenido y eligiendo
	// promos con los datos de la persona, que es lo contrario de este producto.
	public class Promocion
	{
		public string Gancho { get; private set; }
		public string Detalle { get; private set; }

		/// <summary>El producto al que apunta, o vacío si es general (una rifa, un pasillo).</summary>
		public string Sku { get; private set; }

		public DateTime ValidaHasta { get; private set; }

		public Promocion(string gancho, string detalle, string sku, DateTime validaHasta)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(gancho);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(detalle);
			ArgumentNullException.ThrowIfNull(sku);

			Gancho = gancho;
			Detalle = detalle;
			Sku = sku.Trim();
			ValidaHasta = validaHasta;
		}

		/// <summary>Como aaaa-mm-dd, que es la forma en que la fecha viaja.</summary>
		public string ValidaHastaExpresada => ValidaHasta.ToString("yyyy-MM-dd");

		/// <summary>El día del vencimiento INCLUIDO: "válida hasta el viernes" incluye el viernes.</summary>
		public bool VigenteEl(DateTime fecha) => fecha.Date <= ValidaHasta.Date;

		/// <summary>Cuántos días le quedan mirándola ese día. Cero el último día; negativo, vencida.</summary>
		public int DiasQueQuedanEl(DateTime fecha) => (int)(ValidaHasta.Date - fecha.Date).TotalDays;

		public string ParaElAsistente =>
			$"{Gancho}: {Detalle}, válida hasta el {ValidaHastaExpresada}";
	}
}
