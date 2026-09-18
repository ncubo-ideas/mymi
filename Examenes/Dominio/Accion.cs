using System;

namespace Examenes.Dominio
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
	internal sealed class Accion
	{
		/// <summary>
		/// EL CODIGO de que hay que hacer: identidad, y lo unico que cruza el wire. Como se le dice a
		/// una persona es texto de presentacion, y lo busca con este codigo la capa que redacta, en el
		/// idioma que le pidieron.
		/// </summary>
		internal string Clave { get; private set; }

		internal Plazo Plazo { get; private set; }

		private Accion(string clave, Plazo plazo)
		{
			ArgumentNullException.ThrowIfNull(plazo);

			Clave = clave;
			Plazo = plazo;
		}

		/// <summary>El codigo del plazo, para que quien redacte arme la accion entera sin abrir el plazo.</summary>
		internal string ClaveDelPlazo => Plazo.Clave;

		// -------------------------------------------------------- el conjunto cerrado

		// Hay algo que un medico tiene que VER: un valor, una serie, un salto.
		internal static Accion MostrarleEstoATuMedico(Plazo plazo) =>
			new Accion("mostrarle-esto-a-tu-medico", plazo);

		// Hay algo para CONVERSAR, sin que corra: la direccion, los habitos, la meta.
		internal static Accion ConversarloConTuMedico(Plazo plazo) =>
			new Accion("conversarlo-con-tu-medico", plazo);

		// El hallazgo puede ser real o puede ser la medicion: lo que corresponde es
		// volver a medir, no alarmarse.
		internal static Accion RepetirElExamen(Plazo plazo) =>
			new Accion("repetir-el-examen", plazo);

		// No espera. Es la accion del rojo reservado.
		internal static Accion BuscarAtencionMedica(Plazo plazo) =>
			new Accion("buscar-atencion-medica", plazo);
	}
}
