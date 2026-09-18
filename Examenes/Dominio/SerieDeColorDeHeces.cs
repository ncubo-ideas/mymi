using System;

namespace Examenes.Dominio
{
	// LA SERIE DEL COLOR DE LAS HECES. Se lee al lado de SerieDeColorDeOrina, igual que sus
	// magnitudes, y es el mismo contraste que justifica que ExamenDeHeces exista: misma
	// forma, mismo conteo, prosa y plazos propios.
	//
	// LO SUYO: LA CATEGORIA IMPORTA MAS QUE EN LA ORINA. Un negro y un palido son los dos
	// inesperados y no son lo mismo, asi que "salió negro las 2 veces seguidas" y "no vuelve
	// a lo esperable" son dos frases distintas y no una con adornos. Es la razon concreta de
	// que EvaluacionDeNominal cargue LaUltimaSeRepitio.
	internal sealed class SerieDeColorDeHeces : SerieNominal
	{
		internal SerieDeColorDeHeces() : base(Analito.ColorDeLasHeces) { }

		// ⚠️ Orientativa.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			Ultima != null && Ultima.EsHallazgo ? 3 : 12;

		internal override Interpretacion ConSuReferencia(Interpretacion lectura, Paciente paciente) =>
			Ultima == null ? lectura : lectura.ConReferencia("referencia.color-de-las-heces");

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeNominal hechos = Evaluar();

			if (!HayMediciones) return Interpretacion.SinSerie(hechos);

			// Ver SerieDeColorDeOrina.Leer: con una observacion la lectura ya esta completa.
			if (!hechos.HaySerie)
				return new Interpretacion(Veredicto.Observado,
					Ultima.Resultado.SeveridadDelHallazgo, Ultima.Resultado.ClaveDeLaFrase, hechos,
					Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Constancia, Constancia.SiempreEsperable))
				return new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.color-de-las-heces.siempre-esperable", hechos);

			if (ReferenceEquals(hechos.Constancia, Constancia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					hechos.LaUltimaSeRepitio
						? "lectura.color-de-las-heces.persiste-la-misma"
						: "lectura.color-de-las-heces.persiste",
					hechos, Accion.MostrarleEstoATuMedico(Plazo.EsteMes));

			if (ReferenceEquals(hechos.Constancia, Constancia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando,
					Ultima.Resultado.SeveridadDelHallazgo,
					"lectura.color-de-las-heces.aparecio", hechos, Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Constancia, Constancia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"lectura.color-de-las-heces.no-confirmado", hechos);

			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.color-de-las-heces.volvio", hechos);
		}
	}
}
