using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Un mes: el periodo de los servicios. Termina el ultimo dia del mes, y el que sigue puede caer en
	/// otro anio.
	/// </summary>
	internal sealed class Mes : Periodo
	{
		internal Mes(int anio, int mes) : base(anio, mes)
		{
			if (mes < 1 || mes > 12)
				throw new ArgumentOutOfRangeException(nameof(mes), mes, "Un mes va de 1 a 12.");
		}

		internal override DateTime UltimoDia
		{
			get { return new DateTime(Anio, Numero, DateTime.DaysInMonth(Anio, Numero)); }
		}

		internal override Periodo Siguiente()
		{
			return Numero == 12 ? new Mes(Anio + 1, 1) : new Mes(Anio, Numero + 1);
		}

		internal override Periodo Anterior()
		{
			return Numero == 1 ? new Mes(Anio - 1, 12) : new Mes(Anio, Numero - 1);
		}

		/// <summary>
		/// El periodo dicho como lo escribe el recibo. La cultura es invariante y no la de la maquina:
		/// este dominio no lee ninguna, y un formato de numero ajeno escribiria otra etiqueta para el
		/// mismo mes.
		/// </summary>
		internal override string Etiqueta
		{
			get
			{
				return Anio.ToString(System.Globalization.CultureInfo.InvariantCulture)
					+ "-" + Numero.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
			}
		}
	}
}
