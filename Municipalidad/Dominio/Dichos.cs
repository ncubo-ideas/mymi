using System;
using System.Globalization;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Las formas en que este dominio dice una cifra o un dia DENTRO de una frase que una persona lee,
	/// en un solo lugar.
	/// <para>
	/// Existe para que dos portadores no digan la misma cantidad de dos maneras: eso seria la frase
	/// rival que tener una sola autora existe para evitar, en pequeno. Ninguna de las tres formas
	/// depende de la cultura de la maquina.
	/// </para>
	/// </summary>
	internal static class Dichos
	{
		/// <summary>
		/// Un monto, con los mismos digitos que lleva su columna tipada y SIN simbolo de moneda: este
		/// dominio no tiene un tipo de moneda y no inventa uno adentro de una frase.
		/// </summary>
		internal static string Monto(decimal monto)
		{
			return ConComaDecimal(monto.ToString("0.##", CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Una proporcion dicha como porcentaje, con una decimal a lo sumo: 0,8 es "80 %". El signo
		/// viaja tal cual, asi que quien quiera decir la direccion en palabras pasa la magnitud.
		/// </summary>
		internal static string Porcentaje(decimal proporcion)
		{
			return ConComaDecimal(
				Math.Round(proporcion * 100m, 1).ToString("0.##", CultureInfo.InvariantCulture)) + " %";
		}

		/// <summary>
		/// La coma decimal del espanol, puesta a mano y no por la cultura de la maquina: una frase que
		/// dijera "6.2 %" en una maquina y "6,2 %" en otra seria la misma frase dicha de dos maneras.
		/// Sin separador de miles no queda ninguna ambiguedad que resolver.
		/// </summary>
		private static string ConComaDecimal(string dicho)
		{
			return dicho.Replace('.', ',');
		}

		/// <summary>
		/// Un dia de calendario en anio-mes-dia, sin hora: un periodo y una consulta cierran un dia y
		/// no un instante, y el orden anio-mes-dia no se puede leer al reves.
		/// </summary>
		internal static string Dia(DateTime dia)
		{
			return dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}
}
