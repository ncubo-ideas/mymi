using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DEL NITROGENO UREICO. Lo que aporta: un BUN alto por deshidratacion vuelve
	// solo en la medicion siguiente, y uno que no vuelve es otra cosa. Con un punto no se
	// pueden separar, y son las dos causas mas frecuentes.
	//
	// Es una de las seis series de banda de la quimica general y se lee como la de la
	// hemoglobina, menos el sexo: aquella no se puede leer sin ese dato, esta si. Lo unico
	// suyo es el vocabulario.
	internal sealed class SerieDeNitrogenoUreico : SerieContinua
	{
		internal SerieDeNitrogenoUreico() : base(Analito.NitrogenoUreico) { }

		private NitrogenoUreico Ultima => (NitrogenoUreico)Ultimo;

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.PosicionEnBanda;

		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			lineas.Add(new ReferenciaDeLectura("piso", rango.Minimo,
				"linea.nitrogeno-ureico.piso"));
			lineas.Add(new ReferenciaDeLectura("techo", rango.Maximo,
				"linea.nitrogeno-ureico.techo"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					"lectura.nitrogeno-ureico.salto-inverosimil", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			// Mantenerse no puede juzgar por el signo -EsFavorable revienta-, asi que lo que
			// decide es si el ultimo valor quedo DENTRO de la banda. Se pregunta contra el
			// rango, que es lo que DireccionDeseable.Mantenerse no sabe hacer solo.
			bool seMovio = Ultima.EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = Ultima.EsNormalPara(paciente);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.nitrogeno-ureico.estable-dentro", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.Vigilar,
					"lectura.nitrogeno-ureico.estable-fuera", hechos, Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.nitrogeno-ureico.volvio", hechos);

			return new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"lectura.nitrogeno-ureico.paso-fuera", hechos, Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));
		}
	}
}
