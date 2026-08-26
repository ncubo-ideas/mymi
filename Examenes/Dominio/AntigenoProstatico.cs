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
	// LO QUE ESTE ANALITO SABE Y NINGUN OTRO: leerse por VELOCIDAD. Un cambio de 2.2
	// ng/mL importa por los ng/mL POR ANO que representa, no por el valor final. Ese
	// metodo no existe en la hemoglobina ni en el colesterol, y no porque no se haya
	// escrito: porque no significa nada. "La glucosuria subio 1,5 cruces por ano" no es
	// una frase.
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

		// 0.75 ng/mL por ano. NO es un numero inventado para el demo: es el corte habitual
		// de velocidad de PSA, y se usa incluso con el valor absoluto dentro del rango.
		public const decimal VelocidadQueExigeAtencionPorAno = 0.75m;

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

		public override decimal CambioMinimoSignificativo => 0.75m;

		public override DireccionDeseable Direccion => DireccionDeseable.Bajar;

		// ---------------------------------------------------------------- lo suyo

		// LA capacidad de este analito. Es publica y con nombre porque es lo que la IA
		// pregunta y la pantalla no contesta.
		//
		// EL TRAMO ES EL MAS RECIENTE, no la serie entera, y esa es la regla nombrada:
		// los anos estables de antes no diluyen un salto de este ano, lo hacen mas
		// notable. Con los numeros del guion la diferencia decide el caso:
		//
		//   serie entera   3.0 (2023) -> 5.2 (2026)   2.2 ng/mL en 1096 dias = 0.73/ano
		//   tramo reciente 3.0 (2025) -> 5.2 (2026)   2.2 ng/mL en  365 dias = 2.20/ano
		//
		// Con el umbral en 0.75 la serie entera diria que NO exige atencion.
		public Velocidad VelocidadDe(SerieDeAnalito serie)
		{
			ArgumentNullException.ThrowIfNull(serie);

			string unidad = Valor.Unidad.Simbolo;
			int dias = serie.DiasDelUltimoTramo;

			// Dos mediciones el MISMO dia no dan una velocidad: un cociente sobre cero
			// dias no es un ritmo anual, es una division por cero disfrazada. Ese caso ya
			// lo atrapa Evaluar() como salto inverosimil, que es la lectura correcta.
			if (dias <= 0)
				return Velocidad.NoAplica(Suyo, unidad, VelocidadQueExigeAtencionPorAno);

			decimal porAno = serie.CambioDelUltimoTramo * SerieDeAnalito.DiasDeUnAno / dias;

			return Velocidad.Medida(Suyo, porAno, unidad, dias, VelocidadQueExigeAtencionPorAno);
		}

		// Si el antigeno se mantuvo plano durante los ultimos anos. Es la otra pregunta
		// propia: la linea base es lo que le da sentido al ultimo valor.
		public bool EsEstableDurante(SerieDeAnalito serie, int anos)
		{
			ArgumentNullException.ThrowIfNull(serie);
			if (anos <= 0)
				throw new ArgumentOutOfRangeException(nameof(anos), anos,
					"Preguntar por la estabilidad de cero anos no tiene respuesta.");

			EvaluacionDeTendencia hechos = serie.Evaluar();
			if (!hechos.HaySerie) return false;
			if (hechos.DiasTranscurridos < anos * SerieDeAnalito.DiasDeUnAno) return false;

			return !EsCambioReal(hechos.Cambio);
		}

		// ---------------------------------------------------------------- la lectura

		// Se lee por VELOCIDAD antes que por nivel, que es lo contrario de los demas.
		// Un 5.2 con techo 4.5 esta apenas arriba; lo que manda es que llego ahi en un
		// ano viniendo de 3.0.
		public override Interpretacion Interpretar(SerieDeAnalito serie, int edad)
		{
			ArgumentNullException.ThrowIfNull(serie);

			EvaluacionDeTendencia hechos = serie.Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Seguimiento,
					$"cambió {Formato(hechos.Cambio)} {hechos.Unidad} en {hechos.DiasTranscurridos} días, "
					+ "que es demasiado para el tiempo transcurrido", hechos);

			// EL CASO DEL ANALITO: la velocidad sola alcanza, aunque el valor este en
			// rango. Va primero por eso.
			Velocidad velocidad = VelocidadDe(serie);
			if (velocidad.ExigeAtencion)
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultaMedica,
					$"viene subiendo a {velocidad.Expresada}, y por encima de "
					+ $"{Formato(VelocidadQueExigeAtencionPorAno)} {hechos.Unidad} por año conviene mirarlo",
					hechos);

			if (!EsCambioReal(hechos.Cambio))
			{
				// Estable pero por encima del techo de su edad sigue siendo algo que un
				// medico tiene que ver. Estable no significa bien.
				return EsNormalPara(edad)
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						$"se mantiene alrededor de {Valor.Expresado} desde hace "
						+ $"{hechos.DiasTranscurridos} días", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultaMedica,
						$"se mantiene estable, pero en {Valor.Expresado} está por encima de "
						+ $"lo esperado a tu edad", hechos);
			}

			return Direccion.EsFavorable(hechos.Cambio)
				? new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					$"bajó de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad}", hechos)
				: new Interpretacion(Veredicto.Empeorando, Severidad.Seguimiento,
					$"subió de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad} "
					+ $"en {hechos.DiasTranscurridos} días", hechos);
		}

		private static string Formato(decimal valor) => valor.ToString("0.##");
	}
}
