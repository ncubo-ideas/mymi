using System;

namespace Gimnasio.Dominio
{
	// COMO CAMBIO LO QUE LE PRESCRIBEN DE UN EJERCICIO, del primer plan que lo trae al
	// ultimo. Es la pregunta 2 del Preguntas.md reformulada por segunda vez: primero
	// murio "cuanto subi lo que levanto" -nadie lo registra- y quedo "cuanto subio lo que
	// me piden levantar"; ahora el papel no trae peso, y queda "como cambio lo que me
	// prescriben", leido sobre lo que el papel SI trae: volumen para fuerza, minutos para
	// cardio (N8).
	//
	// Carrier PLANO (regla 13): dos magnitudes, el cambio, en que se mide, y si subio.
	// Subir es la direccion de la progresion -N8, sin firmar-.
	internal sealed class Progresion
	{
		internal string NombreDelEjercicio { get; }
		internal string EnQueSeMide { get; }
		internal decimal Antes { get; }
		internal decimal Despues { get; }

		internal Progresion(string nombreDelEjercicio, Prescripcion primera, Prescripcion ultima)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombreDelEjercicio);
			ArgumentNullException.ThrowIfNull(primera);
			ArgumentNullException.ThrowIfNull(ultima);
			if (!primera.EsDeLaMismaFormaQue(ultima))
				throw new ArgumentException(
					$"'{nombreDelEjercicio}' paso de {primera.EnQueSeMide} a {ultima.EnQueSeMide}: dos formas "
					+ "de prescribir no se comparan. Preguntar SePuedeCompararLoPrescrito antes.", nameof(ultima));

			NombreDelEjercicio = nombreDelEjercicio;
			EnQueSeMide = primera.EnQueSeMide;
			Antes = primera.Magnitud;
			Despues = ultima.Magnitud;
		}

		internal decimal Cambio { get { return Despues - Antes; } }
		internal bool Subio { get { return Cambio > 0m; } }
		internal bool Bajo { get { return Cambio < 0m; } }
	}
}
