using System;

namespace Gimnasio.Dominio
{
	// LO QUE SONIA QUIERE DE UNA MEDIDA.
	//
	// Dos partes, y la segunda es opcional a proposito:
	//
	//   LA DIRECCION siempre esta. Es lo que le da signo a un numero -juicio 1-.
	//   EL VALOR AL QUE QUIERE LLEGAR puede no estar.
	//
	// HUECO 4: el Preguntas.md pide contestar "que me falta para cumplir mi objetivo",
	// y eso necesita un NUMERO al que llegar. Pero en ninguna parte se dijo que Sonia
	// declare ese numero: se dijo que declara un objetivo, que es una direccion.
	// Sin valor no hay "cuanto falta", y decir "te falta bajar" no contesta la pregunta.
	//
	// Se resolvio dejando el valor OPCIONAL y diciendolo cuando no esta, en vez de
	// inventar una meta. Es la misma salida que tomo Presupuesto con el rendimiento de
	// un bien: sin valor no hay porcentaje, y se dice.
	internal sealed class Meta
	{
		internal string NombreDeLaMedida { get; }
		internal Direccion Direccion { get; }
		internal bool TieneValorDeseado { get; }
		internal decimal ValorDeseado { get; }

		private Meta(string nombreDeLaMedida, Direccion direccion, bool tieneValor, decimal valorDeseado)
		{
			NombreDeLaMedida = nombreDeLaMedida;
			Direccion = direccion;
			TieneValorDeseado = tieneValor;
			ValorDeseado = valorDeseado;
		}

		internal static Meta SoloDireccion(string nombreDeLaMedida, Direccion direccion)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombreDeLaMedida);
			ArgumentNullException.ThrowIfNull(direccion);
			return new Meta(nombreDeLaMedida, direccion, false, 0m);
		}

		internal static Meta ConValor(string nombreDeLaMedida, Direccion direccion, decimal valorDeseado)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombreDeLaMedida);
			ArgumentNullException.ThrowIfNull(direccion);
			if (valorDeseado <= 0)
				throw new ArgumentOutOfRangeException(nameof(valorDeseado), valorDeseado,
					"Un valor deseado tiene que ser positivo.");
			return new Meta(nombreDeLaMedida, direccion, true, valorDeseado);
		}

		// Cuanto falta, en las unidades de la medida. Solo tiene sentido con valor.
		internal decimal CuantoFalta(decimal valorActual)
		{
			if (!TieneValorDeseado)
				throw new InvalidOperationException(
					$"La meta de '{NombreDeLaMedida}' no declara a que valor llegar, asi que no hay "
					+ "cuanto falta. Preguntar TieneValorDeseado antes.");
			return Math.Abs(ValorDeseado - valorActual);
		}

		internal bool YaSeCumplio(decimal valorActual)
		{
			if (!TieneValorDeseado) return false;
			if (Direccion.EsSubir()) return valorActual >= ValorDeseado;
			if (Direccion.EsBajar()) return valorActual <= ValorDeseado;
			return valorActual == ValorDeseado;
		}
	}
}
