using System;

namespace Cargas.Dominio
{
	// QUE HACER, Y CUANDO. Todo consejo lleva plazo: es el requisito del punto 5 de las
	// especificaciones — "«consulte a su médico» siempre con plazo".
	//
	// Antes el que-hacer era un texto fijo colgado de la Severidad, asi que dos analitos
	// con la misma severidad decian exactamente lo mismo: el PSA subiendo y el colesterol
	// subiendo compartian frase, que es la "misma tabla" de Alvaro un piso mas arriba.
	// El plazo depende del analito, de la magnitud del cambio y de la severidad, asi que
	// la accion la elige quien interpreta —el analito— y no el conjunto de severidades.
	//
	// EL VOCABULARIO SIGUE SIENDO CERRADO. El constructor es privado y los cuatro "que"
	// son fabricas con nombre: la pantalla no puede inventar una accion nueva, y una
	// llamada del dominio tampoco puede colar prosa libre. Lo unico que varia por
	// analito es la combinacion que + plazo.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	public sealed class Accion
	{
		// Que hacer, ya escrito como se le muestra a la persona, con acentos.
		public string Que { get; private set; }

		public Plazo Plazo { get; private set; }

		private Accion(string que, Plazo plazo)
		{
			ArgumentNullException.ThrowIfNull(plazo);

			Que = que;
			Plazo = plazo;
		}

		// La accion dicha entera: "mostráselo a tu médico este mes". Es lo unico que una
		// pantalla necesita imprimir, y viene armada para que no la componga ella.
		public string Legible => $"{Que} {Plazo.ParaMostrar}";

		// -------------------------------------------------------- el conjunto cerrado

		// Hay algo que un medico tiene que VER: un valor, una serie, un salto.
		public static Accion MostrarleEstoATuMedico(Plazo plazo) =>
			new Accion("mostrále esto a tu médico", plazo);

		// Hay algo para CONVERSAR, sin que corra: la direccion, los habitos, la meta.
		public static Accion ConversarloConTuMedico(Plazo plazo) =>
			new Accion("conversalo con tu médico", plazo);

		// El hallazgo puede ser real o puede ser la medicion: lo que corresponde es
		// volver a medir, no alarmarse.
		public static Accion RepetirElExamen(Plazo plazo) =>
			new Accion("repetí el examen", plazo);

		// No espera. Es la accion del rojo reservado.
		public static Accion BuscarAtencionMedica(Plazo plazo) =>
			new Accion("buscá atención médica", plazo);
	}
}
