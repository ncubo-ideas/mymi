using System;

namespace Presupuesto.Dominio
{
	// QUE TAN URGENTE es un hallazgo. Conjunto CERRADO, comparado por referencia.
	//
	// Sale del juicio 5 de Preguntas.md, y ese juicio dice algo mas fuerte de lo que
	// parece: la severidad la decide LA CLASE DE COMPROMISO, no el monto. Un recibo de
	// luz que no llego es un aviso; una cuota de prestamo es otra cosa, y no porque sean
	// mas plata.
	//
	// Por eso la severidad NO se calcula aca a partir de un numero. Se la pide cada
	// Compromiso, que es quien sabe lo que vale.
	//
	// QUE HACER sale de aca y no lo escribe cada compromiso. Es la misma garantia que usa
	// Examenes: con cuatro textos fijos, ningun compromiso puede inventarse una accion
	// propia. Si manana hay que decir otra cosa, se cambia en un solo lugar.
	internal sealed class Severidad
	{
		internal static readonly Severidad Informativo =
			new Severidad("informativo", 0, "");

		internal static readonly Severidad Seguimiento =
			new Severidad("seguimiento", 1, "Conviene revisarlo cuando puedas.");

		internal static readonly Severidad Atencion =
			new Severidad("atencion", 2, "Revisalo esta semana.");

		// El unico nivel que nombra una consecuencia, porque es el unico que la tiene:
		// un pago comprometido que no salio no es un dato que falta, es plata que se va
		// a perder en intereses o en mora.
		internal static readonly Severidad Critico =
			new Severidad("critico", 3, "Verificalo hoy: si el pago no salio, corre interes o mora.");

		private Severidad(string nombre, int orden, string queHacer)
		{
			Nombre = nombre;
			Orden = orden;
			QueHacer = queHacer;
		}

		internal string Nombre { get; }

		// Para ordenar lo mas grave primero. No es "cuanta plata": es cuanto corre.
		internal int Orden { get; }

		internal string QueHacer { get; }

		internal bool PideAlgo => QueHacer.Length > 0;

		internal bool EsAlMenos(Severidad otra)
		{
			if (otra == null) throw new ArgumentNullException(nameof(otra));
			return Orden >= otra.Orden;
		}

		public override string ToString() => Nombre;
	}
}
