using System;

namespace Examenes.Dominio
{
	// LA SERIE DE LAS CETONAS. Lo que aporta: unas cetonas de un dia de ayuno desaparecen en
	// la prueba siguiente, y las que no desaparecen no se explican por el ayuno. Es la misma
	// pregunta que en los nitritos -si cedio- con otra causa.
	internal sealed class SerieDeCetonas : SerieBinaria
	{
		internal SerieDeCetonas() : base(Analito.CetonasEnOrina) { }

		// ⚠️ Orientativa.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			Ultima != null && Ultima.EsHallazgo ? 3 : 12;

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDePresencia hechos = Evaluar();

			if (!HayMediciones) return Interpretacion.SinSerie(hechos);
			if (!hechos.HaySerie)
				return new Interpretacion(Veredicto.Indeterminado,
					Ultima.Resultado.SeveridadDelHallazgo, Ultima.Resultado.ClaveDeLaFrase, hechos,
					Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Presencia, Presencia.NuncaAparecio))
				return new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.cetonas.nunca-aparecio", hechos);

			// LA ESCALERA DE ESTE ANALITO: una vez y dos veces seguidas no piden lo mismo.
			if (ReferenceEquals(hechos.Presencia, Presencia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"lectura.cetonas.persiste", hechos,
					Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));

			if (ReferenceEquals(hechos.Presencia, Presencia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"lectura.cetonas.aparecio", hechos,
					Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));

			if (ReferenceEquals(hechos.Presencia, Presencia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"lectura.cetonas.no-confirmado", hechos);

			// LA BUENA NOTICIA, y la unica que se puede dar mirando la serie.
			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.cetonas.cedio", hechos);
		}
	}
}
