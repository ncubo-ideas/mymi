using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Una cosa que se gasto en este vehiculo: su dinero, el instante en que se incurrio, el asunto
	/// recibido y su clasificacion. Lo que se GASTO, no lo que el fabricante publico.
	/// <para>
	/// No tiene clave de ninguna clase y no tiene maquina de estados: tiene una clasificacion, que ES
	/// su tipo. Queda abierta a herencia porque esa es la variacion que carga -- una que el periodo
	/// consume entera y una que se reparte a lo largo de su vida util.
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

		/// <summary>El dinero dicho como su propia moneda lo dice.</summary>
		internal string TheMoneyItSays()
		{
			return _money.Expressed();
		}

		/// <summary>En que se fue, dicho hacia afuera.</summary>
		internal string TheSubjectItWasSpentOn()
		{
			return _subject.Value;
		}

		/// <summary>
		/// Cuando se incurrio, dicho como INSTANTE con su hora y no como un dia de calendario: de que
		/// dia local es un instante necesitaria una zona que este libro no tiene.
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
		/// Si se SABE que este gasto esta en la misma moneda que ese otro. TOTAL, y es la pregunta con
		/// la que se agrupa por moneda recorriendo la lista.
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
		/// Como se dice su vida util, o nada cuando no tiene ninguna. TOTAL: una celda vacia dice que
		/// no tiene, y un cero se leeria como una vida de cero meses.
		/// </summary>
		internal abstract string HowItsExpectedLifeIsSaid();

		/// <summary>
		/// Si ese periodo consumio algo de este gasto. TOTAL, y es la pregunta que hay que hacer antes
		/// de pedir el dinero consumido.
		/// </summary>
		internal abstract bool ConsumesAnythingBetween(DateTime from, DateTime to);

		/// <summary>
		/// El dinero que ese periodo consumio de este gasto. Accesor GUARDADO: un periodo que no
		/// consumio nada no tiene cifra que decir, y un cero ahi se leeria como un gasto que hubo.
		/// </summary>
		internal abstract Money TheMoneyItConsumedBetween(DateTime from, DateTime to);

		/// <summary>
		/// La clausula que hace visible la regla con que este gasto se reparte en ese periodo, o nada
		/// cuando no hay regla que hacer visible. TOTAL.
		/// </summary>
		internal abstract string TheClauseOfItsConsumptionBetween(DateTime from, DateTime to);

		/// <summary>
		/// La entrada en una frase, dicha entera: el dinero, en que se fue, cuando, y de que clase es.
		/// Es lo que hace que una fila sirva sola.
		/// </summary>
		internal abstract string Describe();

		/// <summary>La clausula que las dos variantes comparten: cuanto, en que y cuando.</summary>
		protected string TheSpendClause()
		{
			return _money.Expressed() + " en " + _subject.Value + " el " + TheInstant(_incurredAt);
		}

		/// <summary>Un instante dicho como instante, con su hora.</summary>
		protected static string TheInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}
	}
}
