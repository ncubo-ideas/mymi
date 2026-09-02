using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// LA PROCEDENCIA DE UN APORTE: quien puso el dato. Observado lo leyo Sonia (del
	/// tablero, o lo declaro ella); Estimado lo derivo el modulo (del historial, o de la
	/// forma de corte); Confirmado lo propuso el modulo y Sonia dijo que si. Es la
	/// tercera procedencia que la lamina 5 no contempla: un valor derivado no es un hecho
	/// de Sonia ni de la institucion, y de eso depende si puede alimentar un calculo.
	/// </summary>
	// LA REGLA DURA DEL §7.3, como propiedad del tipo y no como un if en el algoritmo:
	// un Estimado NO cierra un tramo ni produce veredicto de rendimiento. Si el odometro
	// se estimara del rendimiento proyectado y despues el rendimiento se calculara con
	// ese odometro, el resultado se confirmaria a si mismo. Confirmado si cuenta, porque
	// Sonia miro el numero y dijo que si.
	internal sealed class OrigenDeLectura
	{
		internal static readonly OrigenDeLectura Observado = new OrigenDeLectura("observado", cuenta: true);
		internal static readonly OrigenDeLectura Estimado = new OrigenDeLectura("estimado", cuenta: false);
		internal static readonly OrigenDeLectura Confirmado = new OrigenDeLectura("confirmado", cuenta: true);

		internal string Nombre { get; private set; }

		/// <summary>Si un dato con esta procedencia puede alimentar un calculo que se
		/// presenta como medicion. Un Estimado alimenta proyecciones -donde la
		/// aproximacion se declara- pero jamas un rendimiento.</summary>
		internal bool CuentaParaCalcular { get; private set; }

		private OrigenDeLectura(string nombre, bool cuenta)
		{
			Nombre = nombre;
			CuentaParaCalcular = cuenta;
		}

		internal static OrigenDeLectura PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Observado.Nombre) return Observado;
			if (normalizado == Estimado.Nombre) return Estimado;
			if (normalizado == Confirmado.Nombre) return Confirmado;

			throw new ErrorDeDominio(
				$"'{nombre}' no es un origen de lectura conocido. Los validos son: "
				+ $"{Observado.Nombre}, {Estimado.Nombre}, {Confirmado.Nombre}.");
		}
	}
}
