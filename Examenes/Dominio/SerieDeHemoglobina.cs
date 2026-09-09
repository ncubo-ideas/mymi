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
	internal sealed class SerieDeHemoglobina : SerieContinua
	{
		internal SerieDeHemoglobina() : base(Analito.Hemoglobina) { }

		private Hemoglobina Ultima => (Hemoglobina)Ultimo;

		// ---------------------------------------------------------------- lo suyo

		internal GradoDeAnemia PeorGrado(Paciente paciente)
		{
			GradoDeAnemia peor = GradoDeAnemia.Ninguna;
			foreach (ExamenDeSangre medicion in Mediciones)
			{
				GradoDeAnemia grado = ((Hemoglobina)medicion).GradoDeAnemia(paciente);
				if (grado.EsPeorQue(peor)) peor = grado;
			}
			return peor;
		}

		// Si hubo anemia antes y hoy ya no la hay. Es la unica forma de decir "te
		// recuperaste", y necesita los dos extremos.
		internal bool VieneRecuperandose(Paciente paciente) =>
			Cantidad >= 2 && PeorGrado(paciente).HayAnemia && !Ultima.GradoDeAnemia(paciente).HayAnemia;

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.PosicionEnBanda;

		// LA SERIE QUE NO SE PUEDE LEER SIN SEXO. Es una de las dos —la otra es el HDL, que
		// viaja adentro del colesterol fraccionado— y la razon es concreta: 12.2 g/dL es
		// anemia leve en un hombre y es normal en una mujer. No hay lectura intermedia que
		// dar; hay que preguntar.
		protected override bool SeSabeLeerPara(Paciente paciente) =>
			Ultimo == null || Ultima.HayRangoPara(paciente);

		// Piso Y techo: es el unico analito del demo donde alejarse es peor en las dos
		// direcciones, y por eso su dibujo es una banda y no una linea.
		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			// SIN SEXO NO HAY BANDA QUE DIBUJAR. El piso y el techo son exactamente lo que
			// depende del dato que falta, asi que dibujarlos con el rango masculino seria
			// mostrarle a la persona una banda que quizas no es la suya. La lista vacia es
			// la respuesta correcta, y la tarjeta muestra el valor sin el dibujo.
			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			if (rango == null) return lineas;
			lineas.Add(new ReferenciaDeLectura("piso", rango.Minimo,
				$"por debajo de {rango.Minimo.Expresado} ya es anemia"));
			lineas.Add(new ReferenciaDeLectura("techo", rango.Maximo,
				$"lo normal llega hasta {rango.Maximo.Expresado}"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie)
			{
				// Con una sola medicion no hay tendencia, pero el NIVEL ya se puede leer.
				// Es lo que distingue a este analito: no necesita serie para decir algo
				// util, y sin embargo no inventa la serie que no tiene.
				GradoDeAnemia unico = Ultima.GradoDeAnemia(paciente);
				if (!unico.HayAnemia) return Interpretacion.SinSerie(hechos);

				return new Interpretacion(Veredicto.Indeterminado, unico.Severidad,
					$"una sola medición ({Ultima.Valor.Expresado}): todavía no hay serie, pero ese "
					+ $"valor ya indica anemia {unico.Nombre}", hechos, unico.Accion);
			}

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					$"cambió {Math.Abs(hechos.Cambio)} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} día{(hechos.DiasTranscurridos == 1 ? "" : "s")}, "
					+ "que es demasiado para el tiempo transcurrido", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			GradoDeAnemia grado = Ultima.GradoDeAnemia(paciente);

			// Mantenerse no puede juzgar por el signo, asi que lo que decide es si el
			// movimiento ACERCA o ALEJA del rango. Se pregunta contra el rango, que es lo
			// que DireccionDeseable.Mantenerse no sabe hacer sola.
			bool seMovio = Ultima.EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = Ultima.EsNormalPara(paciente);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						$"se mantiene en {Ultima.Valor.Expresado}, dentro de lo normal", hechos)
					: new Interpretacion(Veredicto.Estable, grado.Severidad,
						$"lleva tiempo en {Ultima.Valor.Expresado}" + Coletilla(grado), hechos,
						grado.Accion);

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					$"volvió a {Ultima.Valor.Expresado}, dentro de lo normal", hechos);

			return new Interpretacion(Veredicto.Empeorando, grado.Severidad,
				$"pasó de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad}"
				+ Coletilla(grado), hechos, grado.Accion);
		}

		private static string Coletilla(GradoDeAnemia grado) =>
			grado.HayAnemia ? $", que es anemia {grado.Nombre}" : ", fuera de lo normal";
	}
}
