using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// QUE HACER, Y CUANDO. Todo consejo lleva plazo, y el vocabulario es CERRADO: el
	/// constructor es privado y los que-hacer son fabricas con nombre, asi ni una
	/// pantalla ni una llamada pueden colar prosa libre. Lo unico que varia es la
	/// combinacion que + plazo.
	/// </summary>
	// Nada de receta ciega: ninguna de estas acciones es "revise la presion de las
	// llantas". Piden datos o piden mirar, siempre sobre LOS hechos de Sonia.
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

		/// <summary>La accion dicha entera: "anotá el kilometraje en tu próxima carga".</summary>
		public string Legible => $"{Que} {Plazo.ParaMostrar}";

		// -------------------------------------------------------- el conjunto cerrado

		/// <summary>El dato que solo Sonia tiene, y que desbloquea el calculo.</summary>
		public static Accion AnotarElKilometraje(Plazo plazo) =>
			new Accion("anotá el kilometraje", plazo);

		/// <summary>El visto bueno a lo que la inferencia propuso.</summary>
		public static Accion ConfirmarElTanque(Plazo plazo) =>
			new Accion("confirmá si el tanque quedó lleno", plazo);

		/// <summary>La salida del dato sospechoso: el problema es de datos, no del carro.</summary>
		public static Accion RegistrarLaCargaQueFalto(Plazo plazo) =>
			new Accion("registrá la carga que faltó", plazo);

		/// <summary>Cuando la caida es real y sostenida: que lo vea quien sabe, con los
		/// hechos en la mano.</summary>
		public static Accion RevisarloConTuTaller(Plazo plazo) =>
			new Accion("revisalo con tu taller", plazo);

		/// <summary>Cuando el total no cuadra con precio por volumen: el reclamo es con
		/// la estacion, y el respaldo es el numero de transaccion.</summary>
		public static Accion MostrarleEstoATuEstacion(Plazo plazo) =>
			new Accion("mostrále esto a tu estación", plazo);
	}
}
