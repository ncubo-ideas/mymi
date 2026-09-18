using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DEL SODIO. Lo que aporta: sobre una banda de diez unidades, la diferencia
	// entre "asi es esta persona" y "se esta moviendo" no se puede ver en un punto. Un 134
	// aislado y un 134 que viene de 141 no piden lo mismo.
	//
	// Es una de las seis series de banda de la quimica general y se lee como la de la
	// hemoglobina, menos el sexo: aquella no se puede leer sin ese dato, esta si. Lo unico
	// suyo es el vocabulario.
	internal sealed class SerieDeSodio : SerieContinua
	{
		internal SerieDeSodio() : base(Analito.Sodio) { }

		private Sodio Ultima => (Sodio)Ultimo;

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.PosicionEnBanda;

		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			lineas.Add(new ReferenciaDeLectura("piso", rango.Minimo,
				"linea.sodio.piso"));
			lineas.Add(new ReferenciaDeLectura("techo", rango.Maximo,
				"linea.sodio.techo"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					"lectura.sodio.salto-inverosimil", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			// Mantenerse no puede juzgar por el signo -EsFavorable revienta-, asi que lo que
			// decide es si el ultimo valor quedo DENTRO de la banda. Se pregunta contra el
			// rango, que es lo que DireccionDeseable.Mantenerse no sabe hacer solo.
			bool seMovio = Ultima.EsCambioReal(hechos.Cambio);
			bool ahoraEnRango = Ultima.EsNormalPara(paciente);

			if (!seMovio)
				return ahoraEnRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.sodio.estable-dentro", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultePronto,
					"lectura.sodio.estable-fuera", hechos, Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));

			if (ahoraEnRango)
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.sodio.volvio", hechos);

			return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"lectura.sodio.paso-fuera", hechos, Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));
		}
	}
}
