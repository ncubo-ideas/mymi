using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE EL COSTO DE LA DISTANCIA DE UN PERIODO TIENE QUE DECIR: el periodo, el ancla con su
	/// instante, la distancia recorrida desde ella con su unidad, y una cifra por moneda -- o cual
	/// de las tres situaciones sin cifra le toca.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. Las tres situaciones sin
	/// cifra son respuestas con nombre y no huecos: un cero por unidad en cualquiera de ellas se
	/// leeria como un vehiculo que anduvo gratis.
	/// </para>
	/// <para>
	/// El motivo de la que no tiene tramo NO viaja aqui: lo compone la serie del odometro en su
	/// propia clausula, que es de quien es esa ley, y quien redacta pone una dentro de la otra.
	/// Transportarlo dejaria dos versiones de una misma razon.
	/// </para>
	/// </summary>
	internal sealed class ACostPerDistanceClause
	{
		private const string NothingToSay = "";
		private const int NoDistanceToSay = 0;

		private static readonly string[] NoFigures = new string[0];

		private readonly string _shape;
		private readonly string _from;
		private readonly string _to;
		private readonly string _firstReading;
		private readonly int _distance;
		private readonly string _unit;
		private readonly string[] _figureShapes;
		private readonly string[] _figureAmounts;
		private readonly string[] _figureCodes;

		private ACostPerDistanceClause(
			string shape, string from, string to, string firstReading, int distance, string unit,
			string[] figureShapes, string[] figureAmounts, string[] figureCodes)
		{
			_shape = shape;
			_from = from;
			_to = to;
			_firstReading = firstReading;
			_distance = distance;
			_unit = unit;
			_figureShapes = figureShapes;
			_figureAmounts = figureAmounts;
			_figureCodes = figureCodes;
		}

		/// <summary>
		/// La clausula con una cifra por moneda: lo que se gasto despues del ancla, dividido por la
		/// distancia recorrida desde ella.
		/// </summary>
		internal static ACostPerDistanceClause Measured(
			string from, string to, string firstReading, int distance, string unit,
			List<Money> perCurrency)
		{
			if (perCurrency == null || perCurrency.Count == 0)
				throw new DomainException(
					"Se compuso un costo por distancia medido sin ninguna cifra por moneda. Sin ninguna "
					+ "no hay nada que dividir, y esa situacion tiene su propio nombre.");

			string[] shapes = new string[perCurrency.Count];
			string[] amounts = new string[perCurrency.Count];
			string[] codes = new string[perCurrency.Count];

			for (int each = 0; each < perCurrency.Count; each++)
			{
				shapes[each] = perCurrency[each].TheShapeOfItsCurrency();
				amounts[each] = perCurrency[each].TheAmountAsSaid();
				codes[each] = perCurrency[each].TheCodeOfItsCurrency();
			}

			return new ACostPerDistanceClause(
				CostPerDistanceShape.Measured, from, to, firstReading, distance, unit,
				shapes, amounts, codes);
		}

		/// <summary>
		/// La clausula de un periodo en que, despues de esa lectura, no se gasto nada. Se dice con
		/// palabras: un cero por unidad se leeria como un vehiculo que anduvo gratis.
		/// </summary>
		internal static ACostPerDistanceClause NothingSpentAfterTheAnchor(
			string from, string to, string firstReading, int distance, string unit)
		{
			return new ACostPerDistanceClause(
				CostPerDistanceShape.NothingSpentAfterTheAnchor, from, to, firstReading, distance,
				unit, NoFigures, NoFigures, NoFigures);
		}

		/// <summary>
		/// La clausula de un periodo en que si se gasto y el odometro no se movio. Se dice con
		/// palabras, por la misma razon.
		/// </summary>
		internal static ACostPerDistanceClause NoDistanceTravelled(
			string from, string to, string firstReading, string unit)
		{
			return new ACostPerDistanceClause(
				CostPerDistanceShape.NoDistanceTravelled, from, to, firstReading, NoDistanceToSay,
				unit, NoFigures, NoFigures, NoFigures);
		}

		/// <summary>
		/// La clausula de un periodo sin tramo de uso que medir. El motivo lo dice la serie del
		/// odometro en la suya, y esta no lo lleva.
		/// </summary>
		internal static ACostPerDistanceClause WithNoSpanOfUse(string from, string to)
		{
			return new ACostPerDistanceClause(
				CostPerDistanceShape.NoSpanOfUse, from, to, NothingToSay, NoDistanceToSay,
				NothingToSay, NoFigures, NoFigures, NoFigures);
		}

		/// <summary>Que pudo decirse del costo de la distancia, por su codigo. TOTAL.</summary>
		internal string TheShapeItHas()
		{
			return _shape;
		}

		/// <summary>Donde empieza el periodo, como instante con su hora. TOTAL.</summary>
		internal string TheInstantItStartsAt()
		{
			return _from;
		}

		/// <summary>Donde termina el periodo, como instante con su hora. TOTAL.</summary>
		internal string TheInstantItEndsAt()
		{
			return _to;
		}

		/// <summary>
		/// El instante de la primera lectura del odometro del periodo, que es el ancla, o vacio
		/// donde no hay tramo que medir. TOTAL.
		/// </summary>
		internal string TheInstantOfTheAnchor()
		{
			return _firstReading;
		}

		/// <summary>
		/// Cuanto recorrio el vehiculo desde el ancla, o cero donde no hay tramo. TOTAL, y cual de
		/// los dos es lo dice la forma y nunca ese cero.
		/// </summary>
		internal int TheDistanceTravelled()
		{
			return _distance;
		}

		/// <summary>La unidad de esa distancia, como se dice, o vacia por lo mismo.
		/// TOTAL.</summary>
		internal string TheUnitTheDistanceIsIn()
		{
			return _unit;
		}

		/// <summary>La forma de la moneda de cada cifra, una por fila. TOTAL.</summary>
		internal string[] TheShapesOfEachFigure()
		{
			return _figureShapes;
		}

		/// <summary>Los digitos de cada cifra por unidad, en el mismo orden. TOTAL.</summary>
		internal string[] TheFiguresAsSaid()
		{
			return _figureAmounts;
		}

		/// <summary>El codigo de la moneda de cada cifra, en el mismo orden. TOTAL.</summary>
		internal string[] TheCodesOfEachFigure()
		{
			return _figureCodes;
		}
	}
}
