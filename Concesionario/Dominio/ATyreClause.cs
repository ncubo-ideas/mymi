using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE UNA LLANTA PUBLICADA TIENE QUE DECIR: en que posicion va, que medida, que presion en frio
	/// y en que unidad, con que clasificaciones, y de que revision sale todo.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. La presion viaja con los digitos
	/// con que se publico, y en la unidad en que se publico: quien la lee la va a marcar en un
	/// manometro, asi que aqui no se convierte ni se redondea nada.
	/// </para>
	/// </summary>
	internal sealed class ATyreClause
	{
		private readonly string _publication;
		private readonly string _revisionCode;
		private readonly string _position;
		private readonly string _size;
		private readonly decimal _coldPressure;
		private readonly string _pressureUnit;
		private readonly string _ratings;

		private ATyreClause(
			string publication, string revisionCode, string position, string size,
			decimal coldPressure, string pressureUnit, string ratings)
		{
			_publication = publication;
			_revisionCode = revisionCode;
			_position = position;
			_size = size;
			_coldPressure = coldPressure;
			_pressureUnit = pressureUnit;
			_ratings = ratings;
		}

		/// <summary>
		/// La clausula cuando ninguna revision sellada publico la llanta de esa posicion. Lleva la
		/// posicion igual: adelante y atras casi nunca llevan la misma, asi que cual falta importa.
		/// </summary>
		internal static ATyreClause OfNothingPublished(TyrePosition position)
		{
			if (position == null)
				throw new DomainException(
					"Se compuso la ausencia de una llanta sin decir de que posicion. Una ausencia que "
					+ "no nombra lo que falta no se distingue de cualquier otra.");

			return new ATyreClause(
				SpecificationPublication.NotPublished, "", position.Name, "", 0, "", "");
		}

		/// <summary>
		/// La clausula de una llanta que si se publico, con la revision de la que sale.
		/// </summary>
		internal static ATyreClause Published(
			string revisionCode, TyrePosition position, string size, decimal coldPressure,
			PressureUnit pressureUnit, string ratings)
		{
			if (string.IsNullOrWhiteSpace(revisionCode))
				throw new DomainException(
					"Se compuso la clausula de una llanta sin nombrar la revision de la que sale. Una "
					+ "respuesta sin procedencia es un rumor: quien la reciba no puede saber si sigue "
					+ "vigente ni contra que compararla cuando cambie.");

			return new ATyreClause(
				SpecificationPublication.Published, revisionCode, position.Name, size, coldPressure,
				pressureUnit.Name, ratings);
		}

		/// <summary>Si hay algo publicado detras de esta respuesta, por su codigo. TOTAL.</summary>
		internal string ThePublicationShape()
		{
			return _publication;
		}

		/// <summary>Si hay algo publicado que contestar. TOTAL.</summary>
		internal bool IsPublished()
		{
			return _publication == SpecificationPublication.Published;
		}

		/// <summary>El codigo de la revision que la publico, o vacio donde no hay ninguna. TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revisionCode;
		}

		/// <summary>De que posicion habla, por el NOMBRE del miembro. TOTAL.</summary>
		internal string ThePositionItIsAbout()
		{
			return _position;
		}

		/// <summary>La medida publicada, tal como se publico. TOTAL.</summary>
		internal string TheSizePublished()
		{
			return _size;
		}

		/// <summary>
		/// La presion en frio con los digitos con que se publico, en cultura invariante. TOTAL.
		/// </summary>
		internal string ThePressureAsPublished()
		{
			return _coldPressure.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>La unidad en que esa presion se publico, como se dice. TOTAL.</summary>
		internal string TheUnitThePressureIsIn()
		{
			return _pressureUnit;
		}

		/// <summary>
		/// Las clasificaciones publicadas, tal como se publicaron: son el limite de carga y de
		/// velocidad de la llanta. TOTAL.
		/// </summary>
		internal string TheRatingsPublished()
		{
			return _ratings;
		}
	}
}
