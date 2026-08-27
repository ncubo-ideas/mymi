using System;

namespace Cargas.Dominio
{
	// UNA PRUEBA BINARIA HECHA EN UNA FECHA: el punto de una serie de presencia.
	//
	// Es el equivalente de lo que en una serie continua es el propio ExamenDeSangre —ahi
	// el punto ya trae fecha, valor y los umbrales del analito—. Un binario no tiene
	// umbrales que traer, asi que su punto es apenas esto: cuando se hizo y que dio.
	//
	// POR QUE NO ES LecturaDeExamen. Aquella dice QUE se midio dentro de UN examen, y no
	// lleva fecha porque la fecha es del examen que la contiene. Un punto de serie SI la
	// necesita: la serie no tiene los cargas, tiene los puntos.
	//
	// SOLO ENTRAN LAS REALIZADAS. Una prueba que el laboratorio no corrio no es un punto:
	// no es un negativo, no es un positivo, no esta. Ver MagnitudBinaria.FueRealizada.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	public sealed class PruebaBinaria
	{
		public DateTime Fecha { get; private set; }

		public MagnitudBinaria Resultado { get; private set; }

		public PruebaBinaria(DateTime fecha, MagnitudBinaria resultado)
		{
			ArgumentNullException.ThrowIfNull(resultado);
			if (!resultado.FueRealizada)
				throw new ArgumentException(
					"Una prueba que el laboratorio no corrio no es un punto de la serie: "
					+ "no midio 'negativo', no midio nada.", nameof(resultado));

			Fecha = fecha;
			Resultado = resultado;
		}

		// Proyecciones planas, para que el print del DSL no navegue dos niveles y para
		// que el dibujo reciba el hecho y no el texto.
		public bool EsPositivo => Resultado.EsPositivo;
		public bool EsHallazgo => Resultado.EsHallazgo;

		// El resultado dicho con el vocabulario de SU analito: "positivo" en nitritos,
		// "reactivo" en una serologia. Es la razon de que la magnitud no sea un bool.
		public string Expresado => Resultado.Expresado;
	}
}
