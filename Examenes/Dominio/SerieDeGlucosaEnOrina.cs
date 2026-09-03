using System;

namespace Examenes.Dominio
{
	// LA SERIE DE LA GLUCOSA EN ORINA. Lo que aporta no es la severidad -que no escala- sino
	// la buena noticia: una glucosuria que en la prueba siguiente ya no esta es la unica cosa
	// tranquilizadora que se puede decir de ella, y hace falta la segunda prueba para poder
	// decirla.
	internal sealed class SerieDeGlucosaEnOrina : SerieBinaria
	{
		internal SerieDeGlucosaEnOrina() : base(Analito.GlucosaEnLaOrina) { }

		// ⚠️ Orientativa.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			Ultima != null && Ultima.EsHallazgo ? 3 : 12;

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDePresencia hechos = Evaluar();

			if (!HayMediciones) return Interpretacion.SinSerie(hechos);
			if (!hechos.HaySerie)
				return new Interpretacion(Veredicto.Indeterminado,
					Ultima.Resultado.SeveridadDelHallazgo, Ultima.Resultado.ParaSonia, hechos,
					Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Presencia, Presencia.NuncaAparecio))
				return new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					$"no salió glucosa en la orina en las {hechos.CantidadDeMediciones} veces", hechos);

			// LA ESCALERA DE ESTE ANALITO: una vez y dos veces seguidas no piden lo mismo.
			if (ReferenceEquals(hechos.Presencia, Presencia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					$"la glucosa salió en la orina en las {hechos.PositivasSeguidas} pruebas "
					+ "seguidas", hechos, Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));

			if (ReferenceEquals(hechos.Presencia, Presencia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"salió glucosa en la orina, que casi siempre significa que la glucosa en "
					+ "sangre estuvo alta", hechos,
					Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));

			if (ReferenceEquals(hechos.Presencia, Presencia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"la glucosa salió en la orina una vez y en la prueba siguiente ya no", hechos);

			// LA BUENA NOTICIA, y la unica que se puede dar mirando la serie.
			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
				$"la glucosa salió en la orina {hechos.Positivas} veces y en la última ya no", hechos);
		}
	}
}
