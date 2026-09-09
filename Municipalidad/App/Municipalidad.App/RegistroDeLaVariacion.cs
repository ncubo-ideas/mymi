using System;

namespace Municipalidad.App
{
	/// <summary>
	/// Por que un cobro de un rubro cambio entre sus dos ultimos comprobantes: los dos periodos, los
	/// dos montos por periodo, cuanto cambio, que lo explica, y LA FRASE con que el dominio lo dice.
	/// Un DTO con forma de wire hecho de primitivos, nunca un portador del dominio.
	/// <para>
	/// Es UNA respuesta y no filas, asi que ninguno de sus campos es una columna y no hay largos que
	/// comprobar. Los numeros viajan tipados y la frase viaja armada: quien la muestre no la rehace.
	/// </para>
	/// </summary>
	public sealed class RegistroDeLaVariacion
	{
		private RegistroDeLaVariacion(
			string rubro, string desde, string hasta, decimal antes, decimal ahora, decimal proporcion,
			string explicacion, bool esSalto, string urgencia, string dice)
		{
			Rubro = rubro;
			Desde = desde;
			Hasta = hasta;
			Antes = antes;
			Ahora = ahora;
			Proporcion = proporcion;
			Explicacion = explicacion;
			EsSalto = esSalto;
			Urgencia = urgencia;
			Dice = dice;
		}

		/// <summary>De que rubro es esta variacion, por el nombre del rubro.</summary>
		public string Rubro { get; }

		/// <summary>El periodo del comprobante anterior, en el alfabeto de su rubro.</summary>
		public string Desde { get; }

		/// <summary>El periodo del comprobante mas reciente, en el alfabeto de su rubro.</summary>
		public string Hasta { get; }

		/// <summary>Lo que costo UN periodo en el comprobante anterior.</summary>
		public decimal Antes { get; }

		/// <summary>Lo que costo UN periodo en el comprobante mas reciente.</summary>
		public decimal Ahora { get; }

		/// <summary>El cambio como proporcion: 0,8 es un 80 % mas.</summary>
		public decimal Proporcion { get; }

		/// <summary>
		/// Que explica el cambio, por el nombre: `cambio-de-tarifa`, `cambio-de-avaluo`, `cambio-menor`
		/// o `sin-cambio`.
		/// </summary>
		public string Explicacion { get; }

		/// <summary>Si el cambio llega a ser un salto o se queda por debajo del umbral preguntado.</summary>
		public bool EsSalto { get; }

		/// <summary>Cuanto apura, por el nombre del nivel. Un aumento ya paso, asi que nunca apura.</summary>
		public string Urgencia { get; }

		/// <summary>
		/// La frase que una persona lee, compuesta por el dominio. Se dice tal cual: quien la reescriba
		/// produce una segunda explicacion que el dominio nunca dio.
		/// </summary>
		public string Dice { get; }

		/// <summary>
		/// Arma el registro y refusa una frase o un nombre en blanco: una respuesta sin frase se
		/// veria igual que una con frase vacia, y es lo unico que una caratula no puede rehacer.
		/// </summary>
		internal static RegistroDeLaVariacion Of(
			string rubro, string desde, string hasta, decimal antes, decimal ahora, decimal proporcion,
			string explicacion, bool esSalto, string urgencia, string dice)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(rubro);
			ArgumentException.ThrowIfNullOrWhiteSpace(desde);
			ArgumentException.ThrowIfNullOrWhiteSpace(hasta);
			ArgumentException.ThrowIfNullOrWhiteSpace(explicacion);
			ArgumentException.ThrowIfNullOrWhiteSpace(urgencia);
			ArgumentException.ThrowIfNullOrWhiteSpace(dice);

			return new RegistroDeLaVariacion(
				rubro, desde, hasta, antes, ahora, proporcion, explicacion, esSalto, urgencia, dice);
		}
	}
}
