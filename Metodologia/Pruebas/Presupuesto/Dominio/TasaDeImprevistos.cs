using System;

namespace Presupuesto.Dominio
{
	// LA RESPUESTA A LA PREGUNTA 5: ".cuanto deberia estar apartando para imprevistos?".
	//
	// "Anticipar imprevistos" suena contradictorio y no lo es: no se puede predecir CUAL,
	// pero si CUANTO POR ANO. Es razonamiento de seguro.
	//
	// LA TASA SE CALCULA SOBRE EVENTOS, NO SOBRE CARGOS, y es lo unico que la hace util.
	// "Tuve 47 cargos no presupuestados este ano" no es informacion; "tuve 3 imprevistos que
	// sumaron 900 mil" si lo es, y de eso sale cuanto apartar por mes.
	//
	// El dominio devuelve el HECHO -cuantos, cuanto, en cuanto tiempo, cuanto por mes- y no
	// dice "aparta esto". Que la persona decida apartar mas o menos que su propio historial
	// es una decision suya, y este dominio no da consejo financiero.
	public sealed class TasaDeImprevistos
	{
		internal TasaDeImprevistos(int cuantos, decimal total, decimal meses)
		{
			Cuantos = cuantos;
			Total = total;
			Meses = meses;
		}

		// EVENTOS, no cargos.
		public int Cuantos { get; }

		public decimal Total { get; }

		public decimal Meses { get; }

		// Con un solo imprevisto en la historia no hay tasa: hay una anecdota. Dos es el
		// minimo para hablar de con que frecuencia pasan las cosas, igual que dos mediciones
		// son el minimo para hablar de tendencia.
		public bool HayTasa => Cuantos >= 2 && Meses > 0;

		public decimal PorMes => HayTasa ? Total / Meses : 0m;

		// Cada cuantos meses ocurre uno. Sirve para decir "te pasa algo asi como cada ocho
		// meses", que es mas facil de creer que un promedio mensual.
		public decimal CadaCuantosMeses => HayTasa ? Meses / Cuantos : 0m;

		public string Lectura
		{
			get
			{
				if (Cuantos == 0)
					return "No hay imprevistos registrados en ese periodo.";

				if (!HayTasa)
					return $"Hay un solo imprevisto registrado, de {Total}. "
						+ "Con uno no se puede hablar de con que frecuencia pasan.";

				return $"{Cuantos} imprevistos en {Meses:F0} meses, por {Total} en total: "
					+ $"uno cada {CadaCuantosMeses:F1} meses, {PorMes:F0} por mes.";
			}
		}
	}
}
