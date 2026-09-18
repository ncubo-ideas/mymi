using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// EL ALCANCE PUBLICADO ENTERO, sin ningun hecho de ningun vehiculo adentro: los sistemas que la
	/// revision declara dentro -- cada uno con sus limites propios si los publico -- y las exclusiones
	/// con su texto y su condicion, mas la revision de la que sale.
	/// <para>
	/// Las columnas van paralelas y en el orden en que se publicaron. Sin alcance publicado sale la
	/// ausencia, que es un objeto y no una lista vacia con el codigo en blanco.
	/// </para>
	/// </summary>
	internal sealed class ACoverageScope
	{
		private static readonly string[] NoText = new string[0];
		private static readonly int[] NoAmount = new int[0];

		private readonly string _publication;
		private readonly string _revisionCode;
		private readonly string[] _systemCodes;
		private readonly string[] _systemLimitShapes;
		private readonly int[] _systemMonths;
		private readonly int[] _systemDistances;
		private readonly string[] _systemDistanceUnits;
		private readonly string[] _exclusionSystems;
		private readonly string[] _exclusionTexts;
		private readonly string[] _exclusionConditions;

		private ACoverageScope(
			string publication, string revisionCode,
			string[] systemCodes, string[] systemLimitShapes, int[] systemMonths,
			int[] systemDistances, string[] systemDistanceUnits,
			string[] exclusionSystems, string[] exclusionTexts, string[] exclusionConditions)
		{
			_publication = publication;
			_revisionCode = revisionCode;
			_systemCodes = systemCodes;
			_systemLimitShapes = systemLimitShapes;
			_systemMonths = systemMonths;
			_systemDistances = systemDistances;
			_systemDistanceUnits = systemDistanceUnits;
			_exclusionSystems = exclusionSystems;
			_exclusionTexts = exclusionTexts;
			_exclusionConditions = exclusionConditions;
		}

		/// <summary>
		/// Ninguna revision sellada publico alcance. No es un alcance vacio: un alcance vacio diria que
		/// el fabricante publico que no cubre nada.
		/// </summary>
		internal static ACoverageScope NothingWasPublished()
		{
			return new ACoverageScope(
				SpecificationPublication.NotPublished, "",
				NoText, NoText, NoAmount, NoAmount, NoText, NoText, NoText, NoText);
		}

		/// <summary>
		/// El alcance que esa revision publico, con sus dos listas tal como entraron. Se compone en cada
		/// lectura y no se guarda: la revision no cambia y la respuesta tampoco.
		/// </summary>
		internal static ACoverageScope Of(
			string revisionCode, List<CoveredSystem> systems, List<CoverageExclusion> exclusions)
		{
			if (string.IsNullOrWhiteSpace(revisionCode))
				throw new DomainException(
					"Se compuso el alcance publicado sin nombrar la revision de la que sale. Un alcance "
					+ "sin procedencia no se puede comparar con el de la revision siguiente, que es lo "
					+ "unico que deja ver que cambio.");

			if (systems == null || exclusions == null)
				throw new DomainException(
					$"Se compuso el alcance de la revision '{revisionCode}' sin alguna de sus dos listas. "
					+ "Ninguna de las dos se dice callandola: un alcance sin exclusiones y un alcance del "
					+ "que no se miraron las exclusiones se leen igual y no son lo mismo.");

			string[] codes = new string[systems.Count];
			string[] limitShapes = new string[systems.Count];
			int[] months = new int[systems.Count];
			int[] distances = new int[systems.Count];
			string[] units = new string[systems.Count];

			for (int each = 0; each < systems.Count; each++)
			{
				codes[each] = systems[each].Code;
				limitShapes[each] = systems[each].TheShapeOfItsOwnLimits();
				months[each] = systems[each].TheMonthsItsOwnLimitsName();
				distances[each] = systems[each].TheDistanceItsOwnLimitsName();
				units[each] = systems[each].TheNameOfTheUnitItsOwnLimitsUse();
			}

			string[] exclusionSystems = new string[exclusions.Count];
			string[] texts = new string[exclusions.Count];
			string[] conditions = new string[exclusions.Count];

			for (int each = 0; each < exclusions.Count; each++)
			{
				exclusionSystems[each] = exclusions[each].Code;
				texts[each] = exclusions[each].TheTextItPublished();
				conditions[each] = exclusions[each].TheConditionItAppliesUnder();
			}

			return new ACoverageScope(
				SpecificationPublication.Published, revisionCode,
				codes, limitShapes, months, distances, units,
				exclusionSystems, texts, conditions);
		}

		/// <summary>Si hay alcance publicado, por su codigo. TOTAL.</summary>
		internal string ThePublicationShape()
		{
			return _publication;
		}

		/// <summary>El codigo de la revision que lo publico, o vacio donde no hay ninguna. TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revisionCode;
		}

		/// <summary>Los codigos de los sistemas que declara adentro, una fila por sistema. TOTAL.</summary>
		internal string[] TheSystemCodes()
		{
			return _systemCodes;
		}

		/// <summary>Si cada sistema publico limites propios, por su codigo y en el mismo orden. TOTAL.</summary>
		internal string[] TheSystemLimitShapes()
		{
			return _systemLimitShapes;
		}

		/// <summary>El plazo propio de cada sistema, o cero donde no publico ninguno. TOTAL.</summary>
		internal int[] TheSystemMonths()
		{
			return _systemMonths;
		}

		/// <summary>La distancia propia de cada sistema, o cero por lo mismo. TOTAL.</summary>
		internal int[] TheSystemDistances()
		{
			return _systemDistances;
		}

		/// <summary>La unidad de esa distancia, por su nombre, o vacia por lo mismo. TOTAL.</summary>
		internal string[] TheSystemDistanceUnits()
		{
			return _systemDistanceUnits;
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
	}
}
