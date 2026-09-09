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

		internal override string ReferenciaLegible(Paciente paciente) =>
			Ultima == null ? "" : "lo esperable es chocolate, marrón o verdoso";

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeNominal hechos = Evaluar();

			if (!HayMediciones) return Interpretacion.SinSerie(hechos);

			// Ver SerieDeColorDeOrina.Leer: con una observacion la lectura ya esta completa.
			if (!hechos.HaySerie)
				return new Interpretacion(Veredicto.Observado,
					Ultima.Resultado.SeveridadDelHallazgo, Ultima.Resultado.ParaSonia, hechos,
					Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Constancia, Constancia.SiempreEsperable))
				return new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					$"en las {hechos.CantidadDeMediciones} veces que te lo hiciste el color de "
					+ "las heces estuvo dentro de lo esperable", hechos);

			if (ReferenceEquals(hechos.Constancia, Constancia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					hechos.LaUltimaSeRepitio
						? $"el color de las heces salió {hechos.UltimaCategoria} las "
							+ $"{hechos.NoEsperablesSeguidas} veces seguidas: ya no es un "
							+ "resultado suelto"
						: "el color de las heces no vuelve a lo esperable en las "
							+ $"{hechos.NoEsperablesSeguidas} últimas veces",
					hechos, Accion.MostrarleEstoATuMedico(Plazo.EsteMes));

			if (ReferenceEquals(hechos.Constancia, Constancia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando,
					Ultima.Resultado.SeveridadDelHallazgo,
					$"el color de las heces salió {hechos.UltimaCategoria}, y la vez anterior "
					+ "era de lo esperable", hechos, Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Constancia, Constancia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"una vez el color de las heces salió fuera de lo esperable y en el examen "
					+ $"siguiente volvió a {hechos.UltimaCategoria}", hechos);

			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
				$"el color de las heces estuvo fuera de lo esperable {hechos.NoEsperables} "
				+ $"veces y en la última ya salió {hechos.UltimaCategoria}", hechos);
		}
	}
}
