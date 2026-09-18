using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DEL CLORO. Lo que aporta es lo mismo que en el sodio, y por la misma razon:
	// la banda es angosta y un punto no dice si la persona esta ahi o va hacia ahi.
	//
	// Es una de las seis series de banda de la quimica general y se lee como la de la
	// hemoglobina, menos el sexo: aquella no se puede leer sin ese dato, esta si. Lo unico
	// suyo es el vocabulario.
	internal sealed class SerieDeCloro : SerieContinua
	{
		internal SerieDeCloro() : base(Analito.Cloro) { }

		private Cloro Ultima => (Cloro)Ultimo;

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.PosicionEnBanda;

		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			lineas.Add(new ReferenciaDeLectura("piso", rango.Minimo,
				"linea.cloro.piso"));
			lineas.Add(new ReferenciaDeLectura("techo", rango.Maximo,
				"linea.cloro.techo"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					"lectura.cloro.salto-inverosimil", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			// Mantenerse no puede juzgar por el signo -EsFavorable revienta-, asi que lo que
			// decide es si el ultimo valor quedo DENTRO de la banda. Se pregunta contra el
			// rango, que es lo que DireccionDeseable.Mantenerse no sabe hacer solo.
			bool seMovio = Ultima.EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = Ultima.EsNormalPara(paciente);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.cloro.estable-dentro", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.Vigilar,
					"lectura.cloro.estable-fuera", hechos, Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.cloro.volvio", hechos);

			return new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"lectura.cloro.paso-fuera", hechos, Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));
		}
	}
}
