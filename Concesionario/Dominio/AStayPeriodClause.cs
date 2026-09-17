using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE UN PERIODO DE ESTADIAS TIENE QUE DECIR: en cual de las dos situaciones esta, entre que
	/// dos dias se pregunto, cuantos dias estuvo adentro, en cuantas estadias, y cuales son esos dias.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. Los dos extremos y los dias son
	/// DIAS DE CALENDARIO -- ninguno sale de un instante -- y cruzan en yyyy-MM-dd, que es la forma en
	/// que la sucursal los dijo y la unica que quien redacta puede volver a decir en su idioma.
	/// </para>
	/// <para>
	/// La ausencia nombra los dos extremos igual que la otra situacion: un periodo sin estadias sigue
	/// siendo un periodo, y un conteo en cero no diria entre cuando y cuando se pregunto.
	/// </para>
	/// </summary>
	internal sealed class AStayPeriodClause
	{
		private const int NoNumberToSay = 0;

		private static readonly string[] NoDaysToSay = new string[0];

		private readonly string _shape;
		private readonly string _from;
		private readonly string _to;
		private readonly int _dayCount;
		private readonly int _stayCount;
		private readonly string[] _days;

		private AStayPeriodClause(
			string shape, string from, string to, int dayCount, int stayCount, string[] days)
		{
			_shape = shape;
			_from = from;
			_to = to;
			_dayCount = dayCount;
			_stayCount = stayCount;
			_days = days;
		}

		/// <summary>
		/// El periodo que si tiene dias: cuantos, en cuantas estadias y cuales. Sin ningun dia la
		/// respuesta es la ausencia con nombre y no un periodo que estuvo adentro cero dias.
		/// </summary>
		internal static AStayPeriodClause WithStaysOnRecord(
			string fromDay, string toDay, int dayCount, int stayCount, string[] days)
		{
			if (days == null || days.Length == 0)
				throw new DomainException(
					"Se compuso un periodo con estadias registradas y no llego ninguno de sus dias. Que "
					+ "no haya ninguno es su propia situacion y se dice con palabras.");

			return new AStayPeriodClause(
				StayPeriodShape.WithStaysOnRecord, TheDay(fromDay), TheDay(toDay), dayCount, stayCount,
				days);
		}

		/// <summary>
		/// El periodo que no tiene ninguna estadia con dias registrados, nombrando sus dos extremos.
		/// </summary>
		internal static AStayPeriodClause WithNoStayOnRecord(string fromDay, string toDay)
		{
			return new AStayPeriodClause(
				StayPeriodShape.NoStayOnRecord, TheDay(fromDay), TheDay(toDay), NoNumberToSay,
				NoNumberToSay, NoDaysToSay);
		}

		/// <summary>En cual de las dos situaciones esta, por su codigo. TOTAL.</summary>
		internal string TheShapeItHas()
		{
			return _shape;
		}

		/// <summary>El primer DIA del periodo por el que se pregunto, en yyyy-MM-dd. TOTAL.</summary>
		internal string TheDayItStartsAt()
		{
			return _from;
		}

		/// <summary>El ultimo, inclusive y en la misma forma. TOTAL.</summary>
		internal string TheDayItEndsAt()
		{
			return _to;
		}

		/// <summary>
		/// Cuantos dias del periodo estuvo adentro, o cero donde no hay ninguno. TOTAL, y cual de los
		/// dos es lo dice la forma y nunca ese cero.
		/// </summary>
		internal int HowManyDays()
		{
			return _dayCount;
		}

		/// <summary>En cuantas estadias, o cero por lo mismo. TOTAL.</summary>
		internal int HowManyStays()
		{
			return _stayCount;
		}

		/// <summary>
		/// Los dias del periodo en que estuvo adentro, ascendentes y sin repetidos. TOTAL: vacio donde
		/// no hay ninguno.
		/// </summary>
		internal string[] TheDaysItCovered()
		{
			return _days;
		}

		private static string TheDay(string day)
		{
			if (string.IsNullOrWhiteSpace(day))
				throw new DomainException(
					"Se compuso un periodo de estadias sin decir entre que dos dias se pregunto. Sin "
					+ "ellos, todos los periodos dirian la misma frase.");

			return day;
		}
	}
}
