using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DEL POTASIO. Lo que aporta, y es el caso donde mas aporta de los siete: un
	// potasio alto que en la medicion siguiente volvio a 4.2 era casi seguro una muestra
	// hemolizada. La hoja de aquel dia sigue diciendo 5.6 para siempre.
	//
	// Es una de las seis series de banda de la quimica general y se lee como la de la
	// hemoglobina, menos el sexo: aquella no se puede leer sin ese dato, esta si. Lo unico
	// suyo es el vocabulario.
	internal sealed class SerieDePotasio : SerieContinua
	{
		internal SerieDePotasio() : base(Analito.Potasio) { }

		private Potasio Ultima => (Potasio)Ultimo;

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.PosicionEnBanda;

		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			lineas.Add(new ReferenciaDeLectura("piso", rango.Minimo,
				$"por debajo de {rango.Minimo.Expresado} es hipopotasemia"));
			lineas.Add(new ReferenciaDeLectura("techo", rango.Maximo,
				$"lo normal llega hasta {rango.Maximo.Expresado}"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					$"el potasio cambió {Math.Abs(hechos.Cambio)} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} día{(hechos.DiasTranscurridos == 1 ? "" : "s")}, "
					+ "que es demasiado para el tiempo transcurrido", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			// Mantenerse no puede juzgar por el signo -EsFavorable revienta-, asi que lo que
			// decide es si el ultimo valor quedo DENTRO de la banda. Se pregunta contra el
			// rango, que es lo que DireccionDeseable.Mantenerse no sabe hacer solo.
			bool seMovio = Ultima.EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = Ultima.EsNormalPara(paciente);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						$"el potasio se mantiene en {Ultima.Valor.Expresado}, dentro de lo normal",
						hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultePronto,
						$"el potasio lleva tiempo en {Ultima.Valor.Expresado}, fuera de lo normal",
						hechos, Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					$"el potasio volvió a {Ultima.Valor.Expresado}, dentro de lo normal", hechos);

			return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
				$"el potasio pasó de {hechos.ValorInicial} a {hechos.ValorFinal} "
				+ $"{hechos.Unidad}, fuera de lo normal",
				hechos, Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));
		}
	}
}
