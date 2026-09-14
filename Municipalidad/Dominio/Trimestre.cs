using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Un trimestre: el periodo de bienes inmuebles y parques. Termina el ultimo dia de su tercer mes, y
	/// hay cuatro por anio.
	/// <para>
	/// El mismo numero que en un mes es abril, aca es el cuarto trimestre. Por eso el numero entra ya
	/// separado del anio y quien lo lee es la cadencia: el dominio no adivina.
	/// </para>
	/// </summary>
	internal sealed class Trimestre : Periodo
	{
		internal Trimestre(int anio, int trimestre) : base(anio, trimestre)
		{
			if (trimestre < 1 || trimestre > 4)
				throw new ArgumentOutOfRangeException(nameof(trimestre), trimestre, "Un trimestre va de 1 a 4.");
		}

		internal int UltimoMes { get { return Numero * 3; } }

		internal override DateTime UltimoDia
		{
			get { return new DateTime(Anio, UltimoMes, DateTime.DaysInMonth(Anio, UltimoMes)); }
		}

		internal override Periodo Siguiente()
		{
			return Numero == 4 ? new Trimestre(Anio + 1, 1) : new Trimestre(Anio, Numero + 1);
		}

		internal override Periodo Anterior()
		{
			return Numero == 1 ? new Trimestre(Anio - 1, 4) : new Trimestre(Anio, Numero - 1);
		}

		/// <summary>
		/// El periodo dicho como lo escribe el recibo. La cultura es invariante y no la de la maquina:
		/// este dominio no lee ninguna, y un formato de numero ajeno escribiria otra etiqueta para el
		/// mismo trimestre.
		/// </summary>
		internal override string Etiqueta
		{
			get
			{
				return Anio.ToString(System.Globalization.CultureInfo.InvariantCulture)
					+ "-T" + Numero.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
		}
	}
}
