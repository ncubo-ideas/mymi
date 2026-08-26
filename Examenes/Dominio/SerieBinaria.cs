using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DE UN ANALITO BINARIO: negativo, negativo, positivo.
	//
	// POR QUE UN BINARIO NECESITA SERIE. Es la pregunta que este tipo contesta, y la
	// respuesta esta entera en un solo ejemplo del propio feedback:
	//
	//   UNA sangre oculta en heces positiva  -> "conviene repetirlo". La causa mas
	//                                           frecuente es local y banal.
	//   DOS SEGUIDAS                         -> ya no. Se repitio y volvio a salir.
	//
	// Los dos son "un binario positivo". La conducta no es la misma, y la diferencia no
	// esta en el resultado: esta en la HISTORIA. Una magnitud sola no puede decirlo —
	// SangreEnHeces.SeveridadDelHallazgo contesta lo mismo siempre, porque solo se ve a
	// si misma—. Es exactamente el argumento con el que este dominio existe, aplicado a
	// la unica forma a la que todavia no se lo habiamos aplicado.
	//
	// Y LA SIMETRICA, que es la buena noticia: un positivo que en la prueba siguiente no
	// vuelve a aparecer no es un hallazgo confirmado. Es §2 de la reunion —"cuando lo
	// vuelve a medir pasa otra vez a 3, ahi al medico esa segunda lectura le sirve"—
	// trasladado a los binarios.
	//
	// NO ORDENA Y NO RESTA. No hay tendencia, no hay velocidad, no hay lineas de
	// referencia que trazar: no existe "mas positivo". Por eso no cuelga de SerieContinua
	// sino al lado, y por eso Evaluar() devuelve EvaluacionDePresencia y no
	// EvaluacionDeTendencia.
	//
	// ESTA CLASE SE QUEDA CON LOS HECHOS. Contar positivas, contar las seguidas, saber
	// cual fue la ultima. QUE SIGNIFICAN lo contesta cada subclase en Interpretar(), y
	// por eso hay tres y no una: los nitritos, la sangre en la orina y la sangre en las
	// heces cuentan igual y concluyen distinto.
	public abstract class SerieBinaria : SerieDeAnalito
	{
		private readonly List<PruebaBinaria> pruebas = new List<PruebaBinaria>();

		protected SerieBinaria(Analito analito) : base(analito)
		{
		}

		public IReadOnlyList<PruebaBinaria> Pruebas => pruebas;

		public override int Cantidad => pruebas.Count;

		// Se inserta manteniendo el orden por fecha, igual que en la serie continua y por
		// la misma razon: el proveedor puede emitir un examen viejo despues de uno nuevo,
		// y en un binario una serie desordenada convierte un "cedió" en un "apareció".
		public void Agregar(PruebaBinaria prueba)
		{
			ArgumentNullException.ThrowIfNull(prueba);

			int posicion = pruebas.Count;
			while (posicion > 0 && pruebas[posicion - 1].Fecha > prueba.Fecha) posicion--;

			pruebas.Insert(posicion, prueba);
		}

		public PruebaBinaria Primera => pruebas.Count == 0 ? null : pruebas[0];

		public PruebaBinaria Ultima => pruebas.Count == 0 ? null : pruebas[pruebas.Count - 1];

		public override DateTime FechaDeLaUltima => Ultima == null ? default : Ultima.Fecha;

		// Para un binario, "hay algo que no deberia estar" es literal: que el hallazgo
		// este presente hoy. No hace falta la edad, y eso esta bien: la firma es una sola
		// para que el Expediente pregunte sin saber que forma tiene enfrente.
		public override bool UltimaEsHallazgo(int edad) => Ultima != null && Ultima.EsHallazgo;

		// UNA TIRA DE PUNTOS EN EL TIEMPO, no una curva. Un binario no tiene eje vertical:
		// tiene presencias y ausencias en fechas. Que sea su propia forma —y no la
		// Afirmacion que usaban antes— es lo que le permite a la pantalla dibujar la
		// historia en vez de repetir el ultimo resultado.
		public override FormaDeLectura Forma => FormaDeLectura.PresenciaEnElTiempo;

		// NINGUNA. No hay linea que trazar sobre un positivo: no existe un umbral, ni una
		// meta, ni un piso. Devolver una lista vacia es la respuesta correcta, y es la
		// razon de que las referencias sean una lista plana y no un record con campos que
		// habria que llenar con ceros.
		public override IReadOnlyList<ReferenciaDeLectura> Referencias(int edad) =>
			new List<ReferenciaDeLectura>();

		// LOS HECHOS DE LA SERIE. Cuantas pruebas, cuantas dieron positivas, y —la que
		// importa— cuantas positivas hay SEGUIDAS al final.
		//
		// "Seguidas al final" y no "en total" es deliberado: negativo, positivo, negativo,
		// positivo son dos positivos y no son una persistencia. Lo que cambia la conducta
		// es que se haya repetido y haya vuelto a salir, no que haya salido dos veces
		// alguna vez.
		public EvaluacionDePresencia Evaluar()
		{
			string ultimoResultado = Ultima == null ? "" : Ultima.Expresado;

			if (pruebas.Count < 2)
				return EvaluacionDePresencia.SinSerie(Analito, pruebas.Count, ultimoResultado);

			int positivas = 0;
			foreach (PruebaBinaria prueba in pruebas)
				if (prueba.EsPositivo) positivas++;

			int seguidas = 0;
			for (int i = pruebas.Count - 1; i >= 0 && pruebas[i].EsPositivo; i--) seguidas++;

			int dias = (int)(Ultima.Fecha.Date - Primera.Fecha.Date).TotalDays;

			Presencia presencia = Cual(positivas, seguidas);

			return new EvaluacionDePresencia(Analito, presencia, pruebas.Count,
				positivas, seguidas, dias, ultimoResultado);
		}

		// Las cinco lecturas posibles de una serie de dos o mas pruebas. Es la unica
		// regla generica que hay aca, y lo es porque contar no depende del analito: lo que
		// depende del analito es QUE HACER con cada una, y eso vive en Interpretar().
		private static Presencia Cual(int positivas, int seguidas)
		{
			if (positivas == 0) return Presencia.NuncaAparecio;

			// La ultima dio positiva: la pregunta es si ya se habia repetido.
			if (seguidas > 0) return seguidas >= 2 ? Presencia.Persiste : Presencia.Aparecio;

			// La ultima dio negativa, y antes hubo positivos. Un positivo aislado que no
			// se repitio NO ERA; dos o mas que despues se apagaron si fueron y cedieron.
			return positivas == 1 ? Presencia.NoConfirmado : Presencia.Cedio;
		}
	}
}
