using System;
using System.Text;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El texto normalizado de una placa: recortado, en mayusculas, y solo con lo que es letra o
	/// digito. Es la llave con la que un lector cruza este vehiculo contra lo que otro sistema
	/// publico por placa.
	/// <para>
	/// La regla de caracteres tiene que ser identica a la del otro lado, no meramente parecida: dos
	/// reglas que discrepan no dan ningun error, dan cero coincidencias, y eso se ve igual que un
	/// vehiculo que nunca aparecio por alla. Por eso vive en un tipo, donde una prueba puede
	/// apuntarle sin construir antes una placa.
	/// </para>
	/// </summary>
	internal sealed class TheJoinKey
	{
		private readonly string _value;

		private TheJoinKey(string value)
		{
			_value = value;
		}

		/// <summary>
		/// La llave de un texto recibido. Un texto en blanco y uno que no deja ningun caracter al
		/// normalizar se rechazan por separado, y el rechazo dice el texto TAL COMO LLEGO -- sin
		/// recortar y sin subir a mayusculas -- porque es el unico dato que le sirve a quien lo lee.
		/// </summary>
		internal static TheJoinKey Of(string received)
		{
			if (string.IsNullOrWhiteSpace(received))
				throw new DomainException(
					"Llego una placa en blanco: no hay nada que normalizar ni con que cruzar. Quien "
					+ "publica la placa de un vehiculo es el concesionario.");

			string key = TheLettersAndDigitsOf(received);

			if (key.Length == 0)
				throw new DomainException(
					"'" + received + "' no deja nada al normalizar: no es una placa.");

			return new TheJoinKey(key);
		}

		/// <summary>
		/// Si un texto deja una llave. TOTAL: contesta para todo texto, incluidos los que
		/// <see cref="TheJoinKey.Of"/> rechaza y el campo ausente que puede traer un feed. Es la
		/// pregunta que se hace donde rechazar todavia es gratis.
		/// </summary>
		internal static bool CanBeMadeFrom(string received)
		{
			return !string.IsNullOrWhiteSpace(received)
				&& TheLettersAndDigitsOf(received).Length > 0;
		}

		/// <summary>El texto normalizado, que es lo unico que cruza.</summary>
		internal string Value
		{
			get { return _value; }
		}

		private static string TheLettersAndDigitsOf(string received)
		{
			StringBuilder key = new StringBuilder(received.Length);

			foreach (char each in received.Trim().ToUpperInvariant())
				if (char.IsLetterOrDigit(each)) key.Append(each);

			return key.ToString();
		}
	}
}
