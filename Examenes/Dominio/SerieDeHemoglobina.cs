using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DE LA HEMOGLOBINA. No tiene velocidad ni meta: se lee por NIVEL contra su
	// rango, y el grado de anemia manda sobre la tendencia.
	//
	// Lo suyo es PeorGrado(): la anemia mas profunda que hubo en toda la serie. Un valor
	// que hoy esta bien despues de haber estado en 8.0 no se lee igual que uno que nunca
	// bajo, y eso no se puede preguntar mirando una medicion sola.
	public sealed class SerieDeHemoglobina : SerieDeAnalito
	{
		public SerieDeHemoglobina() : base(Analito.Hemoglobina) { }

		private Hemoglobina Ultima => (Hemoglobina)Ultimo;

		// ---------------------------------------------------------------- lo suyo

		public GradoDeAnemia PeorGrado()
		{
			GradoDeAnemia peor = GradoDeAnemia.Ninguna;
			foreach (ExamenDeSangre medicion in Mediciones)
			{
				GradoDeAnemia grado = ((Hemoglobina)medicion).GradoDeAnemia();
				if (grado.EsPeorQue(peor)) peor = grado;
			}
			return peor;
		}

		// Si hubo anemia antes y hoy ya no la hay. Es la unica forma de decir "te
		// recuperaste", y necesita los dos extremos.
		public bool VieneRecuperandose() =>
			Cantidad >= 2 && PeorGrado().HayAnemia && !Ultima.GradoDeAnemia().HayAnemia;

		// ---------------------------------------------------------------- la lectura

		public override Interpretacion Interpretar(int edad)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie)
			{
				// Con una sola medicion no hay tendencia, pero el NIVEL ya se puede leer.
				// Es lo que distingue a este analito: no necesita serie para decir algo
				// util, y sin embargo no inventa la serie que no tiene.
				GradoDeAnemia unico = Ultima.GradoDeAnemia();
				if (!unico.HayAnemia) return Interpretacion.SinSerie(hechos);

				return new Interpretacion(Veredicto.Indeterminado, unico.Severidad,
					$"una sola medición ({Ultima.Valor.Expresado}): todavía no hay serie, pero ese "
					+ $"valor ya indica anemia {unico.Nombre}", hechos);
			}

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Seguimiento,
					$"cambió {Math.Abs(hechos.Cambio)} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} día{(hechos.DiasTranscurridos == 1 ? "" : "s")}, "
					+ "que es demasiado para el tiempo transcurrido", hechos);

			GradoDeAnemia grado = Ultima.GradoDeAnemia();

			// Mantenerse no puede juzgar por el signo, asi que lo que decide es si el
			// movimiento ACERCA o ALEJA del rango. Se pregunta contra el rango, que es lo
			// que DireccionDeseable.Mantenerse no sabe hacer sola.
			bool seMovio = Ultima.EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = Ultima.EsNormalPara(edad);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						$"se mantiene en {Ultima.Valor.Expresado}, dentro de lo normal", hechos)
					: new Interpretacion(Veredicto.Estable, grado.Severidad,
						$"lleva tiempo en {Ultima.Valor.Expresado}" + Coletilla(grado), hechos);

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					$"volvió a {Ultima.Valor.Expresado}, dentro de lo normal", hechos);

			return new Interpretacion(Veredicto.Empeorando, grado.Severidad,
				$"pasó de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad}"
				+ Coletilla(grado), hechos);
		}

		private static string Coletilla(GradoDeAnemia grado) =>
			grado.HayAnemia ? $", que es anemia {grado.Nombre}" : ", fuera de lo normal";
	}
}
