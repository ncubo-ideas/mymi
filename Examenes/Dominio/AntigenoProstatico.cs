using System;

namespace Examenes.Dominio
{
	// Antigeno prostatico especifico (PSA).
	//
	// ES EL EJEMPLO que justifica todo el dominio, y por eso esta modelado y no dejado
	// como relato. El valor aislado casi no dice nada: un 3.0 puede ser perfectamente
	// normal o puede ser la mitad del camino a un problema, y lo unico que distingue un
	// caso del otro es la SERIE.
	//
	//   3.0 -> 3.1 en dos anos : estable. Nadie hace nada.
	//   3.0 -> 5.2 en un ano   : el mismo paciente, un valor todavia cercano al techo,
	//                            y sin embargo esto exige atencion HOY.
	//
	// El umbral de atencion es 0.75 ng/mL de cambio, que no es un numero inventado para
	// el demo: la velocidad de PSA es un criterio clinico real y ese es su valor de
	// corte habitual por ano. Que sea real importa, porque el demo se defiende ante
	// alguien que sabe de laboratorios.
	//
	// Ninguna pantalla puede contestar "viene subiendo o esta estable". Eso es lo que
	// hace que la tool MCP sobre este analito no sea una repeticion de la UI.
	public sealed class AntigenoProstatico : ExamenDeSangre
	{
		// Se nombra el analito UNA vez y con el namespace por delante. Dentro de esta
		// clase el identificador `Analito` es tambien la propiedad heredada, y calificarlo
		// deja sin lugar a dudas que aca se habla del tipo. Las otras tres subclases hacen
		// lo mismo por la misma razon.
		private static readonly Analito Suyo = Examenes.Dominio.Analito.AntigenoProstatico;

		public AntigenoProstatico(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		public override string Nombre => Suyo.ParaMostrar;

		// El techo sube con la edad: la prostata crece, y 4.0 a los 70 no significa lo
		// mismo que 4.0 a los 45. Este escalonado por decada es la razon concreta de que
		// RangoPara reciba la edad en vez de ser una constante.
		public override RangoDeReferencia RangoPara(int edad)
		{
			decimal techo;
			if (edad < 50) techo = 2.5m;
			else if (edad < 60) techo = 3.5m;
			else if (edad < 70) techo = 4.5m;
			else techo = 6.5m;

			return RangoDeReferencia.HastaUnMaximoDe(techo, Suyo.Unidad);
		}

		public override decimal CambioMaximoVerosimilPorDia => 0.05m;

		// Velocidad de PSA: 0.75 ng/mL de cambio ya es criterio de seguimiento, incluso
		// con el valor absoluto dentro del rango.
		public override decimal CambioQueExigeAtencion => 0.75m;

		public override bool DireccionPreocupanteEsAscenso => true;

		// 0.75 ng/mL por ano. NO es un numero inventado para el demo: es el corte habitual
		// de velocidad de PSA, y se usa incluso con el valor absoluto dentro del rango. Es
		// la unica razon por la que este analito esta modelado y no dejado como relato.
		public override decimal VelocidadQueExigeAtencionPorAno => 0.75m;
	}
}
