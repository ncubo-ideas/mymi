using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
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
		/// TODAS las exclusiones publicadas sobre ese sistema, en el orden en que entraron. Quedarse
		/// con la primera elegiria por la persona cual le importa. TOTAL: un sistema sin ninguna da
		/// una lista vacia, que es lo que toda respuesta suya lleva.
		/// </summary>
		internal List<CoverageExclusion> TheExclusionsApplicableTo(string systemCode)
		{
			List<CoverageExclusion> applicable = new List<CoverageExclusion>();

			foreach (CoverageExclusion exclusion in _exclusions)
				if (exclusion.NamesTheSystem(systemCode)) applicable.Add(exclusion);

			return applicable;
		}

		/// <summary>Los codigos de los sistemas que el alcance declara adentro, tal como se publicaron.</summary>
		internal string[] TheCoveredSystemsPublished()
		{
			List<string> codes = new List<string>();

			foreach (CoveredSystem system in _systems) codes.Add(system.Code);

			return codes.ToArray();
		}

		/// <summary>
		/// El alcance entero como forma y valores: los sistemas que declara adentro con sus limites
		/// propios, y las exclusiones con su texto y su condicion. Las exclusiones salen con texto y no
		/// como codigos: una lista de codigos no diria que esta excluido ni cuando.
		/// </summary>
		internal ACoverageScope TheScopePublished(string revisionCode)
		{
			return ACoverageScope.Of(revisionCode, _systems, _exclusions);
		}

		/// <summary>
		/// Donde esta parado un sistema respecto de lo publicado, contra los hechos que la lectura
		/// recibio del vehiculo. El ORDEN de los pasos es la ley: el catalogo decide antes que el
		/// alcance, el alcance antes que los limites, los limites antes que las exclusiones, y lo
		/// que no se puede medir degrada la respuesta solo cuando todavia podia cambiarla.
		/// </summary>
		internal CoverageAnswer TheStandingFor(
			string revisionCode, string systemCode, SystemLimits vehicleEnvelope,
			DateTime deliveredAt, int deliveryReading, int currentReading,
			DistanceUnit odometerUnit, DateTime asOf)
		{
			List<CoverageExclusion> applicable = TheExclusionsApplicableTo(systemCode);

			if (!TheVocabularyIsTheManufacturers(systemCode))
				return new ASettledCoverageAnswer(
					systemCode, CoverageStanding.NotPublished,
					ACoverageClause.MeasuringNothing(
						revisionCode, CoverageReason.NotInTheVocabulary, applicable));

			if (!TheScopeDeclaresItInside(systemCode))
				return new ASettledCoverageAnswer(
					systemCode, CoverageStanding.NotCovered,
					ACoverageClause.MeasuringNothing(
						revisionCode, CoverageReason.OnlyAmongTheExclusions, applicable));

			SystemLimits governing = TheSystemInsideTheScopeNamed(systemCode)
				.TheMoreSpecificLimitGoverns(vehicleEnvelope);

			if (governing.TheTimeHalfIsPast(deliveredAt, asOf))
				return new ASettledCoverageAnswer(
					systemCode, CoverageStanding.OutsideEnvelope,
					ACoverageClause.Measuring(
						revisionCode, CoverageReason.OutsideTheLimits, governing, applicable));

			if (governing.HasADistanceHalf() && !governing.MeasuresDistanceIn(odometerUnit))
				return new ACoverageSuspendedByAUnitMismatch(
					systemCode, governing.TheUnitItsDistanceIsMeasuredIn(), odometerUnit,
					ACoverageClause.MeasuringNothing(
						revisionCode, CoverageReason.UnitMismatch, applicable));

			if (governing.TheDistanceHalfIsPast(deliveryReading, currentReading))
				return new ASettledCoverageAnswer(
					systemCode, CoverageStanding.OutsideEnvelope,
					ACoverageClause.Measuring(
						revisionCode, CoverageReason.OutsideTheLimits, governing, applicable));

			if (applicable.Count > 0)
				return new ASettledCoverageAnswer(
					systemCode, CoverageStanding.CoveredWithExclusion,
					ACoverageClause.Measuring(
						revisionCode, CoverageReason.InsideTheLimits, governing, applicable));

			return new ASettledCoverageAnswer(
				systemCode, CoverageStanding.Covered,
				ACoverageClause.Measuring(
					revisionCode, CoverageReason.InsideTheLimits, governing, applicable));
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
	}
}
