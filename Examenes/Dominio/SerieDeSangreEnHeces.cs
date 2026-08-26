using System;

namespace Examenes.Dominio
{
	// LA SERIE DE LA SANGRE OCULTA EN HECES. Es la que justifica que los binarios tengan
	// serie, porque es la unica del demo donde la HISTORIA cambia la conducta:
	//
	//   una vez positiva      -> Seguimiento      "conviene repetir el examen"
	//   dos veces SEGUIDAS    -> ConsultaMedica   ya se repitio y volvio a salir
	//
	// SangreEnHeces.SeveridadDelHallazgo contesta Seguimiento siempre, y esta bien: una
	// magnitud solo se ve a si misma. La que puede distinguir un dato suelto de un hecho
	// confirmado es la serie, y no lo hace con un umbral generico sino con el criterio de
	// ESTE analito: en otro, dos positivos no significarian lo mismo.
	//
	// Se lee al lado de SerieDeSangreEnOrina, igual que sus magnitudes: misma forma,
	// mismo conteo, conclusiones distintas. La diferencia esta abajo, en Interpretar, y
	// es la unica parte que importa.
	public sealed class SerieDeSangreEnHeces : SerieBinaria
	{
		public SerieDeSangreEnHeces() : base(Analito.SangreEnLasHeces) { }

		// Anual como tamizaje, y a los tres meses si aparecio: lo que hay que saber es si
		// se repite, y eso no se ve esperando un ano.
		//
		// ⚠️ Orientativa. La periodicidad real depende de la edad y de los antecedentes,
		// y conviene que la revise alguien con criterio de laboratorio.
		public override int MesesHastaElProximo(int edad) =>
			Ultima != null && Ultima.EsHallazgo ? 3 : 12;

		public override Interpretacion Interpretar(int edad)
		{
			EvaluacionDePresencia hechos = Evaluar();

			// Sin ninguna prueba no hay nada que leer. Con UNA, la lectura es la de la
			// magnitud —que es exactamente lo que el dominio sabia decir antes de que
			// esta serie existiera— y el veredicto queda Indeterminado: hay un hallazgo,
			// pero todavia no hay historia. Las dos cosas son ciertas a la vez.
			if (!HayMediciones) return Interpretacion.SinSerie(hechos);
			if (!hechos.HaySerie)
				return new Interpretacion(Veredicto.Indeterminado,
					Ultima.Resultado.SeveridadDelHallazgo, Ultima.Resultado.ParaSonia, hechos);

			if (ReferenceEquals(hechos.Presencia, Presencia.NuncaAparecio))
				return new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					$"en las {hechos.CantidadDeMediciones} pruebas que te hiciste nunca "
					+ "apareció sangre en las heces", hechos);

			if (ReferenceEquals(hechos.Presencia, Presencia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultaMedica,
					$"la sangre en las heces apareció en las {hechos.PositivasSeguidas} pruebas "
					+ "seguidas: ya no es un resultado suelto", hechos);

			if (ReferenceEquals(hechos.Presencia, Presencia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando, Severidad.Seguimiento,
					"apareció sangre en las heces por primera vez; muchas veces se debe a "
					+ "algo común", hechos);

			if (ReferenceEquals(hechos.Presencia, Presencia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"una vez apareció sangre en las heces y en la prueba siguiente ya no "
					+ "estaba", hechos);

			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
				$"la sangre en las heces apareció {hechos.Positivas} veces y en la última "
				+ "ya no está", hechos);
		}
	}
}
