using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Que llanta lleva el modelo en una posicion: la medida, la presion en frio con la unidad en
	/// que se publico, y las clasificaciones. Las tres juntas o no es una especificacion, y la
	/// posicion es parte de la respuesta: adelante y atras casi nunca llevan la misma presion.
	/// </summary>
	internal sealed class TyreSpecification
	{
		private readonly TyrePosition _position;
		private readonly string _size;
		private readonly decimal _coldPressure;
		private readonly PressureUnit _pressureUnit;
		private readonly string _ratings;

		/// <summary>
		/// Se arma entera y se valida antes de entrar a ninguna revision. La presion va con la
		/// unidad en que se publico y se contesta en esa: no se normaliza ni se convierte, porque
		/// quien la lee la va a marcar en un manometro.
		/// </summary>
		internal TyreSpecification(
			TyrePosition position, string size, decimal coldPressure, PressureUnit pressureUnit,
			string ratings)
		{
			if (position == null)
				throw new DomainException(
					"Se publico una especificacion de llanta sin decir para que posicion es. Una "
					+ "presion sin posicion se termina aplicando a las cuatro.");

			if (string.IsNullOrWhiteSpace(size))
				throw new DomainException(
					$"Se publico la llanta {position.Name} sin medida. La medida es lo que la persona "
					+ "compra: una especificacion que no la dice no contesta la pregunta que le hacen.");

			if (coldPressure <= 0)
				throw new DomainException(
					$"Se publico la llanta {position.Name} con una presion en frio de {coldPressure}. "
					+ "Una llanta que rueda lleva alguna presion, y un cero se leeria como que va "
					+ "desinflada.");

			if (pressureUnit == null)
				throw new DomainException(
					$"Se publico la presion de la llanta {position.Name} sin decir en que unidad "
					+ "esta. Aca las unidades no se convierten, asi que una presion sin unidad es un "
					+ "numero que se puede marcar en la bomba equivocada.");

			if (string.IsNullOrWhiteSpace(ratings))
				throw new DomainException(
					$"Se publico la llanta {position.Name} sin clasificaciones. Son el limite de "
					+ "carga y de velocidad de la llanta, y callarlas deja escoger una que no los "
					+ "aguanta.");

			_position = position;
			_size = size;
			_coldPressure = coldPressure;
			_pressureUnit = pressureUnit;
			_ratings = ratings;
		}

		/// <summary>
		/// La clausula que esta llanta aporta a la respuesta: la posicion, la medida, la presion con
		/// su unidad y las clasificaciones, como forma y valores. Elegir que va en ella es del
		/// dominio; redactarla es de quien elige el idioma.
		/// </summary>
		internal ATyreClause TheClauseItComposes(string revisionCode)
		{
			return ATyreClause.Published(
				revisionCode, _position, _size, _coldPressure, _pressureUnit, _ratings);
		}
	}
}
