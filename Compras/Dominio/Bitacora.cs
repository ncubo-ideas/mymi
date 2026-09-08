using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Hasta donde leyo la app la narrativa del supermercado. Vive en el journal, no en
	/// memoria: es lo que hace exacto el resume tras una caida, porque avanza DENTRO del
	/// mismo comando que aplica la compra.
	/// </summary>
	// LA CREDENCIAL YA NO VIVE ACA. Hasta el 2026-09-03 el device key, el access token y el
	// refresh token eran estado de esta clase y se journalizaban, con una deuda declarada:
	// quedaban en claro en una carpeta del usuario que tambien lee el proceso MCP, y el
	// refresh es DURABLE. Se pago: un token no es un hecho de la vida de la persona, es
	// contexto de sesion de quien hospeda la app. Ademas viajaba con cualquier copia o
	// traspaso del journal. Ahora la guarda el host fuera del journal -en la PWA, el storage
	// del browser del origen- y el bucle de refresh la recibe de ahi, no del diario.
	public class Bitacora
	{
		internal long UltimoEvento { get; private set; }

		/// <summary>Solo hacia adelante. La guarda Check del comando ya rechaza el repetido; esta es la segunda linea.</summary>
		internal void Avanzar(long evento)
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

		internal void GuardarCredencial(string token, string refreshToken)
		{
		}
	}
}
