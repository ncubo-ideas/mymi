namespace Concesionario.App.Views
{
	// THE CLOSED SETS THE SCREEN WALKS. They lived in AppState, which is the Blazor app's own
	// state, and the projections in this folder read them from there. That worked while the only
	// reader was that screen; it stopped working the day a second surface needed the same
	// projections, because a cargo or hybrid module ships Concesionario.App.dll and never the
	// Wasm assembly.
	//
	// THEY ARE DOMAIN VOCABULARY, NOT SCREEN STATE. A kind of service, a fluid, a tyre position:
	// none of them is a rendering decision, and none of them changes because the screen changes.
	// The nine kinds are what the manufacturer publishes a schedule for; the six fluids and the
	// three positions are what a revision can speak about.
	//
	// AppState keeps its own names pointing here, so every .razor that already named them
	// through AppState keeps working and reads the same list.
	public static class TheClosedSets
	{
		public static readonly string[] TheNineKinds =
		{
			"Inspection", "FluidChange", "FilterChange", "WearPartReplacement", "TimedComponent",
			"SoftwareUpdate", "RecallRemedy", "Diagnostic", "Repair"
		};

		public static readonly string[] TheFourProfiles =
		{
			"Standard", "SevereDuty", "LowDistance", "Commercial"
		};

		public static readonly string[] TheFiveDeferralReasons =
		{
			"Cost", "Timing", "SecondOpinion", "NotConvinced", "WillDoElsewhere"
		};

		public static readonly string[] TheSixFluidKinds =
		{
			"EngineOil", "BrakeFluid", "Coolant", "TransmissionFluid", "PowerSteeringFluid",
			"WasherFluid"
		};

		public static readonly string[] TheThreeTyrePositions =
		{
			"Front", "Rear", "Spare"
		};
	}
}
