using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DE LA GLUCOSA EN AYUNAS. Lo que aporta sobre una medicion sola es el
	// caso mas comun de todos: un 101 aislado no es nada y tres 101 en tres anos son una
	// trayectoria, y las dos cosas se ven igual en la hoja del laboratorio.
	//
	// Es una de las seis series de banda de la quimica general y se lee como la de la
	// hemoglobina, menos el sexo: aquella no se puede leer sin ese dato, esta si. Lo unico
	// suyo es el vocabulario.
	internal sealed class SerieDeGlucosaEnAyunas : SerieContinua
	{
		internal SerieDeGlucosaEnAyunas() : base(Analito.GlucosaEnAyunas) { }

		private GlucosaEnAyunas Ultima => (GlucosaEnAyunas)Ultimo;

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.PosicionEnBanda;

		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			lineas.Add(new ReferenciaDeLectura("piso", rango.Minimo,
				"linea.glucosa-en-ayunas.piso"));
			lineas.Add(new ReferenciaDeLectura("techo", rango.Maximo,
				"linea.glucosa-en-ayunas.techo"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					"lectura.glucosa-en-ayunas.salto-inverosimil", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			// Mantenerse no puede juzgar por el signo -EsFavorable revienta-, asi que lo que
			// decide es si el ultimo valor quedo DENTRO de la banda. Se pregunta contra el
			// rango, que es lo que DireccionDeseable.Mantenerse no sabe hacer solo.
			bool seMovio = Ultima.EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = Ultima.EsNormalPara(paciente);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.glucosa-en-ayunas.estable-dentro", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.Vigilar,
					"lectura.glucosa-en-ayunas.estable-fuera", hechos, Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.glucosa-en-ayunas.volvio", hechos);

			return new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"lectura.glucosa-en-ayunas.paso-fuera", hechos, Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));
		}
	}
}
