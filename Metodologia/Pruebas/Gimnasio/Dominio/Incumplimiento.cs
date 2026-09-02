using System;

namespace Gimnasio.Dominio
{
	// CUANTO SE APARTO SONIA DE LO QUE EL PLAN ESPERABA.
	//
	// Es la respuesta a "quiero saber el incumplimiento en la cantidad de dias que dije
	// que iba a ir". Y notese de donde salen las dos mitades: los dias esperados son
	// del PLAN -que lo arma el gimnasio- y los dias que fue tambien los tiene el
	// gimnasio. Ninguna mitad es aporte de Sonia; por eso esta pregunta sobrevive a la
	// decision de cero aporte.
	//
	// Es un carrier PLANO -tres numeros y nada anidado- porque el print del substrato
	// proyecta un solo nivel (regla 13).
	internal sealed class Incumplimiento
	{
		internal int DiasEsperados { get; }
		internal int DiasQueFue { get; }

		internal Incumplimiento(int diasEsperados, int diasQueFue)
		{
			if (diasEsperados < 0)
				throw new ArgumentOutOfRangeException(nameof(diasEsperados), diasEsperados,
					"No se pueden esperar dias negativos.");
			if (diasQueFue < 0)
				throw new ArgumentOutOfRangeException(nameof(diasQueFue), diasQueFue,
					"No se puede ir una cantidad negativa de dias.");

			DiasEsperados = diasEsperados;
			DiasQueFue = diasQueFue;
		}

		// Si fue de mas, no hay dias faltantes: el incumplimiento es cero, no negativo.
		internal int DiasQueFaltaron
		{
			get { return DiasEsperados > DiasQueFue ? DiasEsperados - DiasQueFue : 0; }
		}

		internal bool Cumplio { get { return DiasQueFaltaron == 0; } }
	}
}
