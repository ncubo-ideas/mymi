using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// QUE HACER, Y CUANDO. Todo consejo lleva plazo, y el vocabulario es CERRADO: el
	/// constructor es privado y los que-hacer son fabricas con nombre, asi ni una
	/// pantalla ni una llamada pueden colar prosa libre. Lo unico que varia es la
	/// combinacion que + plazo.
	/// <para>
	/// Nada de receta ciega: ninguna de estas acciones es revisar la presion de las llantas.
	/// Piden datos o piden mirar, siempre sobre LOS hechos de Sonia.
	/// </para>
	/// </summary>
	internal sealed class Accion
	{
		/// <summary>
		/// EL CODIGO de que hacer: identidad, y lo unico que cruza el wire. Como se le dice a la
		/// persona lo busca con este codigo la capa que redacta, en el idioma que le pidieron.
		/// </summary>
		internal string Clave { get; private set; }

		internal Plazo Plazo { get; private set; }

		private Accion(string clave, Plazo plazo)
		{
			ArgumentNullException.ThrowIfNull(plazo);

			Clave = clave;
			Plazo = plazo;
		}

		/// <summary>El codigo del plazo, aplanado para que el print lo alcance en un solo nivel.</summary>
		internal string ClaveDelPlazo => Plazo.Clave;

		/// <summary>El dato que solo Sonia tiene, y que desbloquea el calculo.</summary>
		internal static Accion AnotarElKilometraje(Plazo plazo) =>
			new Accion("anotar-el-kilometraje", plazo);

		/// <summary>El visto bueno a lo que la inferencia propuso.</summary>
		internal static Accion ConfirmarElTanque(Plazo plazo) =>
			new Accion("confirmar-el-tanque", plazo);

		/// <summary>La salida del dato sospechoso: el problema es de datos, no del carro.</summary>
		internal static Accion RegistrarLaCargaQueFalto(Plazo plazo) =>
			new Accion("registrar-la-carga-que-falto", plazo);

		/// <summary>Cuando la caida es real y sostenida: que lo vea quien sabe, con los
		/// hechos en la mano.</summary>
		internal static Accion RevisarloConTuTaller(Plazo plazo) =>
			new Accion("revisarlo-con-tu-taller", plazo);

		/// <summary>Cuando el total no cuadra con precio por volumen: el reclamo es con
		/// la estacion, y el respaldo es el numero de transaccion.</summary>
		internal static Accion MostrarleEstoATuEstacion(Plazo plazo) =>
			new Accion("mostrarle-esto-a-tu-estacion", plazo);
	}
}
