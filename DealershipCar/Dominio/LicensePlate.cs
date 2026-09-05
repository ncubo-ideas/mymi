using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La placa que el concesionario publico para este vehiculo: el texto tal como llego y el texto
	/// normalizado con el que se cruza. Dos textos de UN hecho, y ninguno se deriva del otro fuera de
	/// aqui.
	/// <para>
	/// Es un hecho del vehiculo y nunca su identidad: el Vin identifica y la placa describe. Por eso
	/// no lleva igualdad escrita ni ningun miembro que tome otra placa -- de este lado nada compara
	/// dos placas, y cual rige se decide por posicion en la historia.
	/// </para>
	/// </summary>
	internal sealed class LicensePlate
	{
		private readonly TheJoinKey _joinKey;

		/// <summary>
		/// La placa tal como la publico el concesionario. El texto se guarda sin tocar y la llave se
		/// deriva de el, que es lo que deja que la pantalla diga lo publicado mientras el cruce usa
		/// lo normalizado.
		/// </summary>
		internal LicensePlate(string received)
		{
			_joinKey = TheJoinKey.Of(received);

			AsPublished = received;
		}

		/// <summary>El texto tal como llego, con sus guiones y sus espacios si los traia.</summary>
		internal string AsPublished { get; }

		/// <summary>El texto normalizado, que es con lo que se cruza.</summary>
		internal string JoinKey
		{
			get { return _joinKey.Value; }
		}
	}
}
