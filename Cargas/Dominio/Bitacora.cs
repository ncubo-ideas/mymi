using System;

namespace Cargas.Dominio
{
	// Hasta donde leyo la app la narrativa del proveedor.
	//
	// UltimoEvento es lo que hace exacto el resume: vive en el journal, no en una
	// variable en memoria, y avanza DENTRO del mismo comando que aplica la carga.
	// Si la app se cae, se vuelve a pedir a partir de aca y no hay estado intermedio
	// posible - o la carga y su checkpoint entraron juntos, o no entro ninguno.
	//
	// Avanzar solo acepta ir hacia adelante. La guarda Check del comando ya rechaza
	// un evento repetido antes de escribir nada; esta validacion es la segunda linea,
	// para que el invariante viva en el dominio y no solo en el llamador.
	//
	// LA CREDENCIAL YA NO VIVE ACA. Hasta el 2026-09-03 el device key, el access token y
	// el refresh token eran estado de esta clase y se journalizaban, "porque las tres
	// piezas son con que sigo leyendo". Era una deuda declarada y se pago: un token no es
	// un hecho de la vida de la persona, es contexto de sesion de quien hospeda la app (el
	// browser, la consola, Gnothi). En el journal quedaba en claro, viajaba con cualquier
	// copia o traspaso del journal, y lo leia todo proceso que lee la carpeta. Ahora la
	// guarda el host fuera del journal -en la PWA, el storage del browser del origen- y el
	// bucle de refresh la recibe de ahi, no del diario.
	public class Bitacora
	{
		public long UltimoEvento { get; private set; }

		public void Avanzar(long evento)
		{
			if (evento <= UltimoEvento)
				throw new InvalidOperationException(
					$"El evento {evento} no avanza la bitacora: ya se habia procesado hasta {UltimoEvento}.");

			UltimoEvento = evento;
		}

		// COMPATIBILIDAD CON JOURNALS ANTERIORES, y nada mas. Los journals escritos antes
		// del 2026-09-03 contienen entradas `bitacora.RegistrarDevice(...)` y
		// `bitacora.GuardarCredencial(...)`; el replay las vuelve a ejecutar y necesita que
		// los metodos existan con la misma firma. Aceptan y descartan: no guardan nada, no
		// validan nada, no fallan. Ninguna API los emite ya. Lo que esos journals tienen en
		// claro sigue ahi -el journal es append-only-: la credencial vieja se revoca en el
		// proveedor, no se edita el historial.
		public void RegistrarDevice(string deviceKey)
		{
		}

		public void GuardarCredencial(string token, string refreshToken)
		{
		}
	}
}
