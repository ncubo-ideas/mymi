using System;

namespace Examenes.Dominio
{
	// LA SERIE DE LA SANGRE EN LA ORINA. La otra mitad del contraste de Alvaro, ahora a
	// nivel de serie: cuenta exactamente igual que la de heces y concluye distinto.
	//
	// LO QUE LA SERIE APORTA ACA NO ES UNA ESCALADA, ES UN DESCARGO. La sangre en la
	// orina ya entra en la severidad mas alta que produce este dominio con una sola
	// prueba, asi que no hay a donde escalar. Lo que no se podia decir sin historia es lo
	// contrario:
	//
	//   apareció una vez y la siguiente salió limpia -> fue PASAJERA
	//
	// Y esa distincion es real: una hematuria transitoria suele tener causas banales, y
	// la que se repite es la que se estudia. Con una sola medicion las dos se leen igual
	// —Urgente— y la persona se queda con el susto. Es §2 de la reunion
	// —"cuando lo vuelve a medir pasa otra vez a 3, ahi al medico esa segunda lectura le
	// sirve"— en su version binaria.
	public sealed class SerieDeSangreEnOrina : SerieBinaria
	{
		public SerieDeSangreEnOrina() : base(Analito.SangreEnLaOrina) { }

		// Anual dentro del urianalisis de rutina; al MES si aparecio, porque lo que hay
		// que averiguar es si fue pasajera y esa respuesta no espera.
		//
		// ⚠️ Orientativa, igual que todos los umbrales de este dominio.
		public override int MesesHastaElProximo(Paciente paciente) =>
			Ultima != null && Ultima.EsHallazgo ? 1 : 12;

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
					$"en las {hechos.CantidadDeMediciones} pruebas que te hiciste nunca "
					+ "apareció sangre en la orina", hechos);

			// Ya estaba en lo mas alto: persistir no sube nada, y decirlo asi es mas
			// honesto que inventar un escalon que este dominio no tiene. Lo que cambia es
			// la frase, y no es adorno: "no es pasajera" es la informacion.
			if (ReferenceEquals(hechos.Presencia, Presencia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.Urgente,
					$"la sangre en la orina apareció en las {hechos.PositivasSeguidas} pruebas "
					+ "seguidas: no fue algo pasajero", hechos,
					Accion.BuscarAtencionMedica(Plazo.Pronto));

			if (ReferenceEquals(hechos.Presencia, Presencia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando, Severidad.Urgente,
					"apareció sangre en la orina, y eso conviene que lo revise un médico "
					+ "pronto", hechos, Accion.BuscarAtencionMedica(Plazo.Pronto));

			// EL DESCARGO, y es la unica lectura de este analito que no pide nada. Sin la
			// serie, este mismo expediente decia "buscá atención médica pronto".
			if (ReferenceEquals(hechos.Presencia, Presencia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"apareció sangre en la orina una vez y la prueba siguiente salió limpia: "
					+ "fue pasajera", hechos);

			// Estuvo de verdad y despues se apago. No es lo mismo que no haber estado
			// nunca: conviene confirmarlo con una prueba mas.
			return new Interpretacion(Veredicto.Mejorando, Severidad.Vigilar,
				$"la sangre en la orina apareció {hechos.Positivas} veces y en la última ya "
				+ "no está", hechos, Accion.RepetirElExamen(Plazo.EsteMes));
		}
	}
}
