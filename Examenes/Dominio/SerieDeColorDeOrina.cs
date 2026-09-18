using System;

namespace Examenes.Dominio
{
	// LA SERIE DEL COLOR DE LA ORINA. Es el ejemplo favorito de Alvaro, y hasta este cambio
	// era lo unico medido que este dominio no sabia poner en el tiempo.
	//
	// LO QUE LA SERIE APORTA, que es de lo unico que se trata. ColorDeOrina.SeveridadDelHallazgo
	// contesta Vigilar siempre, y esta bien: una magnitud solo se ve a si misma. La historia
	// distingue tres cosas que esa respuesta no puede distinguir:
	//
	//   un oscuro suelto           -> Vigilar          conversalo en la proxima cita
	//   DOS oscuros seguidos       -> ConsultePronto   ya no es un resultado suelto
	//   un oscuro y despues ambar  -> Informativo      no era, y decirlo es la buena noticia
	internal sealed class SerieDeColorDeOrina : SerieNominal
	{
		internal SerieDeColorDeOrina() : base(Analito.ColorDeLaOrina) { }

		// Anual dentro del urianalisis de rutina; a los tres meses si el ultimo color no fue
		// de lo esperable. No al mes como los nitritos: un color se MIRA y no amerita cita,
		// y esa diferencia de plazo entre dos analitos de la MISMA tira es el argumento de
		// Alvaro otra vez.
		//
		// ⚠️ Orientativa.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			Ultima != null && Ultima.EsHallazgo ? 3 : 12;

		// Un color no tiene rango: su referencia es el conjunto de lo esperable. Se escribe
		// aca y no en SerieNominal —al reves de SerieBinaria, que dice "lo esperable es
		// negativo" para los tres— porque el vocabulario es de ESTA muestra: los esperables
		// de la orina y los de las heces son dos conjuntos distintos.
		internal override Interpretacion ConSuReferencia(Interpretacion lectura, Paciente paciente) =>
			Ultima == null ? lectura : lectura.ConReferencia("referencia.color-de-la-orina");

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeNominal hechos = Evaluar();

			if (!HayMediciones) return Interpretacion.SinSerie(hechos);

			// CON UNA OBSERVACION LA LECTURA YA ESTA COMPLETA, y es la diferencia de fondo
			// con las otras dos formas. Un colesterol solo no dice para donde viene y un
			// positivo solo no esta confirmado; un color ES su categoria. Por eso el
			// veredicto es Observado y no Indeterminado, y por eso aca NO se llama a
			// Interpretacion.SinSerie —que ademas pediria repetir el examen, y un color no
			// se repite para confirmarse—.
			if (!hechos.HaySerie)
				return new Interpretacion(Veredicto.Observado,
					Ultima.Resultado.SeveridadDelHallazgo, Ultima.Resultado.ClaveDeLaFrase, hechos,
					Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Constancia, Constancia.SiempreEsperable))
				return new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.color-de-la-orina.siempre-esperable", hechos);

			// LA ESCALERA, que es la pieza entera: el mismo color, leido con historia, deja
			// de esperar a la proxima cita.
			if (ReferenceEquals(hechos.Constancia, Constancia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					hechos.LaUltimaSeRepitio
						? "lectura.color-de-la-orina.persiste-la-misma"
						: "lectura.color-de-la-orina.persiste",
					hechos, Accion.MostrarleEstoATuMedico(Plazo.EsteMes));

			if (ReferenceEquals(hechos.Constancia, Constancia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando,
					Ultima.Resultado.SeveridadDelHallazgo,
					"lectura.color-de-la-orina.aparecio", hechos, Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Constancia, Constancia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"lectura.color-de-la-orina.no-confirmado", hechos);

			// LA BUENA NOTICIA que solo la serie puede dar.
			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.color-de-la-orina.volvio", hechos);
		}
	}
}
