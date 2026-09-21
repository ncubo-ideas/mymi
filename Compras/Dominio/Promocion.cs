using System;
using System.Globalization;

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
	internal class Promocion
	{
		internal string Gancho { get; private set; }
		internal string Detalle { get; private set; }

		/// <summary>El producto al que apunta, o vacío si es general (una rifa, un pasillo).</summary>
		internal string Sku { get; private set; }

		internal DateTime ValidaHasta { get; private set; }

		internal Promocion(string gancho, string detalle, string sku, DateTime validaHasta)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(gancho);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(detalle);
			ArgumentNullException.ThrowIfNull(sku);

			Gancho = gancho;
			Detalle = detalle;
			Sku = sku.Trim();
			ValidaHasta = validaHasta;
		}

		/// <summary>
		/// Como aaaa-mm-dd, que es la forma en que la fecha viaja. INVARIANTE y no de la maquina: sin
		/// cultura fija, un calendario no gregoriano escribe otro año en el mismo instante.
		/// </summary>
		internal string ValidaHastaExpresada => ValidaHasta.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		/// <summary>El día del vencimiento INCLUIDO: "válida hasta el viernes" incluye el viernes.</summary>
		internal bool VigenteEl(DateTime fecha) => fecha.Date <= ValidaHasta.Date;

		/// <summary>Cuántos días le quedan mirándola ese día. Cero el último día; negativo, vencida.</summary>
		internal int DiasQueQuedanEl(DateTime fecha) => (int)(ValidaHasta.Date - fecha.Date).TotalDays;

		/// <summary>
		/// QUE FRASE dice una promocion. Es UNA sola: el gancho y el detalle los escribio el super y
		/// no se traducen en ningun idioma; lo unico que pone la redaccion es el marco.
		/// </summary>
		internal string Clave => "promocion";
	}
}
