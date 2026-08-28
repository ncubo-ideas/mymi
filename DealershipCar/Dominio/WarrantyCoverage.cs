using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El alcance de garantia que el fabricante publico dentro de una revision: que sistemas
	/// declara adentro, que exclusiones publico, y el catalogo de codigos que llego a nombrar de
	/// alguna de las dos formas. Dice DONDE ESTA PARADO un sistema respecto de lo publicado y
	/// nunca si un reclamo se va a resolver de una manera u otra: eso es del fabricante.
	/// </summary>
	internal sealed class WarrantyCoverage
	{
		private readonly List<CoveredSystem> _systems = new List<CoveredSystem>();
		private readonly List<CoverageExclusion> _exclusions = new List<CoverageExclusion>();

		/// <summary>
		/// Nace vacia con la revision y se le acumula adentro: la garantia no esta indexada por
		/// una clave como los fluidos, se arma publicacion a publicacion.
		/// </summary>
		internal WarrantyCoverage()
		{
		}

		/// <summary>
		/// Como se nombra la pregunta de cobertura sobre un sistema. Sale de aca para que la
		/// respuesta decidida y la ausencia hablen del mismo sujeto con las mismas palabras.
		/// </summary>
		internal static string TheCoverageQuestionAbout(string systemCode)
		{
			return "Warranty coverage for system '" + systemCode + "'";
		}

		/// <summary>
		/// La clausula que toda respuesta de cobertura lleva: esto es alcance publicado, y como se
		/// resuelva un reclamo concreto lo dice el fabricante. Va en la frase para que la ley se
		/// diga a si misma y no se pierda en la primera capa que reformule la respuesta.
		/// </summary>
		internal static string ScopeIsNotAdjudication()
		{
			return "Scope only: this states what the manufacturer published about the scope of the "
				+ "warranty, and nothing about how a claim would be resolved -- that is the "
				+ "manufacturer's to decide, not this record's.";
		}

		/// <summary>
		/// Entra un sistema al alcance. Dentro de una misma revision sin sellar, republicar un
		/// codigo lo CORRIGE en su lugar en vez de duplicarlo: es la unica ventana de correccion
		/// que hay, porque despues de sellar no hay ninguna.
		/// </summary>
		internal void PublishACoveredSystem(CoveredSystem system)
		{
			if (system == null)
				throw new DomainException(
					"Se pidio publicar un sistema cubierto sin decir cual. Publicar nada le daria "
					+ "alcance a la revision sin darle nada que contestar.");

			for (int place = 0; place < _systems.Count; place++)
				if (_systems[place].NamesTheSystem(system.Code))
				{
					_systems[place] = system;
					return;
				}

			_systems.Add(system);
		}

		/// <summary>
		/// Entra una exclusion al alcance. Republicar LA MISMA -- mismo sistema y mismo texto --
		/// la corrige en su lugar; una distinta sobre el mismo sistema se agrega, porque un
		/// sistema puede tener mas de una y quedarse con la ultima elegiria por la persona.
		/// </summary>
		internal void PublishAnExclusion(CoverageExclusion exclusion)
		{
			if (exclusion == null)
				throw new DomainException(
					"Se pidio publicar una exclusion sin decir cual. Una exclusion vacia le avisaria "
					+ "a la persona que hay letra chica sin dejarle leerla.");

			for (int place = 0; place < _exclusions.Count; place++)
				if (_exclusions[place].NamesTheSameExclusionAs(exclusion))
				{
					_exclusions[place] = exclusion;
					return;
				}

			_exclusions.Add(exclusion);
		}

		/// <summary>
		/// Si entro algo al alcance de esta revision. Un alcance de solo exclusiones tambien es un
		/// alcance publicado: si hiciera falta un sistema cubierto, las exclusiones no tendrian
		/// donde vivir.
		/// </summary>
		internal bool HasAnythingPublished()
		{
			return _systems.Count > 0 || _exclusions.Count > 0;
		}

		/// <summary>
		/// Si el catalogo del fabricante conoce ese codigo, de cualquiera de las dos formas: como
		/// sistema cubierto o nombrado por una exclusion. La UNION es lo que separa "no lo cubre"
		/// de "no nos lo dijeron" -- sin ella, un sistema que el fabricante excluyo expresamente
		/// contestaria que nadie publico nada sobre el. Consulta TOTAL: un codigo desconocido es
		/// una respuesta, no un defecto de quien llama.
		/// </summary>
		internal bool TheVocabularyIsTheManufacturers(string systemCode)
		{
			if (string.IsNullOrWhiteSpace(systemCode)) return false;

			foreach (CoveredSystem system in _systems)
				if (system.NamesTheSystem(systemCode)) return true;

			foreach (CoverageExclusion exclusion in _exclusions)
				if (exclusion.NamesTheSystem(systemCode)) return true;

			return false;
		}

		/// <summary>
		/// Si hay letra chica publicada sobre ese sistema. TOTAL, y es la que hay que hacer antes
		/// de pedir que se nombre.
		/// </summary>
		internal bool HasAnApplicableExclusion(string systemCode)
		{
			foreach (CoverageExclusion exclusion in _exclusions)
				if (exclusion.NamesTheSystem(systemCode)) return true;

			return false;
		}

		/// <summary>
		/// Nombra TODAS las exclusiones publicadas sobre ese sistema, con su texto y su condicion.
		/// Quedarse con la primera elegiria por la persona cual le importa. Accesor GUARDADO:
		/// pedirlo de un sistema sin exclusiones es un defecto de secuencia.
		/// </summary>
		internal string AKnownExclusionIsNamed(string systemCode)
		{
			if (!HasAnApplicableExclusion(systemCode))
				throw new DomainException(
					$"No hay ninguna exclusion publicada sobre '{systemCode}': hay que preguntar "
					+ "primero si la hay. Una frase vacia aca se leeria como que se reviso y no habia "
					+ "nada, que es lo mismo que decirlo sin haber mirado.");

			List<string> named = new List<string>();

			foreach (CoverageExclusion exclusion in _exclusions)
				if (exclusion.NamesTheSystem(systemCode)) named.Add(exclusion.Describe());

			return string.Join(" ", named);
		}

		/// <summary>Los codigos de los sistemas que el alcance declara adentro, tal como se publicaron.</summary>
		internal string[] TheCoveredSystemsPublished()
		{
			List<string> codes = new List<string>();

			foreach (CoveredSystem system in _systems) codes.Add(system.Code);

			return codes.ToArray();
		}

		/// <summary>
		/// Las exclusiones publicadas, cada una con su texto y su condicion. Salen con texto y no
		/// como codigos: una lista de codigos no diria que esta excluido ni cuando.
		/// </summary>
		internal string[] TheExclusionsPublished()
		{
			List<string> clauses = new List<string>();

			foreach (CoverageExclusion exclusion in _exclusions) clauses.Add(exclusion.Describe());

			return clauses.ToArray();
		}

		/// <summary>
		/// El alcance entero en una frase: los sistemas que declara adentro, las exclusiones, y la
		/// clausula que dice que esto es alcance y no la resolucion de un reclamo.
		/// </summary>
		internal string DescribeTheScope()
		{
			return "Systems inside the published scope: " + TheSystemsInWords() + " Exclusions: "
				+ TheExclusionsInWords() + " " + ScopeIsNotAdjudication();
		}

		/// <summary>
		/// Donde esta parado un sistema respecto de lo publicado, contra los hechos que la lectura
		/// recibio del vehiculo. El ORDEN de los pasos es la ley: el catalogo decide antes que el
		/// alcance, el alcance antes que los limites, los limites antes que las exclusiones, y lo
		/// que no se puede medir degrada la respuesta solo cuando todavia podia cambiarla.
		/// </summary>
		internal CoverageAnswer TheStandingFor(
			string systemCode, SystemLimits vehicleEnvelope,
			DateTime deliveredAt, int deliveryReading, int currentReading,
			DistanceUnit odometerUnit, DateTime asOf)
		{
			string subject = TheCoverageQuestionAbout(systemCode);

			if (!TheVocabularyIsTheManufacturers(systemCode))
				return new ASettledCoverageAnswer(
					subject, CoverageStanding.NotPublished,
					TheClauseFor(systemCode,
						"The published scope does not name this system at all, so there is nothing "
						+ "here about it -- which is not the same as the scope leaving it out. "));

			if (!TheScopeDeclaresItInside(systemCode))
				return new ASettledCoverageAnswer(
					subject, CoverageStanding.NotCovered,
					TheClauseFor(systemCode,
						"The published scope names this system only among its exclusions, and never "
						+ "among the systems it declares inside. "));

			SystemLimits governing = TheSystemInsideTheScopeNamed(systemCode)
				.TheMoreSpecificLimitGoverns(vehicleEnvelope);

			if (governing.TheTimeHalfIsPast(deliveredAt, asOf))
				return new ASettledCoverageAnswer(
					subject, CoverageStanding.OutsideEnvelope,
					TheClauseFor(systemCode, ThisVehicleIs("past", governing)));

			if (governing.HasADistanceHalf() && !governing.MeasuresDistanceIn(odometerUnit))
				return new ACoverageSuspendedByAUnitMismatch(
					subject, governing.TheUnitItsDistanceIsMeasuredIn(), odometerUnit,
					TheClauseFor(systemCode, string.Empty));

			if (governing.TheDistanceHalfIsPast(deliveryReading, currentReading))
				return new ASettledCoverageAnswer(
					subject, CoverageStanding.OutsideEnvelope,
					TheClauseFor(systemCode, ThisVehicleIs("past", governing)));

			if (HasAnApplicableExclusion(systemCode))
				return new ASettledCoverageAnswer(
					subject, CoverageStanding.CoveredWithExclusion,
					TheClauseFor(systemCode, ThisVehicleIs("inside", governing)));

			return new ASettledCoverageAnswer(
				subject, CoverageStanding.Covered,
				TheClauseFor(systemCode, ThisVehicleIs("inside", governing)));
		}

		private static string ThisVehicleIs(string whereItStands, SystemLimits governing)
		{
			return "Governing limits: " + governing.Describe() + ". This vehicle is " + whereItStands
				+ " them. ";
		}

		private string TheClauseFor(string systemCode, string reason)
		{
			if (!HasAnApplicableExclusion(systemCode))
				return reason + ScopeIsNotAdjudication();

			return reason + AKnownExclusionIsNamed(systemCode) + " " + ScopeIsNotAdjudication();
		}

		private bool TheScopeDeclaresItInside(string systemCode)
		{
			foreach (CoveredSystem system in _systems)
				if (system.NamesTheSystem(systemCode)) return true;

			return false;
		}

		private CoveredSystem TheSystemInsideTheScopeNamed(string systemCode)
		{
			foreach (CoveredSystem system in _systems)
				if (system.NamesTheSystem(systemCode)) return system;

			throw new DomainException(
				$"El alcance publicado no declara adentro a '{systemCode}': hay que preguntar "
				+ "primero si lo declara. Fabricar uno aca inventaria un sistema cubierto que el "
				+ "fabricante nunca publico.");
		}

		private string TheSystemsInWords()
		{
			if (_systems.Count == 0) return "none published.";

			List<string> clauses = new List<string>();

			foreach (CoveredSystem system in _systems) clauses.Add(system.Describe());

			return string.Join(". ", clauses) + ".";
		}

		private string TheExclusionsInWords()
		{
			if (_exclusions.Count == 0) return "none published.";

			return string.Join(" ", TheExclusionsPublished());
		}
	}
}
