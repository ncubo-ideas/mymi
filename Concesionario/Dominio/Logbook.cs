using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Hasta donde leyo la app la narrativa del concesionario. Es la SEGUNDA raiz global del DSL,
	/// al lado de <see cref="MaintenanceSchedule"/> y nunca adentro: el punto de control no es un
	/// hecho de mantenimiento.
	/// </summary>
	// LA CREDENCIAL YA NO VIVE ACA. Hasta el 2026-09-03 la clave de device, el access token y el
	// refresh token eran estado de esta clase y se journalizaban, con sus tres lecturas guardadas.
	// Se sacaron: un token no es un hecho de la vida de la persona, es contexto de sesion de quien
	// hospeda la app. En el journal quedaba en claro, viajaba con cualquier copia del expediente, y
	// lo leia todo proceso que lee la carpeta. Ahora la guarda el host fuera del journal y el bucle
	// de refresh la recibe de ahi, no del diario.
	internal sealed class Logbook
	{
		private long _lastEvent;

		/// <summary>
		/// El constructor ES la creacion: una bitacora que no leyo nada todavia. Hasta donde leyo
		/// llega despues, por su comando.
		/// </summary>
		internal Logbook()
		{
		}

		/// <summary>
		/// El numero del ultimo evento aplicado. TOTAL: cero es "todavia no se leyo nada", que es
		/// un valor legal de esta escala y no una ausencia -- los eventos se numeran desde uno.
		/// </summary>
		internal long LastEvent()
		{
			return _lastEvent;
		}

		/// <summary>
		/// Avanza el punto de control. Solo hacia adelante: un evento ya aplicado no vuelve a
		/// entrar, y esa es la idempotencia de la ingesta vista desde el dominio.
		/// </summary>
		internal void Advance(long eventNumber)
		{
			if (eventNumber <= _lastEvent)
				throw new DomainException(
					$"El evento {eventNumber} no avanza la bitacora: ya se habia procesado hasta "
					+ $"{_lastEvent}. Un evento redelivered se descarta, no se re-aplica.");

			_lastEvent = eventNumber;
		}

		// COMPATIBILIDAD CON JOURNALS ANTERIORES, y nada mas. Los journals escritos antes del
		// 2026-09-03 contienen entradas `logbook.RegisterDevice(...)` y `logbook.StoreCredential(...)`;
		// el replay las vuelve a ejecutar y necesita que los metodos existan con la misma firma.
		// Aceptan y descartan: no guardan nada, no validan nada, no fallan -- si validaran, un
		// journal viejo con una clave que hoy pareciera invalida no se podria ni abrir. Ninguna
		// puerta los emite ya. Lo que esos journals tienen en claro sigue ahi -- el journal es
		// append-only --: la credencial vieja se revoca en el concesionario, no se edita el historial.
		/// <summary>Acepta y descarta: existe para que el replay de un journal viejo no falle.</summary>
		internal void RegisterDevice(string deviceKey)
		{
		}

		/// <summary>Acepta y descarta: existe para que el replay de un journal viejo no falle.</summary>
		internal void StoreCredential(string token, string refreshToken)
		{
		}
	}
}
