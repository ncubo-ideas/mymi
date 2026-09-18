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
				"linea.potasio.piso"));
			lineas.Add(new ReferenciaDeLectura("techo", rango.Maximo,
				"linea.potasio.techo"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					"lectura.potasio.salto-inverosimil", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			// Mantenerse no puede juzgar por el signo -EsFavorable revienta-, asi que lo que
			// decide es si el ultimo valor quedo DENTRO de la banda. Se pregunta contra el
			// rango, que es lo que DireccionDeseable.Mantenerse no sabe hacer solo.
			bool seMovio = Ultima.EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = Ultima.EsNormalPara(paciente);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.potasio.estable-dentro", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultePronto,
					"lectura.potasio.estable-fuera", hechos, Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.potasio.volvio", hechos);

			return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"lectura.potasio.paso-fuera", hechos, Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));
		}
	}
}
