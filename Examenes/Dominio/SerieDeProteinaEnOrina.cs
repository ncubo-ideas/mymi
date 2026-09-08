using System;

namespace Examenes.Dominio
{
	// LA SERIE DE LA PROTEINA EN ORINA. Es la que mas cambia entre una prueba y dos: una
	// proteinuria aislada es pasajera en la mayoria de los casos, y solo la segunda prueba
	// puede decir si esta era de esas. Sin serie, la hoja del laboratorio dice "positivo"
	// para siempre.
	internal sealed class SerieDeProteinaEnOrina : SerieBinaria
	{
		internal SerieDeProteinaEnOrina() : base(Analito.ProteinaEnLaOrina) { }

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
					$"no salió proteína en la orina en las {hechos.CantidadDeMediciones} veces", hechos);

			// LA ESCALERA DE ESTE ANALITO: una vez y dos veces seguidas no piden lo mismo.
			if (ReferenceEquals(hechos.Presencia, Presencia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					$"la proteína salió en las {hechos.PositivasSeguidas} pruebas seguidas: una "
					+ "proteinuria que se repite ya no es pasajera", hechos,
					Accion.MostrarleEstoATuMedico(Plazo.EsteMes));

			if (ReferenceEquals(hechos.Presencia, Presencia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"salió proteína en la orina, que una sola vez suele ser pasajero —fiebre, "
					+ "ejercicio— y lo que corresponde es repetirlo", hechos,
					Accion.RepetirElExamen(Plazo.EsteMes));

			if (ReferenceEquals(hechos.Presencia, Presencia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"la proteína salió una vez y en la prueba siguiente ya no estaba", hechos);

			// LA BUENA NOTICIA, y la unica que se puede dar mirando la serie.
			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
				$"la proteína salió {hechos.Positivas} veces y en la última ya no está", hechos);
		}
	}
}
