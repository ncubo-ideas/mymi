using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// CONTRA QUE SE MIDIO una respuesta de cobertura: que paso de la ley decidio, que limites rigen,
	/// que exclusiones publicadas hay sobre ese sistema y de que revision sale todo. Es la mitad de la
	/// respuesta que no cambia con la situacion, y por eso se compone una sola vez y la llevan las dos
	/// variantes.
	/// <para>
	/// La ley de que esto no adjudica ningun reclamo NO esta aqui: no es un valor que el dominio
	/// elija, es redaccion que toda respuesta lleva siempre, y vive donde se elige el idioma.
	/// </para>
	/// </summary>
	internal sealed class ACoverageClause
	{
		private static readonly string[] Nothing = new string[0];

		private readonly string _publication;
		private readonly string _revisionCode;
		private readonly string _reason;
		private readonly string _limitsShape;
		private readonly int _months;
		private readonly int _distance;
		private readonly string _distanceUnit;
		private readonly string[] _exclusionSystems;
		private readonly string[] _exclusionTexts;
		private readonly string[] _exclusionConditions;

		private ACoverageClause(
			string publication, string revisionCode, string reason, string limitsShape,
			int months, int distance, string distanceUnit,
			string[] exclusionSystems, string[] exclusionTexts, string[] exclusionConditions)
		{
			_publication = publication;
			_revisionCode = revisionCode;
			_reason = reason;
			_limitsShape = limitsShape;
			_months = months;
			_distance = distance;
			_distanceUnit = distanceUnit;
			_exclusionSystems = exclusionSystems;
			_exclusionTexts = exclusionTexts;
			_exclusionConditions = exclusionConditions;
		}

		/// <summary>
		/// La clausula cuando ninguna revision sellada publico alcance. No hay revision que nombrar ni
		/// paso de la ley que contar: no se llego a mirar ninguno.
		/// </summary>
		internal static ACoverageClause OfNothingPublished()
		{
			return new ACoverageClause(
				SpecificationPublication.NotPublished, "", CoverageReason.None,
				LimitsInForce.None, 0, 0, "", Nothing, Nothing, Nothing);
		}

		/// <summary>
		/// La clausula de un paso que decidio sin llegar a medir contra ningun limite -- el catalogo o
		/// el alcance --, con las exclusiones publicadas sobre ese sistema.
		/// </summary>
		internal static ACoverageClause MeasuringNothing(
			string revisionCode, string reason, List<CoverageExclusion> exclusions)
		{
			return WithTheseExclusions(
				revisionCode, reason, LimitsInForce.None, 0, 0, "", exclusions);
		}

		/// <summary>
		/// La clausula de un paso que si midio, con los limites que rigen y las exclusiones publicadas
		/// sobre ese sistema. Los limites van enteros: decir donde quedo el vehiculo sin decir contra
		/// que deja un veredicto que nadie puede comprobar.
		/// </summary>
		internal static ACoverageClause Measuring(
			string revisionCode, string reason, SystemLimits governing,
			List<CoverageExclusion> exclusions)
		{
			if (governing == null || !governing.Exists())
				throw new DomainException(
					"Se compuso la clausula de una cobertura medida sin los limites contra los que se "
					+ "midio. Un veredicto sin el limite que lo decidio no se puede comprobar, y es "
					+ "exactamente lo que esta respuesta existe para dejar ver.");

			return WithTheseExclusions(
				revisionCode, reason, LimitsInForce.Named,
				governing.TheMonthsItLimits(), governing.TheDistanceItLimits(),
				governing.TheNameOfTheUnitItsDistanceIsIn(), exclusions);
		}

		/// <summary>Si hay algo publicado detras de esta respuesta, por su codigo. TOTAL.</summary>
		internal string ThePublicationShape()
		{
			return _publication;
		}

		/// <summary>El codigo de la revision que la publico, o vacio donde no hay ninguna. TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revisionCode;
		}

		/// <summary>Que paso de la ley decidio, por su codigo. TOTAL.</summary>
		internal string TheReasonItGives()
		{
			return _reason;
		}

		/// <summary>Si hay limites que nombrar, por su codigo. TOTAL.</summary>
		internal string TheLimitsShape()
		{
			return _limitsShape;
		}

		/// <summary>El plazo que rige, o cero donde no hay limites que nombrar. TOTAL.</summary>
		internal int TheMonthsInForce()
		{
			return _months;
		}

		/// <summary>La distancia que rige, o cero por lo mismo. TOTAL.</summary>
		internal int TheDistanceInForce()
		{
			return _distance;
		}

		/// <summary>La unidad de esa distancia, por su nombre, o vacia por lo mismo. TOTAL.</summary>
		internal string TheUnitTheDistanceIsInForceIn()
		{
			return _distanceUnit;
		}

		/// <summary>Sobre que sistema es cada exclusion publicada, una por fila. TOTAL.</summary>
		internal string[] TheExclusionSystems()
		{
			return _exclusionSystems;
		}

		/// <summary>Que excluye cada una, tal como se publico, en el mismo orden. TOTAL.</summary>
		internal string[] TheExclusionTexts()
		{
			return _exclusionTexts;
		}

		/// <summary>Bajo que condicion muerde cada una, en el mismo orden. TOTAL.</summary>
		internal string[] TheExclusionConditions()
		{
			return _exclusionConditions;
		}

		private static ACoverageClause WithTheseExclusions(
			string revisionCode, string reason, string limitsShape,
			int months, int distance, string distanceUnit, List<CoverageExclusion> exclusions)
		{
			if (string.IsNullOrWhiteSpace(revisionCode))
				throw new DomainException(
					"Se compuso la clausula de una cobertura sin nombrar la revision de la que sale. Una "
					+ "respuesta sin procedencia es un rumor: quien la reciba no puede saber si sigue "
					+ "vigente ni contra que compararla cuando cambie.");

			if (exclusions == null)
				throw new DomainException(
					$"Se compuso la clausula de una cobertura de la revision '{revisionCode}' sin decir "
					+ "que exclusiones hay. Ninguna se dice con una lista vacia, que es un objeto: "
					+ "callarlas es la peor respuesta posible a una pregunta de cobertura.");

			string[] systems = new string[exclusions.Count];
			string[] texts = new string[exclusions.Count];
			string[] conditions = new string[exclusions.Count];

			for (int each = 0; each < exclusions.Count; each++)
			{
				systems[each] = exclusions[each].Code;
				texts[each] = exclusions[each].TheTextItPublished();
				conditions[each] = exclusions[each].TheConditionItAppliesUnder();
			}

			return new ACoverageClause(
				SpecificationPublication.Published, revisionCode, reason, limitsShape,
				months, distance, distanceUnit, systems, texts, conditions);
		}
	}
}
