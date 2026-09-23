using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DE LOS TRIGLICERIDOS. Es la unica de la quimica general que se lee contra una
	// META y no contra una banda, y por eso su forma es la del LDL y no la del sodio.
	//
	// LO QUE APORTA, y es el caso de Alvaro otra vez con otro analito: unos trigliceridos de
	// 300 que vienen bajando desde 500 son buena noticia aunque sigan fuera de rango, y la
	// hoja del laboratorio no puede decirlo. Aca la direccion SI juzga por el signo —bajar
	// es mejor, siempre— asi que EsFavorable se puede preguntar, al contrario de las seis de
	// banda.
	internal sealed class SerieDeTrigliceridos : SerieContinua
	{
		internal SerieDeTrigliceridos() : base(Analito.Trigliceridos) { }

		private Trigliceridos Ultima => (Trigliceridos)Ultimo;

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.ProgresoHaciaMeta;

		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			lineas.Add(new ReferenciaDeLectura("meta", rango.Maximo,
				"linea.trigliceridos.meta"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					"lectura.trigliceridos.salto-inverosimil", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			bool enRango = Ultima.EsNormalPara(paciente);

			if (!Ultima.EsCambioReal(hechos.Cambio))
				return enRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						"lectura.trigliceridos.estable-dentro", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.Vigilar,
						"lectura.trigliceridos.estable-fuera", hechos,
						Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));

			// LA BUENA NOTICIA QUE UN PUNTO NO PUEDE DAR: bajar es bajar, aunque el valor
			// siga fuera de la meta.
			if (Ultima.Direccion.EsFavorable(hechos.Cambio))
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.trigliceridos.bajo", hechos);

			return enRango
				? new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"lectura.trigliceridos.subio-dentro", hechos,
					Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita))
				: new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"lectura.trigliceridos.subio-fuera", hechos,
					Accion.MostrarleEstoATuMedico(Plazo.EsteMes));
		}
	}
}
