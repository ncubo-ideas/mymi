using System;

namespace Examenes.Dominio
{
	// LA SERIE DE LA SANGRE OCULTA EN HECES. Es la que justifica que los binarios tengan
	// serie, porque es la unica del demo donde la HISTORIA cambia la conducta:
	//
	//   una vez positiva      -> Vigilar          "repetí el examen este mes"
	//   dos veces SEGUIDAS    -> ConsultePronto   ya se repitio y volvio a salir
	//
	// SangreEnHeces.SeveridadDelHallazgo contesta Vigilar siempre, y esta bien: una
	// magnitud solo se ve a si misma. La que puede distinguir un dato suelto de un hecho
	// confirmado es la serie, y no lo hace con un umbral generico sino con el criterio de
	// ESTE analito: en otro, dos positivos no significarian lo mismo.
	//
	// Se lee al lado de SerieDeSangreEnOrina, igual que sus magnitudes: misma forma,
	// mismo conteo, conclusiones distintas. La diferencia esta abajo, en Interpretar, y
	// es la unica parte que importa.
	internal sealed class SerieDeSangreEnHeces : SerieBinaria
	{
		internal SerieDeSangreEnHeces() : base(Analito.SangreEnLasHeces) { }

		// Anual como tamizaje, y a los tres meses si aparecio: lo que hay que saber es si
		// se repite, y eso no se ve esperando un ano.
		//
		// ⚠️ Orientativa. La periodicidad real depende de la edad y de los antecedentes,
		// y conviene que la revise alguien con criterio de laboratorio.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			Ultima != null && Ultima.EsHallazgo ? 3 : 12;

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDePresencia hechos = Evaluar();

			// Sin ninguna prueba no hay nada que leer. Con UNA, la lectura es la de la
			// magnitud —que es exactamente lo que el dominio sabia decir antes de que
			// esta serie existiera— y el veredicto queda Indeterminado: hay un hallazgo,
			// pero todavia no hay historia. Las dos cosas son ciertas a la vez.
			if (!HayMediciones) return Interpretacion.SinSerie(hechos);
			if (!hechos.HaySerie)
				return new Interpretacion(Veredicto.Indeterminado,
					Ultima.Resultado.SeveridadDelHallazgo, Ultima.Resultado.ClaveDeLaFrase, hechos,
					Ultima.Resultado.AccionDelHallazgo);

			if (ReferenceEquals(hechos.Presencia, Presencia.NuncaAparecio))
				return new Interpretacion(Veredicto.Estable, Severidad.Informativo,
					"lectura.sangre-en-heces.nunca-aparecio", hechos);

			if (ReferenceEquals(hechos.Presencia, Presencia.Persiste))
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"lectura.sangre-en-heces.persiste", hechos,
					Accion.MostrarleEstoATuMedico(Plazo.EsteMes));

			if (ReferenceEquals(hechos.Presencia, Presencia.Aparecio))
				return new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"lectura.sangre-en-heces.aparecio", hechos, Accion.RepetirElExamen(Plazo.EsteMes));

			if (ReferenceEquals(hechos.Presencia, Presencia.NoConfirmado))
				return new Interpretacion(Veredicto.HallazgoNoConfirmado, Severidad.Informativo,
					"lectura.sangre-en-heces.no-confirmado", hechos);

			return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.sangre-en-heces.cedio", hechos);
		}
	}
}
