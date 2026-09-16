using System.Globalization;
using Concesionario.App;

namespace Concesionario.Wasm
{
	/// <summary>
	/// La ficha del fabricante tal como la muestran las dos secciones de la pantalla, leida entera
	/// en un solo momento y por la unica puerta. Ninguna frase se compone aca: cada una la compuso
	/// el dominio y esta clase solo la acomoda al lado de su etiqueta y de su revision.
	/// </summary>
	public sealed class WhatTheManufacturerPublished
	{
		/// <summary>De que modelo es la ficha que este registro lleva.</summary>
		public string Model { get; init; } = "";

		/// <summary>
		/// Si hay alguna revision sellada que pueda contestar. Se pregunta ANTES que el codigo y el
		/// instante, que son lecturas guardadas y revientan sin ninguna.
		/// </summary>
		public bool HasASealedRevision { get; init; }

		/// <summary>El codigo de la revision vigente, o vacio si no hay ninguna sellada.</summary>
		public string CurrentRevision { get; init; } = "";

		/// <summary>El dia en que se publico la vigente, o vacio si no hay ninguna sellada.</summary>
		public string CurrentRevisionPublishedAt { get; init; } = "";

		/// <summary>Las seis clases de fluido, publicadas o no, en el orden del canon.</summary>
		public IReadOnlyList<PublishedFluid> Fluids { get; init; } = new List<PublishedFluid>();

		/// <summary>Las tres posiciones de llanta, publicadas o no, en el orden del canon.</summary>
		public IReadOnlyList<PublishedTyre> Tyres { get; init; } = new List<PublishedTyre>();

		/// <summary>Los grados de combustible con su postura.</summary>
		public PublishedFuel Fuel { get; init; } = new PublishedFuel();

		/// <summary>El alcance de garantia publicado, sin ninguna posicion de este vehiculo.</summary>
		public PublishedCoverage Coverage { get; init; } = new PublishedCoverage();

		/// <summary>La regla de servicio en otro taller publicada, con su frase y su revision.</summary>
		public PublishedServicingRule ServicingRule { get; init; } = new PublishedServicingRule();

		/// <summary>
		/// Lee la ficha entera por la puerta, en un solo momento, para que la revision que la
		/// pantalla nombra sea la misma de la que salieron las frases que muestra.
		/// </summary>
		public static WhatTheManufacturerPublished ReadThrough(
			DealershipCarAPI api, string language)
		{
			ArgumentNullException.ThrowIfNull(api);

			bool sealedRevision = api.HasASealedRevision();

			var fluids = new List<PublishedFluid>();

			foreach (string kind in AppState.TheSixFluidKinds)
				fluids.Add(new PublishedFluid
				{
					Kind = kind,
					Published = api.IsFluidPublished(kind),
					Says = api.DescribeFluid(kind, language),
					Revision = api.ReadFluidRevision(kind),
				});

			var tyres = new List<PublishedTyre>();

			foreach (string position in AppState.TheThreeTyrePositions)
				tyres.Add(new PublishedTyre
				{
					Position = position,
					Published = api.IsTyrePublished(position),
					Says = api.DescribeTyre(position, language),
					Revision = api.ReadTyreRevision(position),
				});

			return new WhatTheManufacturerPublished
			{
				Model = api.ReadModel(),
				HasASealedRevision = sealedRevision,
				CurrentRevision = sealedRevision ? api.ReadCurrentRevisionCode() : "",
				CurrentRevisionPublishedAt =
					sealedRevision ? TheDay(api.ReadCurrentRevisionPublishedAt()) : "",
				Fluids = fluids,
				Tyres = tyres,
				Fuel = new PublishedFuel
				{
					Published = api.IsFuelPublished(),
					Says = api.DescribeFuel(language),
					MinimumGrade = api.ReadMinimumFuelGrade(),
					PermittedGrades = api.PermittedFuelGrades(),
					ProhibitedGrades = api.ProhibitedFuelGrades(),
					Revision = api.ReadFuelRevision(),
				},
				Coverage = new PublishedCoverage
				{
					Published = api.IsCoveragePublished(),
					Says = api.DescribeCoverageSummary(language),
					Revision = api.ReadCoverageRevision(),
					CoveredSystems = api.CoveredSystems(),
					Exclusions = api.CoverageExclusions(language),
				},
				ServicingRule = new PublishedServicingRule
				{
					Published = api.IsServicingPolicyPublished(),
					Says = api.DescribePublishedServicingElsewhereRule(language),
					Revision = api.ReadServicingPolicyRevision(),
				},
			};
		}

		private static string TheDay(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}

	/// <summary>Lo publicado sobre una clase de fluido, con la revision de la que sale.</summary>
	public sealed class PublishedFluid
	{
		/// <summary>La clase, tal como la escribe el canon.</summary>
		public string Kind { get; init; } = "";

		/// <summary>Si alguna revision sellada publico algo sobre ella.</summary>
		public bool Published { get; init; }

		/// <summary>La frase del dominio. Sin publicar tambien dice algo, y hay que mostrarlo.</summary>
		public string Says { get; init; } = "";

		/// <summary>De que revision salio, o vacio si no hay ninguna que nombrar.</summary>
		public string Revision { get; init; } = "";
	}

	/// <summary>Lo publicado sobre la llanta de una posicion, con su revision.</summary>
	public sealed class PublishedTyre
	{
		/// <summary>La posicion, tal como la escribe el canon.</summary>
		public string Position { get; init; } = "";

		/// <summary>Si alguna revision sellada publico algo sobre ella.</summary>
		public bool Published { get; init; }

		/// <summary>
		/// La frase del dominio, con la presion en la unidad en que se publico. La pantalla no
		/// convierte ninguna unidad a ninguna otra ni ofrece con que hacerlo.
		/// </summary>
		public string Says { get; init; } = "";

		/// <summary>De que revision salio, o vacio si no hay ninguna que nombrar.</summary>
		public string Revision { get; init; } = "";
	}

	/// <summary>Lo publicado sobre el combustible: la frase entera y las tres listas sueltas.</summary>
	public sealed class PublishedFuel
	{
		/// <summary>Si alguna revision sellada publico grados.</summary>
		public bool Published { get; init; }

		/// <summary>La frase del dominio, con las tres posturas adentro.</summary>
		public string Says { get; init; } = "";

		/// <summary>El grado minimo, suelto, que es como se pide en una bomba.</summary>
		public string MinimumGrade { get; init; } = "";

		/// <summary>Los grados permitidos. Vacia dice que no hay ninguno, no que valga cualquiera.</summary>
		public string[] PermittedGrades { get; init; } = new string[0];

		/// <summary>Los prohibidos, que son los que mas importa que salgan enteros.</summary>
		public string[] ProhibitedGrades { get; init; } = new string[0];

		/// <summary>De que revision salio, o vacio si no hay ninguna que nombrar.</summary>
		public string Revision { get; init; } = "";
	}

	/// <summary>
	/// El alcance de garantia publicado y nada mas. Donde esta parado este vehiculo frente a el se
	/// decide con cuatro hechos del carro que este registro no lleva, asi que aca no viene.
	/// </summary>
	public sealed class PublishedCoverage
	{
		/// <summary>Si alguna revision sellada publico alcance.</summary>
		public bool Published { get; init; }

		/// <summary>La frase del dominio, que dice alcance y nunca que un reclamo se vaya a pagar.</summary>
		public string Says { get; init; } = "";

		/// <summary>De que revision salio, o vacio si no hay ninguna que nombrar.</summary>
		public string Revision { get; init; } = "";

		/// <summary>Los codigos que el alcance declara adentro, en el vocabulario del fabricante.</summary>
		public string[] CoveredSystems { get; init; } = new string[0];

		/// <summary>Las exclusiones publicadas, cada una con su texto y su condicion.</summary>
		public string[] Exclusions { get; init; } = new string[0];
	}

	/// <summary>
	/// La regla de servicio en otro taller que publico el fabricante, con su revision. Es la mitad
	/// publicada sola: donde queda este vehiculo frente a ella pide tres conteos que este registro
	/// no lleva, y esa mitad no esta aca ni se estima.
	/// </summary>
	public sealed class PublishedServicingRule
	{
		/// <summary>Si alguna revision sellada publico una regla.</summary>
		public bool Published { get; init; }

		/// <summary>
		/// Lo que dice la regla, o que no hay ninguna publicada. La frase la compone el dominio en
		/// los dos casos.
		/// </summary>
		public string Says { get; init; } = "";

		/// <summary>De que revision salio, o vacio si no hay ninguna que nombrar.</summary>
		public string Revision { get; init; } = "";
	}
}
