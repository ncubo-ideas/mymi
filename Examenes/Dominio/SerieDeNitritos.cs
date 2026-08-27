using System;

namespace Examenes.Dominio
{
	// LA SERIE DE LOS NITRITOS. Es el ejemplo textual del brief —§6.5: "negativo →
	// negativo → 2+ es un empeoramiento perfectamente interpretable"— y hasta ahora era
	// la unica cosa del brief que el dominio no podia representar.
	//
	// LO SUYO ES QUE LA INFECCION NO CEDA. Unos nitritos positivos sugieren infeccion
	// urinaria y se tratan; la pregunta que importa despues es si la prueba siguiente
	// volvio a salir negativa. Un solo punto no puede contestarla, y es la unica pregunta
	// que la persona de verdad tiene despues del tratamiento.
	public sealed class SerieDeNitritos : SerieBinaria
	{
		public SerieDeNitritos() : base(Analito.NitritosEnOrina) { }

		// Anual dentro del urianalisis de rutina; al mes si salieron positivos, que es
		// cuando corresponde comprobar que la infeccion cedio.
		//
		// ⚠️ Orientativa.
		public override int MesesHastaElProximo(int edad) =>
			Ultima != null && Ultima.EsHallazgo ? 1 : 12;

		public override Interpretacion Interpretar(int edad)
		{
			EvaluacionDePresencia hechos = Evaluar();

			if (!HayMediciones) return Interpretacion.SinSerie(hechos);
			if (!hechos.HaySerie)
				return new Interpretacion(Veredicto.Indeterminado,
					Ultima.Resultado.SeveridadDelHallazgo, Ultima.Resultado.ParaSonia, hechos,
					Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Presencia, Presencia.NuncaAparecio))
				return new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					$"los nitritos salieron negativos las {hechos.CantidadDeMediciones} veces",
					hechos);

			// El plazo es de ESTE analito: una infeccion urinaria amerita cita y no
			// espera un mes.
			if (ReferenceEquals(hechos.Presencia, Presencia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					$"los nitritos siguen positivos en las {hechos.PositivasSeguidas} pruebas "
					+ "seguidas: la infección no cedió", hechos,
					Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));

			if (ReferenceEquals(hechos.Presencia, Presencia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"los nitritos salieron positivos, que suele indicar una infección "
					+ "urinaria", hechos, Accion.MostrarleEstoATuMedico(Plazo.EstaSemana));

			if (ReferenceEquals(hechos.Presencia, Presencia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"los nitritos salieron positivos una vez y en la prueba siguiente "
					+ "volvieron a negativo", hechos);

			// LA BUENA NOTICIA DE ESTE ANALITO, y la unica que se puede dar mirando la
			// serie: estuvo, se trato, y se fue.
			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
				$"los nitritos estuvieron positivos {hechos.Positivas} veces y en la última "
				+ "ya salieron negativos", hechos);
		}
	}
}
