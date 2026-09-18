using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Una cosa que se gasto en este vehiculo: su dinero, el instante en que se incurrio, el asunto
	/// recibido y su clasificacion. Lo que se GASTO, no lo que el fabricante publico.
	/// <para>
	/// No tiene clave de ninguna clase y no tiene maquina de estados: tiene una clasificacion, que
	/// ES su tipo. Queda abierta a herencia porque esa es la variacion que carga -- una que el
	/// periodo consume entera y una que se reparte a lo largo de su vida util.
	/// </para>
	/// <para>
	/// Sale como valores ya tipados y nunca como una frase: la redaccion vive donde se elige el
	/// idioma, y el dominio no elige uno. La clasificacion es la que dice cual de las dos cosas hay
	/// que leer, asi que ninguna se lee de un cero.
	/// </para>
	/// </summary>
	internal abstract class ACostEntry
	{
		private readonly Money _money;
		private readonly DateTime _incurredAt;
		private readonly CostSubject _subject;

		/// <summary>
		/// Lo que toda entrada tiene. Sin dinero o sin asunto es un defecto de quien llama, y el
		/// rechazo del dinero nombra el asunto, que es lo unico con lo que una persona la reconoce.
		/// </summary>
		internal ACostEntry(Money money, DateTime incurredAt, CostSubject subject)
		{
			if (subject == null)
				throw new DomainException(
					"Llego un gasto sin asunto, asi que no dice en que se fue la plata.");

			if (money == null)
				throw new DomainException(
					"Llego un gasto en " + subject.Value + " sin decir cuanto. Un gasto sin dinero "
					+ "no es un gasto: no hay nada que sumar ni nada que repartir.");

			_money = money;
			_incurredAt = incurredAt;
			_subject = subject;
		}

		/// <summary>El dinero que costo, con su moneda adentro.</summary>
		internal Money TheMoneyItCost()
		{
			return _money;
		}

		/// <summary>El instante recibido en que se incurrio.</summary>
		internal DateTime IncurredAt
		{
			get { return _incurredAt; }
		}

		/// <summary>En que se fue, con las palabras que llegaron.</summary>
		internal CostSubject Subject
		{
			get { return _subject; }
		}

		/// <summary>La forma de la moneda en que costo, por su codigo. TOTAL.</summary>
		internal string TheShapeOfItsCurrency()
		{
			return _money.TheShapeOfItsCurrency();
		}

		/// <summary>Los digitos de lo que costo, en cultura invariante. TOTAL.</summary>
		internal string TheAmountAsSaid()
		{
			return _money.TheAmountAsSaid();
		}

		/// <summary>El codigo de la moneda en que costo. TOTAL.</summary>
		internal string TheCodeOfItsCurrency()
		{
			return _money.TheCodeOfItsCurrency();
		}

		/// <summary>En que se fue, dicho hacia afuera.</summary>
		internal string TheSubjectItWasSpentOn()
		{
			return _subject.Value;
		}

		/// <summary>
		/// Cuando se incurrio, dicho como INSTANTE con su hora y no como un dia de calendario: de
		/// que dia local es un instante necesitaria una zona que este libro no tiene.
		/// </summary>
		internal string TheInstantItWasIncurred()
		{
			return TheInstant(_incurredAt);
		}

		/// <summary>Si se incurrio entre esos dos instantes, ambos inclusive. TOTAL.</summary>
		internal bool WasIncurredBetween(DateTime from, DateTime to)
		{
			return _incurredAt >= from && _incurredAt <= to;
		}

		/// <summary>
		/// Si se SABE que este gasto esta en la misma moneda que ese otro. TOTAL, y es la pregunta
		/// con la que se agrupa por moneda recorriendo la lista.
		/// </summary>
		internal bool IsInTheSameCurrencyAs(ACostEntry other)
		{
			if (other == null)
				throw new DomainException(
					"Se pregunto si dos gastos estan en la misma moneda sin decir el segundo.");

			return _money.IsInTheSameCurrencyAs(other._money);
		}

		/// <summary>De que clase es este gasto. TOTAL: la contesta el tipo, no un dato.</summary>
		internal abstract CostClassification Classification();

		/// <summary>
		/// Si este gasto se reparte a lo largo de una vida util. TOTAL, y es la pregunta que hay
		/// que hacer antes de pedir los meses o la parte: la clasificacion es la que lo dice, no un
		/// cero.
		/// </summary>
		internal abstract bool SpreadsItselfOverTime();

		/// <summary>
		/// A lo largo de cuantos meses se reparte, tal como llegaron. TOTAL: el que no se reparte
		/// contesta cero, y cual de los dos es lo dice su clasificacion y nunca ese cero.
		/// </summary>
		internal abstract int TheMonthsOfItsExpectedLife();

		/// <summary>
		/// Si ese periodo consumio algo de este gasto. TOTAL, y es la pregunta que hay que hacer
		/// antes de pedir el dinero consumido.
		/// </summary>
		internal abstract bool ConsumesAnythingBetween(DateTime from, DateTime to);

		/// <summary>
		/// El dinero que ese periodo consumio de este gasto. Accesor GUARDADO: un periodo que no
		/// consumio nada no tiene cifra que decir, y un cero ahi se leeria como un gasto que hubo.
		/// </summary>
		internal abstract Money TheMoneyItConsumedBetween(DateTime from, DateTime to);

		/// <summary>
		/// La parte que ese periodo consumio, con la regla que la saco adentro. Accesor GUARDADO:
		/// un gasto que no se reparte no tiene regla que hacer visible, y un periodo que no toca la
		/// ventana no tiene parte.
		/// </summary>
		internal abstract AnAmortizedShare TheShareItGivesBetween(DateTime from, DateTime to);

		/// <summary>Un instante dicho como instante, con su hora.</summary>
		protected static string TheInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}
	}
}
