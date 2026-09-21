using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE UN FLUIDO PUBLICADO TIENE QUE DECIR: de que clase es, con que grado, cuanta capacidad y
	/// en que unidad, cada cuanto se cambia, y de que revision sale todo.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase: la redaccion vive donde se elige
	/// el idioma, y el dominio no elige uno. La capacidad viaja con los digitos con que se publico --
	/// ni un cero de mas ni uno de menos -- porque cuantos decimales tiene es del fabricante y donde va
	/// la coma es del idioma.
	/// </para>
	/// </summary>
	internal sealed class AFluidClause
	{
		private readonly string _publication;
		private readonly string _revisionCode;
		private readonly string _fluidKind;
		private readonly string _grade;
		private readonly decimal _capacity;
		private readonly string _capacityUnit;
		private readonly string _intervalShape;
		private readonly int _intervalDistance;
		private readonly int _intervalDays;
		private readonly string _intervalUnit;

		private AFluidClause(
			string publication, string revisionCode, string fluidKind, string grade,
			decimal capacity, string capacityUnit, string intervalShape, int intervalDistance,
			int intervalDays, string intervalUnit)
		{
			_publication = publication;
			_revisionCode = revisionCode;
			_fluidKind = fluidKind;
			_grade = grade;
			_capacity = capacity;
			_capacityUnit = capacityUnit;
			_intervalShape = intervalShape;
			_intervalDistance = intervalDistance;
			_intervalDays = intervalDays;
			_intervalUnit = intervalUnit;
		}

		/// <summary>
		/// La clausula cuando ninguna revision sellada publico ese fluido. Lleva la clase igual, porque
		/// una ausencia que no nombra lo que falta no se distingue de cualquier otra.
		/// </summary>
		internal static AFluidClause OfNothingPublished(FluidKind kind)
		{
			if (kind == null)
				throw new DomainException(
					"Se compuso la ausencia de un fluido sin decir de que clase. Una ausencia que no "
					+ "nombra lo que falta no se distingue de cualquier otra.");

			return new AFluidClause(
				SpecificationPublication.NotPublished, "", kind.Name, "", 0, "",
				IntervalHalvesPublished.None, 0, 0, "");
		}

		/// <summary>
		/// La clausula de un fluido que si se publico, con la revision de la que sale. Las cinco cosas
		/// van juntas o no es una especificacion.
		/// </summary>
		internal static AFluidClause Published(
			string revisionCode, FluidKind kind, string grade, decimal capacity,
			VolumeUnit capacityUnit, PublishedInterval changeInterval)
		{
			if (string.IsNullOrWhiteSpace(revisionCode))
				throw new DomainException(
					"Se compuso la clausula de un fluido sin nombrar la revision de la que sale. Una "
					+ "respuesta sin procedencia es un rumor: quien la reciba no puede saber si sigue "
					+ "vigente ni contra que compararla cuando cambie.");

			return new AFluidClause(
				SpecificationPublication.Published, revisionCode, kind.Name, grade, capacity,
				capacityUnit.Name, changeInterval.TheHalvesItPublished(),
				changeInterval.TheDistanceItPublished(), changeInterval.TheDaysItPublished(),
				changeInterval.TheUnitItsDistanceIsIn());
		}

		/// <summary>Si hay algo publicado detras de esta respuesta, por su codigo. TOTAL.</summary>
		internal string ThePublicationShape()
		{
			return _publication;
		}

		/// <summary>Si hay algo publicado que contestar. TOTAL.</summary>
		internal bool IsPublished()
		{
			return _publication == SpecificationPublication.Published;
		}

		/// <summary>El codigo de la revision que la publico, o vacio donde no hay ninguna. TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revisionCode;
		}

		/// <summary>De que clase de fluido habla, por el NOMBRE del miembro. TOTAL.</summary>
		internal string TheFluidItIsAbout()
		{
			return _fluidKind;
		}

		/// <summary>El grado publicado, tal como se publico, o vacio sin nada publicado. TOTAL.</summary>
		internal string TheGradePublished()
		{
			return _grade;
		}

		/// <summary>
		/// La capacidad con los digitos con que se publico, en cultura invariante. TOTAL: sin nada
		/// publicado sale un cero que nadie va a leer, porque la ausencia se dice con otro marco.
		/// </summary>
		internal string TheCapacityAsPublished()
		{
			return _capacity.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>La unidad en que esa capacidad se publico, como se dice. TOTAL.</summary>
		internal string TheUnitTheCapacityIsIn()
		{
			return _capacityUnit;
		}

		/// <summary>Que mitades publico el intervalo de cambio, por su codigo. TOTAL.</summary>
		internal string TheIntervalHalvesPublished()
		{
			return _intervalShape;
		}

		/// <summary>Cada cuanta distancia se cambia, o cero donde esa mitad no se publico. TOTAL.</summary>
		internal int TheIntervalDistance()
		{
			return _intervalDistance;
		}

		/// <summary>Cada cuantos dias se cambia, o cero por lo mismo. TOTAL.</summary>
		internal int TheIntervalDays()
		{
			return _intervalDays;
		}

		/// <summary>La unidad de esa distancia, como se dice, o vacia por lo mismo. TOTAL.</summary>
		internal string TheUnitTheIntervalIsIn()
		{
			return _intervalUnit;
		}
	}
}
