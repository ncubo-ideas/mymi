using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta a donde esta parado este vehiculo contra su sobre de garantia: o esta decidida
	/// y trae uno de los tres <see cref="EnvelopeStanding"/>, o no se pudo evaluar y lo dice
	/// nombrando en su propio tipo por que. Ninguno de los tres quiere decir "no lo se", y
	/// devolver el benigno para una entrada inevaluable es como una app termina repitiendo rio
	/// abajo, como si fuera un hecho, algo que nunca se midio.
	/// </summary>
	internal abstract class EnvelopeAnswer
	{
		/// <summary>
		/// Como se nombra esta pregunta. Sale de aca para que la respuesta decidida y las
		/// suspendidas hablen del mismo sujeto con las mismas palabras.
		/// </summary>
		internal static string TheEnvelopeQuestion()
		{
			return "El sobre de garantía de este vehículo";
		}

		/// <summary>
		/// La clausula que toda respuesta de sobre lleva: esto dice donde esta parado el vehiculo,
		/// y como se resuelva un reclamo concreto lo dice el fabricante. Va en la frase para que la
		/// ley se diga a si misma y no se pierda en la primera capa que reformule la respuesta.
		/// </summary>
		internal static string TheStandingIsNotAnAdjudication()
		{
			return "Solo posición: esto dice dónde está parado este vehículo frente al sobre que "
				+ "publicó el concesionario, y nada sobre si un reclamo se pagaría -- eso lo decide el "
				+ "fabricante, no este registro.";
		}

		/// <summary>
		/// Si se pudo decidir con lo que hay. TOTAL: contesta siempre, y es la que hay que hacer
		/// antes de leer el resultado.
		/// </summary>
		internal virtual bool CanBeEvaluated()
		{
			return false;
		}

		/// <summary>
		/// Si quedo suspendida porque nadie registro la entrega, que es el ancla desde donde se
		/// miden las dos mitades. TOTAL: es la clase de ausencia, con nombre, y no un codigo de
		/// motivo.
		/// </summary>
		internal virtual bool SuspendedByAnAbsentDelivery()
		{
			return false;
		}

		/// <summary>
		/// Si quedo suspendida porque no hay ninguna lectura del odometro al instante preguntado
		/// ni antes. TOTAL.
		/// </summary>
		internal virtual bool SuspendedByAnAbsentReading()
		{
			return false;
		}

		/// <summary>
		/// Si quedo suspendida porque la distancia del sobre y el odometro estan en unidades
		/// distintas. TOTAL.
		/// </summary>
		internal virtual bool SuspendedByAUnitMismatch()
		{
			return false;
		}

		/// <summary>
		/// El resultado. Accesor GUARDADO: revienta si no se pudo evaluar, porque el conjunto de
		/// tres no tiene un cuarto miembro que diga "no lo se", y devolver cualquiera de los tres
		/// ahi seria contestar una pregunta que no se pudo contestar.
		/// </summary>
		internal virtual EnvelopeStanding Standing()
		{
			throw new DomainException(
				"Esta pregunta sobre el sobre de garantia no se pudo evaluar, asi que no tiene "
				+ "respuesta: hay que preguntar primero si se podia evaluar. Cualquiera de las "
				+ "respuestas posibles devuelta aca se leeria como un hecho medido, y nadie mas "
				+ "abajo podria distinguirlo.");
		}

		/// <summary>
		/// La respuesta en una frase. TOTAL: lo que no se pudo evaluar tambien se dice con
		/// palabras, y diciendo por que.
		/// </summary>
		internal abstract string Describe();

		/// <summary>
		/// Arma la frase de una variante: el sujeto, lo que esa variante tiene para decir, lo que
		/// haya aportado quien compone, y la clausula de que esto no es una adjudicacion. La
		/// clausula la pone la frase y no el que compone, para que no dependa de que se acuerde.
		/// </summary>
		protected static string TheAnswerSaying(string reason, string clause)
		{
			string said = TheEnvelopeQuestion() + ": " + reason;

			if (!string.IsNullOrWhiteSpace(clause)) said = said + " " + clause.Trim();

			return said + " " + TheStandingIsNotAnAdjudication();
		}
	}
}
