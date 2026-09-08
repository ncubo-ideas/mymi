using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Hasta donde leyo la app la narrativa del concesionario, y con que credencial la sigue
	/// leyendo. Es la SEGUNDA raiz global del DSL, al lado de <see cref="MaintenanceSchedule"/> y
	/// nunca adentro: ni el punto de control ni la credencial son hechos de mantenimiento.
	/// </summary>
	internal sealed class Logbook
	{
		private long _lastEvent;
		private string _deviceKey = "";
		private string _token = "";
		private string _refreshToken = "";

		/// <summary>
		/// El constructor ES la creacion: una bitacora que no leyo nada todavia y no tiene con
		/// que leer. Las dos cosas llegan despues, cada una por su comando.
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

		/// <summary>Si esta instalacion ya se registro ante el concesionario. Total.</summary>
		internal bool HasADevice()
		{
			return _deviceKey.Length > 0;
		}

		/// <summary>
		/// La clave con que el concesionario conoce a ESTA instalacion. GUARDADA: sin registro no
		/// hay clave vacia, hay ausencia de clave.
		/// </summary>
		internal string DeviceKey()
		{
			if (!HasADevice())
				throw new DomainException(
					"Esta instalacion no esta registrada ante el concesionario: pregunte "
					+ "HasADevice antes. Una clave vacia se veria igual que una clave.");

			return _deviceKey;
		}

		/// <summary>
		/// Registra esta instalacion, UNA sola vez. Re-registrarla con otra clave dejaria huerfana
		/// la credencial vieja, porque el concesionario veria otro device.
		/// </summary>
		internal void RegisterDevice(string deviceKey)
		{
			if (string.IsNullOrWhiteSpace(deviceKey))
				throw new DomainException(
					"Una instalacion sin clave de device no se puede registrar: la clave es lo "
					+ "que hace que el concesionario le emita una credencial a ESTA instalacion y "
					+ "no a cualquiera.");

			if (HasADevice() && _deviceKey != deviceKey)
				throw new DomainException(
					$"Esta instalacion ya esta registrada como device '{_deviceKey}'; no se puede "
					+ $"cambiar a '{deviceKey}'.");

			_deviceKey = deviceKey;
		}

		/// <summary>Si hay credencial guardada con que leer la narrativa. Total.</summary>
		internal bool HasACredential()
		{
			return _token.Length > 0;
		}

		/// <summary>
		/// El access token con que se leen los eventos. GUARDADA, por lo mismo que
		/// <see cref="DeviceKey"/>.
		/// </summary>
		internal string Token()
		{
			if (!HasACredential())
				throw new DomainException(
					"Esta bitacora no tiene credencial guardada: pregunte HasACredential antes. "
					+ "Un token vacio se veria igual que un token.");

			return _token;
		}

		/// <summary>
		/// Si la credencial se puede rotar sola. Total: hay proveedores que solo dan el access
		/// token, y entonces la instalacion pide una credencial nueva con su clave de device.
		/// </summary>
		internal bool CanRotate()
		{
			return _refreshToken.Length > 0;
		}

		/// <summary>El refresh token, durable y rotatorio. GUARDADA.</summary>
		internal string RefreshToken()
		{
			if (!CanRotate())
				throw new DomainException(
					"Esta bitacora no tiene refresh token: pregunte CanRotate antes. El access "
					+ "token es corto, y sin refresh la instalacion pide credencial nueva.");

			return _refreshToken;
		}

		/// <summary>
		/// Guarda la credencial con que se sigue leyendo. El refresh token es opcional y se
		/// expresa vacio, que en esta operacion es "el proveedor no dio ninguno" y no un olvido.
		/// </summary>
		internal void StoreCredential(string token, string refreshToken)
		{
			if (string.IsNullOrWhiteSpace(token))
				throw new DomainException(
					"Una credencial sin access token no sirve para leer nada: guardarla dejaria "
					+ "la bitacora diciendo que tiene con que leer cuando no tiene.");

			_token = token;
			_refreshToken = refreshToken ?? "";
		}
	}
}
