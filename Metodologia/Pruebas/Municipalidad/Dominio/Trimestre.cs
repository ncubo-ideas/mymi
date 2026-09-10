using System;

namespace Municipalidad.Dominio
{
	// UN TRIMESTRE: el periodo de bienes inmuebles y parques.
	//
	// Lo propio: termina el ultimo dia del tercer mes del trimestre -marzo, junio,
	// setiembre, diciembre-, y hay cuatro por anio.
	//
	// El mismo "4" que en un Mes es abril, aca es el cuarto trimestre. Por eso el numero
	// entra ya separado del anio y lo lee la cadencia: el dominio no adivina.
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

		internal override string Etiqueta { get { return Anio + "-T" + Numero; } }
	}
}
