using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE UNA ESTADIA TIENE QUE DECIR: en cual de las tres situaciones esta, donde estuvo, por que
	/// entro, sus dos extremos y los dias de calendario que cubrio.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. La sucursal y el asunto son
	/// TEXTO DEL REGISTRO -- los mando el taller con el hecho -- asi que cruzan intactos y no se
	/// traducen; los dias los dijo la sucursal y cruzan en yyyy-MM-dd, que es la forma en que llegaron.
	/// </para>
	/// <para>
	/// LOS DOS EXTREMOS SON INSTANTES Y LOS DIAS SON DIAS, y la diferencia no es de presentacion: un
	/// extremo se registro con su hora, y un dia de calendario lo dijo la sucursal aparte porque
	/// sacarlo de un instante pediria una zona que este registro no tiene. Que aparezcan en la misma
	/// frase no los vuelve la misma clase de cosa.
	/// </para>
	/// </summary>
	internal sealed class AStayClause
	{
		private const string NothingToSay = "";
		private const int NoNumberToSay = 0;

		private static readonly string[] NoDaysToSay = new string[0];

		private readonly string _shape;
		private readonly string _branch;
		private readonly string _subject;
		private readonly string _enteredAt;
		private readonly string _collectedAt;
		private readonly int _dayCount;
		private readonly string[] _days;
		private readonly string _askedAt;

		private AStayClause(
			string shape, string branch, string subject, string enteredAt, string collectedAt,
			int dayCount, string[] days, string askedAt)
		{
			_shape = shape;
			_branch = branch;
			_subject = subject;
			_enteredAt = enteredAt;
			_collectedAt = collectedAt;
			_dayCount = dayCount;
			_days = days;
			_askedAt = askedAt;
		}

		/// <summary>
		/// La estadia que sigue abierta: donde esta y desde cuando. No nombra ninguna salida ni ningun
		/// dia, porque los dias llegan con la entrega.
		/// </summary>
		internal static AStayClause StillOpen(string branch, string subject, string enteredAt)
		{
			return new AStayClause(
				StayShape.StillOpen, TheBranch(branch), TheSubject(subject), enteredAt, NothingToSay,
				NoNumberToSay, NoDaysToSay, NothingToSay);
		}

		/// <summary>
		/// La estadia ya entregada: sus dos extremos, cuantos dias cubrio y cuales. Sin ningun dia la
		/// clausula estaria diciendo una estadia cerrada que no cubrio ninguno, que el dominio no deja
		/// existir.
		/// </summary>
		internal static AStayClause Collected(
			string branch, string subject, string enteredAt, string collectedAt, int dayCount,
			string[] days)
		{
			if (days == null || days.Length == 0)
				throw new DomainException(
					"Se compuso una estadia ya entregada sin ninguno de los dias que cubrio. Los dias "
					+ "los dice la sucursal junto con la salida, y una estadia cerrada cubrio al menos "
					+ "uno.");

			return new AStayClause(
				StayShape.Collected, TheBranch(branch), TheSubject(subject), enteredAt, collectedAt,
				dayCount, days, NothingToSay);
		}

		/// <summary>
		/// La constancia de que ninguna estadia cubre ese instante, con el instante adentro: sin el, la
		/// frase no dice cual pregunta contesto.
		/// </summary>
		internal static AStayClause NoStayCovers(string askedAt)
		{
			return new AStayClause(
				StayShape.NoStayCoversTheInstant, NothingToSay, NothingToSay, NothingToSay,
				NothingToSay, NoNumberToSay, NoDaysToSay, askedAt);
		}

		/// <summary>En cual de las tres situaciones esta, por su codigo. TOTAL.</summary>
		internal string TheShapeItHas()
		{
			return _shape;
		}

		/// <summary>
		/// La sucursal donde estuvo, con las palabras que mando ella, o vacia donde no hay estadia.
		/// TOTAL.
		/// </summary>
		internal string TheBranchItWasSpentIn()
		{
			return _branch;
		}

		/// <summary>El asunto con que entro, con las palabras de la sucursal. TOTAL.</summary>
		internal string TheSubjectItCameInFor()
		{
			return _subject;
		}

		/// <summary>Cuando entro, como INSTANTE con su hora. TOTAL.</summary>
		internal string TheInstantItEnteredAt()
		{
			return _enteredAt;
		}

		/// <summary>
		/// Cuando salio, como INSTANTE con su hora, o vacio mientras siga abierta. TOTAL, y cual de los
		/// dos es lo dice la forma.
		/// </summary>
		internal string TheInstantItWasCollectedAt()
		{
			return _collectedAt;
		}

		/// <summary>
		/// Cuantos dias de calendario cubrio, o cero donde no hay ninguno que contar. TOTAL, y cual de
		/// los dos es lo dice la forma y nunca ese cero.
		/// </summary>
		internal int HowManyDaysItCovered()
		{
			return _dayCount;
		}

		/// <summary>
		/// Los dias que cubrio, en yyyy-MM-dd y en el orden en que llegaron. TOTAL: vacio donde no hay
		/// ninguno.
		/// </summary>
		internal string[] TheDaysItCovered()
		{
			return _days;
		}

		/// <summary>
		/// El INSTANTE por el que se pregunto, que solo la ausencia nombra: las otras dos se dicen con
		/// sus propios extremos. TOTAL.
		/// </summary>
		internal string TheInstantItWasAskedAt()
		{
			return _askedAt;
		}

		private static string TheBranch(string branch)
		{
			if (string.IsNullOrWhiteSpace(branch))
				throw new DomainException(
					"Se compuso una estadia sin decir en que sucursal estuvo el vehiculo, y donde "
					+ "estuvo es la mitad de lo que una estadia registra.");

			return branch;
		}

		private static string TheSubject(string subject)
		{
			if (string.IsNullOrWhiteSpace(subject))
				throw new DomainException(
					"Se compuso una estadia sin decir por que entro el vehiculo.");

			return subject;
		}
	}
}
