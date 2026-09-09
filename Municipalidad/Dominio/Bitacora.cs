using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Hasta donde leyo la app la narrativa de la municipalidad, y con que credencial la sigue
	/// leyendo. Es la SEGUNDA global de raiz del DSL, al lado de <see cref="Cuenta"/> y nunca
	/// adentro: ni el punto de control ni la credencial son hechos de cobros municipales.
	/// </summary>
	internal sealed class Bitacora
	{
		private long ultimoEvento;
		private string claveDeDispositivo = "";
		private string token = "";
		private string tokenDeRefresco = "";

		/// <summary>
		/// El constructor ES la creacion: una bitacora que no leyo nada y no tiene con que leer.
		/// Las dos cosas llegan despues, cada una por su comando.
		/// </summary>
		internal Bitacora()
		{
		}

		/// <summary>
		/// El numero del ultimo evento aplicado. TOTAL: cero es "todavia no se leyo nada", un valor
		/// legal de esta escala y no una ausencia, porque los eventos se numeran desde uno.
		/// </summary>
		internal long UltimoEvento()
		{
			return ultimoEvento;
		}

		/// <summary>
		/// PREDICADO TOTAL: el punto de control solo avanza. Un evento ya aplicado no vuelve a
		/// entrar, y eso es la idempotencia de la ingesta vista desde el dominio.
		/// </summary>
		internal bool PuedeAvanzar(long numeroDeEvento)
		{
			return numeroDeEvento > ultimoEvento;
		}

		/// <summary>Avanza el punto de control hasta ese evento.</summary>
		internal void Avanzar(long numeroDeEvento)
		{
			if (!PuedeAvanzar(numeroDeEvento))
				throw new DomainException(
					"Ese evento no avanza la bitacora: ya se habia procesado hasta " + ultimoEvento
					+ ". Preguntar PuedeAvanzar antes.");

			ultimoEvento = numeroDeEvento;
		}

		/// <summary>PREDICADO TOTAL: si esta instalacion ya se registro ante la municipalidad.</summary>
		internal bool TieneDispositivo()
		{
			return claveDeDispositivo.Length > 0;
		}

		/// <summary>
		/// La clave con que la municipalidad conoce a ESTA instalacion. GUARDADA: sin registro no
		/// hay clave vacia, hay ausencia de clave.
		/// </summary>
		internal string ClaveDeDispositivo()
		{
			if (!TieneDispositivo())
				throw new DomainException(
					"Esta instalacion no esta registrada ante la municipalidad. Preguntar "
					+ "TieneDispositivo antes.");

			return claveDeDispositivo;
		}

		/// <summary>
		/// PREDICADO TOTAL: se registra UNA vez. La misma clave otra vez es el mismo hecho otra vez,
		/// y otra clave dejaria huerfana la credencial ya emitida.
		/// </summary>
		internal bool PuedeRegistrarDispositivo(string clave)
		{
			return !string.IsNullOrWhiteSpace(clave) && !TieneDispositivo();
		}

		/// <summary>Registra esta instalacion con la clave que la municipalidad le reconoce.</summary>
		internal void RegistrarDispositivo(string clave)
		{
			if (!PuedeRegistrarDispositivo(clave))
				throw new DomainException(
					"No se puede registrar esta instalacion: la clave no sirve, o ya esta registrada. "
					+ "Preguntar PuedeRegistrarDispositivo antes.");

			claveDeDispositivo = clave;
		}

		/// <summary>PREDICADO TOTAL: si hay credencial guardada con que leer la narrativa.</summary>
		internal bool TieneCredencial()
		{
			return token.Length > 0;
		}

		/// <summary>
		/// El token con que se leen los eventos. GUARDADO, por lo mismo que
		/// <see cref="ClaveDeDispositivo"/>: un token vacio se veria igual que un token.
		/// </summary>
		internal string Token()
		{
			if (!TieneCredencial())
				throw new DomainException(
					"Esta bitacora no tiene credencial guardada. Preguntar TieneCredencial antes.");

			return token;
		}

		/// <summary>
		/// PREDICADO TOTAL: si la credencial se puede rotar sola. Hay proveedores que solo dan el
		/// token de acceso, y entonces la instalacion pide una credencial nueva con su clave.
		/// </summary>
		internal bool PuedeRotar()
		{
			return tokenDeRefresco.Length > 0;
		}

		/// <summary>El token de refresco, durable y rotatorio. GUARDADO.</summary>
		internal string TokenDeRefresco()
		{
			if (!PuedeRotar())
				throw new DomainException(
					"Esta bitacora no tiene token de refresco. Preguntar PuedeRotar antes.");

			return tokenDeRefresco;
		}

		/// <summary>
		/// PREDICADO TOTAL: una credencial sin token de acceso no sirve, y la credencial que ya
		/// esta guardada es el mismo hecho otra vez. El token de refresco es opcional y se expresa
		/// vacio, que en esta operacion es "el proveedor no dio ninguno" y no un olvido.
		/// </summary>
		internal bool PuedeGuardarCredencial(string tokenDeAcceso, string refresco)
		{
			if (string.IsNullOrWhiteSpace(tokenDeAcceso)) return false;
			return tokenDeAcceso != token || ElRefrescoNormalizado(refresco) != tokenDeRefresco;
		}

		/// <summary>Guarda la credencial con que se sigue leyendo, o la rota por una nueva.</summary>
		internal void GuardarCredencial(string tokenDeAcceso, string refresco)
		{
			if (!PuedeGuardarCredencial(tokenDeAcceso, refresco))
				throw new DomainException(
					"No se puede guardar esa credencial: no trae token de acceso, o es la que ya "
					+ "estaba guardada. Preguntar PuedeGuardarCredencial antes.");

			token = tokenDeAcceso;
			tokenDeRefresco = ElRefrescoNormalizado(refresco);
		}

		private static string ElRefrescoNormalizado(string refresco)
		{
			return string.IsNullOrWhiteSpace(refresco) ? "" : refresco;
		}
	}
}
