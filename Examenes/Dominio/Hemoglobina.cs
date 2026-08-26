using System;

namespace Examenes.Dominio
{
	// Hemoglobina. Es el analito que existe para que el dominio tenga un caso donde lo
	// que preocupa es el valor BAJO, no el alto: sin el, "clasificar" podria confundirse
	// con "pasarse del techo" y el rango con dos extremos no se estaria usando.
	//
	// Y ES EL UNICO CUYA DIRECCION DESEABLE ES MANTENERSE. Los otros tres quieren bajar
	// o subir; a este le conviene quedarse adentro del rango, y alejarse es peor en
	// cualquiera de las dos direcciones. Por eso no puede juzgar un cambio por su signo
	// -DireccionDeseable.EsFavorable revienta si se lo preguntan- y tiene que mirar
	// contra donde esta parado. Un bool "la direccion preocupante es el ascenso" no tenia
	// como decir esto.
	//
	// LO QUE ESTE ANALITO SABE Y NINGUN OTRO: el GRADO de anemia. No existe el grado de
	// anemia de un colesterol.
	public sealed class Hemoglobina : ExamenDeSangre
	{
		private static readonly Analito Suyo = Examenes.Dominio.Analito.Hemoglobina;

		public Hemoglobina(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		public override string Nombre => Suyo.ParaMostrar;

		// Rango con piso Y techo, a diferencia del colesterol y el antigeno.
		public override RangoDeReferencia RangoPara(int edad) =>
			edad >= 60
				? RangoDeReferencia.Entre(12.5m, 16.5m, Suyo.Unidad)
				: RangoDeReferencia.Entre(13.0m, 17.0m, Suyo.Unidad);

		public override decimal CambioMaximoVerosimilPorDia => 0.1m;

		public override decimal CambioMinimoSignificativo => 1.0m;

		public override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		// Anual, y semestral si hay anemia: cuando el valor ya esta bajo, lo que importa
		// es si sigue bajando, y eso no se ve esperando un ano.
		public override int MesesHastaElProximo(int edad) =>
			GradoDeAnemia().HayAnemia ? 6 : 12;

		// ---------------------------------------------------------------- lo suyo

		// CUANTA anemia hay, que es distinto de "esta bajo". El rango dice si cruzo el
		// piso; esto dice cuanto, y de ahi sale que hacer.
		public GradoDeAnemia GradoDeAnemia()
		{
			decimal valor = Valor.Valor;

			if (valor >= 13.0m) return Examenes.Dominio.GradoDeAnemia.Ninguna;
			if (valor >= 11.0m) return Examenes.Dominio.GradoDeAnemia.Leve;
			if (valor >= 8.0m) return Examenes.Dominio.GradoDeAnemia.Moderada;
			return Examenes.Dominio.GradoDeAnemia.Severa;
		}

		// ---------------------------------------------------------------- la lectura

		// Se lee por NIVEL contra su rango, no por pendiente ni por velocidad. El grado
		// de anemia manda sobre la tendencia: una hemoglobina que subio pero sigue en 9
		// no es una buena noticia.
		public override Interpretacion Interpretar(SerieDeAnalito serie, int edad)
		{
			ArgumentNullException.ThrowIfNull(serie);

			EvaluacionDeTendencia hechos = serie.Evaluar();
			if (!hechos.HaySerie)
			{
				// Con una sola medicion no hay tendencia, pero el NIVEL ya se puede leer.
				// Es lo que distingue a este analito: no necesita serie para decir algo
				// util, y sin embargo no inventa la serie que no tiene.
				GradoDeAnemia unico = GradoDeAnemia();
				if (!unico.HayAnemia) return Interpretacion.SinSerie(hechos);

				return new Interpretacion(Veredicto.Indeterminado, unico.Severidad,
					$"una sola medición ({Valor.Expresado}): todavía no hay serie, pero ese "
					+ $"valor ya indica anemia {unico.Nombre}", hechos);
			}

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Seguimiento,
					$"cambió {Math.Abs(hechos.Cambio)} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} día{(hechos.DiasTranscurridos == 1 ? "" : "s")}, "
					+ "que es demasiado para el tiempo transcurrido", hechos);

			GradoDeAnemia grado = GradoDeAnemia();

			// Mantenerse no puede juzgar por el signo, asi que lo que decide es si el
			// movimiento ACERCA o ALEJA del rango. Se pregunta contra el rango, que es lo
			// que DireccionDeseable.Mantenerse no sabe hacer sola.
			bool seMovio = EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = EsNormalPara(edad);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						$"se mantiene en {Valor.Expresado}, dentro de lo normal", hechos)
					: new Interpretacion(Veredicto.Estable, grado.Severidad,
						$"lleva tiempo en {Valor.Expresado}" + Coletilla(grado), hechos);

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					$"volvió a {Valor.Expresado}, dentro de lo normal", hechos);

			return new Interpretacion(Veredicto.Empeorando, grado.Severidad,
				$"pasó de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad}"
				+ Coletilla(grado), hechos);
		}

		private static string Coletilla(GradoDeAnemia grado) =>
			grado.HayAnemia ? $", que es anemia {grado.Nombre}" : ", fuera de lo normal";
	}
}
