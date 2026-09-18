using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DE UN ANALITO NOMINAL: ambar, ambar, oscuro.
	//
	// POR QUE UN NOMINAL NECESITA SERIE. Es la pregunta que este tipo contesta, y la
	// respuesta esta entera en tres casos del mismo color:
	//
	//   UN oscuro suelto              -> conversalo en la proxima cita
	//   DOS oscuros seguidos          -> ya no espera la proxima cita
	//   un oscuro y despues un ambar  -> no era, y decirlo es la buena noticia
	//
	// Los tres son "un color inesperado". La conducta no es la misma, y la diferencia no
	// esta en el resultado: esta en la HISTORIA. Una magnitud sola no puede decirlo —
	// ColorDeOrina.SeveridadDelHallazgo contesta lo mismo siempre, porque solo se ve a si
	// misma—. Es el argumento con el que existe SerieBinaria, aplicado a la ultima forma
	// que no lo tenia.
	//
	// Y NO ES UNA TENDENCIA. Sigue siendo cierto lo que decia AnalitoNominal: preguntarle a
	// un color si "viene subiendo" no es una pregunta. No hay eje, no hay velocidad, no hay
	// lineas de referencia que trazar. Lo que hay es si salio de lo esperable y si volvio.
	// Por eso no cuelga de SerieContinua sino al lado, y por eso Evaluar() devuelve
	// EvaluacionDeNominal.
	//
	// ESTA CLASE SE QUEDA CON LOS HECHOS. Contar las inesperadas, contar las seguidas,
	// saber cual fue la ultima. QUE SIGNIFICAN lo contesta cada subclase en su Leer(), y
	// por eso hay dos y no una: el color de la orina y el de las heces cuentan igual y
	// concluyen distinto, con vocabularios que no se pueden compartir.
	internal abstract class SerieNominal : SerieDeAnalito
	{
		private readonly List<ObservacionNominal> observaciones = new List<ObservacionNominal>();

		protected SerieNominal(Analito analito) : base(analito)
		{
		}

		internal IReadOnlyList<ObservacionNominal> Observaciones => observaciones;

		internal override int Cantidad => observaciones.Count;

		// Se inserta manteniendo el orden por fecha, igual que en las otras dos series y
		// por la misma razon: el proveedor puede emitir un examen viejo despues de uno
		// nuevo —el guion de la demo lo hace a proposito— y desordenada una serie convierte
		// un "volvió" en un "apareció".
		internal void Agregar(ObservacionNominal observacion)
		{
			ArgumentNullException.ThrowIfNull(observacion);

			int posicion = observaciones.Count;
			while (posicion > 0 && observaciones[posicion - 1].Fecha > observacion.Fecha)
				posicion--;

			observaciones.Insert(posicion, observacion);
		}

		internal ObservacionNominal Primera => observaciones.Count == 0 ? null : observaciones[0];

		internal ObservacionNominal Ultima =>
			observaciones.Count == 0 ? null : observaciones[observaciones.Count - 1];

		internal override DateTime FechaDeLaUltima => Ultima == null ? default : Ultima.Fecha;

		// Para un nominal, "hay algo que no deberia estar" es que la categoria de HOY no
		// sea de las esperables. No hace falta nada de la persona —ni la edad ni el sexo— y
		// eso esta bien: la firma es una sola para que el Expediente pregunte sin saber que
		// forma tiene enfrente.
		internal override bool UltimaEsHallazgo(Paciente paciente) =>
			Ultima != null && Ultima.EsHallazgo;

		internal override FormaDeLectura Forma => FormaDeLectura.CategoriaEnElTiempo;

		// NINGUNA. No hay linea que trazar sobre un color: no existe un umbral, ni una
		// meta, ni un piso, ni un eje sobre el que ponerlos. Devolver una lista vacia es la
		// respuesta correcta, y es la razon de que las referencias sean una lista plana y
		// no un record con campos que habria que llenar con ceros.
		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente) =>
			new List<ReferenciaDeLectura>();

		// SeSabeLeerPara no se overridea: un color no se compara contra un rango que
		// dependa del sexo. Hereda el true de SerieDeAnalito.

		// LOS HECHOS DE LA SERIE. Cuantas observaciones, cuantas cayeron fuera de lo
		// esperable, y —las que importan— cuantas inesperadas hay SEGUIDAS al final y si
		// son la misma categoria.
		//
		// "Seguidas al final" y no "en total" es deliberado, igual que en la serie binaria:
		// ambar, oscuro, ambar, oscuro son dos inesperadas y no son una persistencia. Lo
		// que cambia la conducta es que se haya repetido, no que haya salido dos veces
		// alguna vez.
		internal EvaluacionDeNominal Evaluar()
		{
			string ultimaCategoria = Ultima == null ? "" : Ultima.ClaveDelResultado;
			bool ultimaEsEsperable = Ultima == null || Ultima.EsEsperable;

			if (observaciones.Count < 2)
				return EvaluacionDeNominal.SinSerie(Analito, observaciones.Count,
					ultimaCategoria, ultimaEsEsperable);

			int noEsperables = 0;
			foreach (ObservacionNominal observacion in observaciones)
				if (!observacion.EsEsperable) noEsperables++;

			int seguidas = 0;
			for (int i = observaciones.Count - 1; i >= 0 && !observaciones[i].EsEsperable; i--)
				seguidas++;

			// SI LAS SEGUIDAS SON LA MISMA CATEGORIA. Es lo que un binario no necesita
			// preguntarse: un negro seguido de un palido son dos inesperadas al hilo y NO
			// son un mismo hallazgo persistiendo, asi que la frase no puede ser la misma.
			bool laMisma = seguidas >= 2;
			for (int i = observaciones.Count - seguidas; laMisma && i < observaciones.Count; i++)
				laMisma = observaciones[i].EsLaMisma(Ultima);

			int dias = (int)(Ultima.Fecha.Date - Primera.Fecha.Date).TotalDays;

			Constancia constancia = Cual(noEsperables, seguidas);

			return new EvaluacionDeNominal(Analito, constancia, observaciones.Count,
				ultimaCategoria, ultimaEsEsperable, noEsperables, seguidas, laMisma, dias);
		}

		// Las cinco lecturas posibles de una serie de dos o mas observaciones. Es la unica
		// regla generica que hay aca, y lo es porque contar no depende del analito: lo que
		// depende del analito es QUE HACER con cada una, y eso vive en Leer().
		//
		// Es la gemela de SerieBinaria.Cual y estan duplicadas a proposito: ver Constancia
		// para por que los dos conjuntos cerrados no son uno.
		private static Constancia Cual(int noEsperables, int seguidas)
		{
			if (noEsperables == 0) return Constancia.SiempreEsperable;

			// La ultima salio fuera de lo esperable: la pregunta es si ya se habia
			// repetido.
			if (seguidas > 0) return seguidas >= 2 ? Constancia.Persiste : Constancia.Aparecio;

			// La ultima volvio a lo esperable, y antes hubo inesperadas. Una aislada que no
			// se repitio NO ERA; dos o mas que despues volvieron si fueron y volvieron.
			return noEsperables == 1 ? Constancia.NoConfirmado : Constancia.Volvio;
		}
	}
}
