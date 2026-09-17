using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta que da la lectura de cobertura: o esta decidida y trae uno de los cinco
	/// <see cref="CoverageStanding"/>, o no se pudo evaluar y lo dice nombrando en su propio tipo
	/// por que. Ninguno de los cinco quiere decir "no lo se", y devolver el benigno para una
	/// entrada inevaluable es como una app termina repitiendo rio abajo, como si fuera un hecho,
	/// algo que nunca se midio.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase: la redaccion vive donde se
	/// elige el idioma, y el dominio no elige uno. La clausula que dice CONTRA QUE se midio la
	/// llevan las dos variantes y se compone una sola vez.
	/// </para>
	/// </summary>
	internal abstract class CoverageAnswer
	{
		/// <summary>Se decidio donde esta parado el sistema.</summary>
		internal const string Settled = "Settled";

		/// <summary>El limite de distancia que rige y el odometro estan en unidades distintas.</summary>
		internal const string UnitMismatch = "UnitMismatch";

		private readonly ACoverageClause _clause;

		/// <summary>
		/// Toda respuesta de cobertura lleva su clausula: contra que se midio y de que revision sale.
		/// Sin ella la respuesta seria una afirmacion que nadie puede comprobar.
		/// </summary>
		protected CoverageAnswer(ACoverageClause clause)
		{
			if (clause == null)
				throw new DomainException(
					"Se compuso una respuesta de cobertura sin la clausula que dice contra que se "
					+ "midio. Decir donde quedo un sistema sin decir contra que limite, y sin nombrar "
					+ "la revision, deja un veredicto que nadie puede comprobar.");

			_clause = clause;
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
		/// Si quedo suspendida porque el limite que rige y el odometro estan en unidades
		/// distintas. TOTAL: es la clase de ausencia, con nombre, y no un codigo de motivo.
		/// </summary>
		internal virtual bool SuspendedByAUnitMismatch()
		{
			return false;
		}

		/// <summary>
		/// El resultado. Accesor GUARDADO: revienta si no se pudo evaluar, porque el conjunto de
		/// cinco no tiene un sexto miembro que diga "no lo se", y devolver cualquiera de los cinco
		/// ahi seria contestar una pregunta que no se pudo contestar.
		/// </summary>
		internal virtual CoverageStanding Standing()
		{
			throw new DomainException(
				"Esta pregunta de cobertura no se pudo evaluar, asi que no tiene respuesta: hay que "
				+ "preguntar primero si se podia evaluar. Cualquiera de las respuestas posibles "
				+ "devuelta aca se leeria como un hecho medido, y nadie mas abajo podria "
				+ "distinguirlo.");
		}

		/// <summary>Que forma tiene esta respuesta, por su codigo. TOTAL.</summary>
		internal abstract string TheShapeItHas();

		/// <summary>
		/// De que sistema habla, en el vocabulario del fabricante y tal como llego. TOTAL, y no se
		/// traduce: es del catalogo del fabricante, no de quien pregunta.
		/// </summary>
		internal abstract string TheSystemItIsAbout();

		/// <summary>
		/// Donde quedo parado el sistema, por el nombre del miembro, o vacio donde no se pudo
		/// decidir. TOTAL, a diferencia de <see cref="Standing"/>, que es la lectura guardada.
		/// </summary>
		internal virtual string TheStandingItReached()
		{
			return "";
		}

		/// <summary>
		/// La unidad del limite de distancia que rige, por su nombre, o vacia donde no hubo
		/// desacuerdo de unidades. TOTAL.
		/// </summary>
		internal virtual string TheUnitTheLimitIsIn()
		{
			return "";
		}

		/// <summary>
		/// La unidad del odometro que no coincidio con la del limite, por su nombre, o vacia por lo
		/// mismo. TOTAL.
		/// </summary>
		internal virtual string TheUnitTheOdometerIsIn()
		{
			return "";
		}

		/// <summary>Si hay algo publicado detras de esta respuesta, por su codigo. TOTAL.</summary>
		internal string ThePublicationShape()
		{
			return _clause.ThePublicationShape();
		}

		/// <summary>El codigo de la revision de la que sale. TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _clause.TheRevisionItCameFrom();
		}

		/// <summary>Que paso de la ley decidio, por su codigo. TOTAL.</summary>
		internal string TheReasonItGives()
		{
			return _clause.TheReasonItGives();
		}

		/// <summary>Si hay limites que nombrar, por su codigo. TOTAL.</summary>
		internal string TheLimitsShape()
		{
			return _clause.TheLimitsShape();
		}

		/// <summary>El plazo que rige. TOTAL.</summary>
		internal int TheMonthsInForce()
		{
			return _clause.TheMonthsInForce();
		}

		/// <summary>La distancia que rige. TOTAL.</summary>
		internal int TheDistanceInForce()
		{
			return _clause.TheDistanceInForce();
		}

		/// <summary>La unidad de esa distancia, por su nombre. TOTAL.</summary>
		internal string TheUnitTheDistanceIsInForceIn()
		{
			return _clause.TheUnitTheDistanceIsInForceIn();
		}

		/// <summary>Sobre que sistema es cada exclusion publicada, una por fila. TOTAL.</summary>
		internal string[] TheExclusionSystems()
		{
			return _clause.TheExclusionSystems();
		}

		/// <summary>Que excluye cada una, en el mismo orden. TOTAL.</summary>
		internal string[] TheExclusionTexts()
		{
			return _clause.TheExclusionTexts();
		}

		/// <summary>Bajo que condicion muerde cada una, en el mismo orden. TOTAL.</summary>
		internal string[] TheExclusionConditions()
		{
			return _clause.TheExclusionConditions();
		}
	}
}
