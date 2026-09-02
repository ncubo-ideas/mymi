using System;

namespace Gimnasio.Dominio
{
	// EL MOVIMIENTO: se mueve como Sonia queria? Es el OTRO juicio sobre el mismo
	// numero, y no se deduce de la Lectura.
	//
	// El Preguntas.md insiste: un valor puede estar ALTO y MEJORANDO, u otro NORMAL y
	// yendo hacia donde ella no quiere. Una app que diga solo una de las dos cosas
	// miente por omision en la mitad de los casos. Por eso hay dos tipos y no uno.
	//
	// El orden en que se pregunta importa y esta fijo:
	//   1. es creible el cambio?   -> si no, no hay tendencia que juzgar
	//   2. supera el ruido?        -> si no, no hay noticia
	//   3. va hacia donde queria?  -> recien aca aparece el signo
	//
	// Invertirlo daria avisos dentro del margen de error del aparato, que es lo que
	// entrena a la persona a ignorar los avisos.
	internal sealed class Tendencia
	{
		private readonly string estado;

		internal decimal Cambio { get; }
		internal string NombreDeLaMedida { get; }

		private Tendencia(string estado, decimal cambio, string nombreDeLaMedida)
		{
			this.estado = estado;
			Cambio = cambio;
			NombreDeLaMedida = nombreDeLaMedida;
		}

		internal static Tendencia Entre(Medida anterior, Medida actual, Direccion direccion,
			Referencia referencia)
		{
			ArgumentNullException.ThrowIfNull(anterior);
			ArgumentNullException.ThrowIfNull(actual);
			ArgumentNullException.ThrowIfNull(direccion);
			ArgumentNullException.ThrowIfNull(referencia);

			if (!anterior.EsDeLaMismaClaseQue(actual))
				throw new ArgumentException(
					$"No se puede comparar '{anterior.Nombre}' contra '{actual.Nombre}': "
					+ "una tendencia es de una medida consigo misma.", nameof(actual));

			decimal cambio = actual.Valor - anterior.Valor;

			if (referencia.NoEsCreible(cambio))
				return new Tendencia("no-creible", cambio, actual.Nombre);

			if (referencia.EsRuido(cambio))
				return new Tendencia("sin-cambio", cambio, actual.Nombre);

			return new Tendencia(direccion.Favorece(cambio) ? "mejora" : "empeora", cambio, actual.Nombre);
		}

		internal bool EsCreible() { return estado != "no-creible"; }
		internal bool HuboCambio() { return estado == "mejora" || estado == "empeora"; }
		internal bool Mejora() { return estado == "mejora"; }
		internal bool Empeora() { return estado == "empeora"; }

		// Magnitud mas signo, que es como el Preguntas.md define la urgencia.
		// Un cambio que no es creible NO avisa: no se sabe si paso.
		internal Urgencia Urgencia()
		{
			if (!HuboCambio()) return Dominio.Urgencia.Ninguna();
			return Mejora() ? Dominio.Urgencia.Positiva() : Dominio.Urgencia.Negativa();
		}

		public override string ToString() { return estado; }
	}
}
