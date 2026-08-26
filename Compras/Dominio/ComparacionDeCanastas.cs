using System;
using System.Globalization;

namespace Compras.Dominio
{
	/// <summary>
	/// Como cambio la canasta entre dos meses: las dos proporciones de gasto ocasional,
	/// el cambio con signo y — lo que ninguna pantalla puede decidir — que SIGNIFICA ese
	/// cambio. Total: comparar contra un mes sin comestibles no revienta.
	/// </summary>
	public class ComparacionDeCanastas
	{
		/// <summary>
		/// Cuanto tiene que moverse la proporcion para llamarlo un cambio real, en
		/// cualquiera de las dos direcciones. Cinco puntos es ruido de despensa.
		/// </summary>
		public const decimal CambioQueCuentaComoReal = 0.05m;

		public Canasta Antes { get; private set; }
		public Canasta Despues { get; private set; }

		public ComparacionDeCanastas(Canasta antes, Canasta despues)
		{
			ArgumentNullException.ThrowIfNull(antes);
			ArgumentNullException.ThrowIfNull(despues);

			Antes = antes;
			Despues = despues;
		}

		/// <summary>Solo hay comparacion si LOS DOS meses tienen comestibles; si no, se inventaria una lectura.</summary>
		public bool HayComparacion => Antes.HayComestible && Despues.HayComestible;

		public decimal ProporcionOcasionalAntes => Antes.ProporcionOcasional;

		public decimal ProporcionOcasionalDespues => Despues.ProporcionOcasional;

		/// <summary>El HECHO, con signo: positivo si lo ocasional subio.</summary>
		public decimal CambioEnProporcion =>
			HayComparacion ? ProporcionOcasionalDespues - ProporcionOcasionalAntes : 0m;

		/// <summary>
		/// QUE SIGNIFICA el cambio. En lo ocasional la direccion deseable es bajar, asi
		/// que un cambio negativo es Mejorando — y eso es lo que un booleano
		/// "crecio o no crecio" no podia decir.
		/// </summary>
		public Veredicto Veredicto
		{
			get
			{
				if (!HayComparacion) return Veredicto.Indeterminado;

				decimal cambio = CambioEnProporcion;
				if (Math.Abs(cambio) < CambioQueCuentaComoReal) return Veredicto.Estable;
				return cambio > 0m ? Veredicto.Empeorando : Veredicto.Mejorando;
			}
		}

		public bool CrecioLoOcasional => Veredicto.EsEmpeorando;

		public string NombreDelVeredicto => Veredicto.Nombre;

		/// <summary>
		/// La lectura en una linea, escrita por el dominio con SUS hechos. No es un
		/// consejo: este modulo reporta como se movio la canasta, y que hacer con eso no
		/// lo decide un supermercado.
		/// </summary>
		public string ParaSonia
		{
			get
			{
				if (!HayComparacion)
					return "todavía no hay dos meses con compras de comida para comparar";

				string antes = Porcentaje(ProporcionOcasionalAntes);
				string despues = Porcentaje(ProporcionOcasionalDespues);
				string puntos = Puntos(Math.Abs(CambioEnProporcion));

				if (Veredicto.EsEmpeorando)
					return $"lo ocasional pasó de {antes} a {despues} de tu canasta de comida: subió {puntos}";
				if (Veredicto.EsMejorando)
					return $"lo ocasional pasó de {antes} a {despues} de tu canasta de comida: bajó {puntos}";

				return $"lo ocasional se mantuvo en torno a {despues} de tu canasta de comida";
			}
		}

		private static string Porcentaje(decimal proporcion) =>
			(proporcion * 100m).ToString("0.#", CultureInfo.InvariantCulture) + "%";

		private static string Puntos(decimal proporcion) =>
			(proporcion * 100m).ToString("0.#", CultureInfo.InvariantCulture) + " puntos";
	}
}
