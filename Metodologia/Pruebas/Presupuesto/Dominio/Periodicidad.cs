using System;

namespace Presupuesto.Dominio
{
	// CADA CUANTO SE ESPERA QUE VUELVA. Conjunto CERRADO, comparado por referencia.
	//
	// Juicio 3 de Preguntas.md: mensual, trimestral o anual. Parte la declara la persona y
	// parte se deduce de la historia -de eso se encarga quien construye el gasto, no esta
	// clase-.
	//
	// De aca sale la pregunta 4, la de la AUSENCIA. Si la periodicidad quedara vaga, la app
	// no podria detectar lo que falta: es la unica pregunta del archivo que ninguna pantalla
	// puede contestar, y depende enteramente de esto.
	//
	// NO ES UN ENUM NI UN int DE MESES. Un entero admitiria 7 meses, 0 meses y -3 meses, y
	// obligaria a validar en cada uso. Ademas, "sabe avanzar una fecha" es comportamiento
	// que pertenece a la periodicidad y no a quien la consulta.
	public sealed class Periodicidad
	{
		public static readonly Periodicidad Mensual = new Periodicidad("mensual", 1);
		public static readonly Periodicidad Trimestral = new Periodicidad("trimestral", 3);
		public static readonly Periodicidad Anual = new Periodicidad("anual", 12);

		private Periodicidad(string nombre, int meses)
		{
			Nombre = nombre;
			Meses = meses;
		}

		public string Nombre { get; }

		public int Meses { get; }

		// AddMonths ya resuelve el caso feo: un cargo del 31 de enero cae al 28 o 29 de
		// febrero en vez de reventar o saltarse a marzo.
		public DateTime ProximoDespuesDe(DateTime ultimo) => ultimo.AddMonths(Meses);

		// Cuantas veces aparece en un ano.
		//
		// HUECO 2 - Preguntas.md no dice como entra al total mensual lo que NO es mensual, y
		// eso ROMPE el juicio 2. Si un seguro anual cae entero en marzo, el total de marzo se
		// dispara muy por encima del 10% y la app grita sin que haya pasado nada: es el pago
		// de siempre, en su mes de siempre. O se prorratea -y entonces el "total mensual" no
		// es lo que se gasto, es lo que se devengo- o el umbral tiene que saber distinguir.
		// El archivo no lo decidio. Esta propiedad HABILITA el prorrateo; quien decide es
		// PresupuestoMensual, y ahi esta marcado.
		public decimal VecesPorAno => 12m / Meses;

		public override string ToString() => Nombre;
	}
}
