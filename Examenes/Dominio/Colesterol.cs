using System;

namespace Examenes.Dominio
{
	// Colesterol total. El analito del guion: 200 y despues 280.
	//
	// Los dos umbrales son lo que hace que la misma diferencia de 80 mg/dL se lea de
	// dos maneras distintas segun cuanto tiempo paso:
	//
	//   CambioMaximoVerosimilPorDia = 1.0  -> 80 mg/dL en 1 dia son 80/dia, muy por
	//                                         encima de 1.0: el laboratorio se equivoco.
	//                                         80 mg/dL en 365 dias son 0.22/dia: pasa.
	//   CambioQueExigeAtencion      = 50   -> y 80 es mas que 50, asi que ese ascenso
	//                                         real no es ruido.
	public sealed class Colesterol : ExamenDeSangre
	{
		private static readonly Analito Suyo = Examenes.Dominio.Analito.ColesterolTotal;

		public Colesterol(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		public override string Nombre => Suyo.ParaMostrar;

		// El techo de lo deseable baja con la edad: a los 60 el mismo 210 pesa mas que
		// a los 30.
		public override RangoDeReferencia RangoPara(int edad) =>
			RangoDeReferencia.HastaUnMaximoDe(edad >= 50 ? 200m : 220m, Suyo.Unidad);

		public override decimal CambioMaximoVerosimilPorDia => 1.0m;

		public override decimal CambioQueExigeAtencion => 50m;

		public override bool DireccionPreocupanteEsAscenso => true;

		// El colesterol NO se lee por velocidad: se lee por nivel y por el salto entre dos
		// mediciones. Cero es la respuesta correcta, no un pendiente.
		public override decimal VelocidadQueExigeAtencionPorAno => 0m;
	}
}
