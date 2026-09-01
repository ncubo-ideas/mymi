using System;

namespace Examenes.Dominio
{
	// QUE LE PASO A UN HALLAZGO BINARIO A LO LARGO DEL TIEMPO. Es el par de Tendencia
	// para la forma 5, y son dos conjuntos y no uno porque no son la misma pregunta.
	//
	//   Tendencia  "para donde viene"   ascenso, descenso, estable
	//   Presencia  "aparecio o no"      no hay para donde: una prueba esta o no esta
	//
	// Meter los binarios en Tendencia obligaria a decir que un negativo que pasa a
	// positivo esta "en ascenso", que no significa nada: no hay mas positivo ni menos
	// positivo. Esa es la aplanadora de siempre, un piso mas abajo.
	//
	// POR QUE EL BINARIO NECESITA SERIE, que es lo que todo esto habilita. Una sola
	// prueba de sangre oculta en heces positiva dice "conviene repetirlo": la causa mas
	// frecuente es local y banal. DOS SEGUIDAS dicen otra cosa, y es una cosa que ninguna
	// medicion sola puede decir. Ese salto -de Seguimiento a ConsultaMedica- es la
	// capacidad entera, y hasta ahora el dominio no tenia donde ponerla.
	//
	// Y la simetrica, que es la buena noticia: un positivo que en la prueba siguiente no
	// vuelve a aparecer NO ES un hallazgo confirmado. Es el mismo argumento de §2 de la
	// reunion -"cuando lo vuelve a medir pasa otra vez a 3, ahi al medico esa segunda
	// lectura le sirve"- trasladado a los binarios.
	internal sealed class Presencia
	{
		// Menos de dos mediciones realizadas: no hay serie, y decir "nunca apareció"
		// con una sola prueba seria afirmar mas de lo que se midio.
		internal static readonly Presencia Indeterminada = new Presencia("indeterminada");

		// Todas las pruebas dieron negativas. Es una respuesta, no un vacio: "en las
		// cuatro veces que te lo hicieron nunca apareció" es informacion.
		internal static readonly Presencia NuncaAparecio = new Presencia("nunca apareció");

		// La ultima dio positiva y la anterior no. Todavia NADIE lo confirmo, y por eso
		// lo primero que pide es repetirse.
		internal static readonly Presencia Aparecio = new Presencia("apareció");

		// Dos o mas positivas SEGUIDAS al final de la serie. Ya se repitio y volvio a
		// salir: dejo de ser un dato suelto. Es el inhabitante que justifica que los
		// binarios tengan serie.
		internal static readonly Presencia Persiste = new Presencia("persiste");

		// Hubo UN positivo aislado y la prueba siguiente dio negativa. El hallazgo NO
		// ERA, y eso es buena noticia — el par binario de PicoNoConfirmado.
		internal static readonly Presencia NoConfirmado = new Presencia("no confirmado");

		// Estuvo presente de verdad -dos o mas veces- y despues dejo de estarlo. NO es lo
		// mismo que NoConfirmado: ahi nunca se confirmo nada, aca se confirmo y cedio.
		// Son dos por la misma razon que ProbableErrorDeMedicion y PicoNoConfirmado son
		// dos veredictos: la persona lee cosas distintas.
		internal static readonly Presencia Cedio = new Presencia("cedió");

		internal string Nombre { get; private set; }

		private Presencia(string nombre)
		{
			Nombre = nombre;
		}

		// Si HOY el hallazgo esta presente. Es lo unico que se puede preguntar sin mirar
		// la historia, y por eso es lo unico que la magnitud sola sabia contestar.
		internal bool EstaPresente =>
			ReferenceEquals(this, Aparecio) || ReferenceEquals(this, Persiste);

		// Si se repitio y volvio a salir. Es lo que separa un dato de un hecho.
		internal bool EstaConfirmado => ReferenceEquals(this, Persiste);

		// Un positivo que todavia nadie confirmo pide, antes que nada, repetirse. No es
		// la conducta completa -esa la decide cada analito- pero si es un hecho de la
		// serie: con un solo punto positivo no hay nada que confirmar.
		internal bool SugiereRepetirElExamen => ReferenceEquals(this, Aparecio);

		// Se movio hacia donde conviene: dejo de aparecer.
		internal bool EsMejora =>
			ReferenceEquals(this, NoConfirmado) || ReferenceEquals(this, Cedio);
	}
}
