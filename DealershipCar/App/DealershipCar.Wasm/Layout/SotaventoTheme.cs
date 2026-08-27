using MudBlazor;

namespace DealershipCar.Wasm.Layout
{
	public static class SotaventoTheme
	{
		private const string Marca = "#1F3A5F";
		private const string MarcaOscura = "#16273F";
		private const string Tinta = "#16202C";
		private const string TintaSuave = "#5B6B7C";
		private const string Papel = "#F4F6F9";
		private const string Tarjeta = "#FFFFFF";
		private const string Borde = "#DFE4EA";
		private const string Atencion = "#8A5300";
		private const string Problema = "#97231F";
		private const string Bien = "#1D6B45";

		public static readonly MudTheme Theme = new MudTheme
		{
			PaletteLight = new PaletteLight
			{
				Primary = Marca,
				Secondary = TintaSuave,
				AppbarBackground = MarcaOscura,
				AppbarText = "#FFFFFF",
				Background = Papel,
				Surface = Tarjeta,
				Error = Problema,
				Warning = Atencion,
				Success = Bien,
				Info = Marca,
				TextPrimary = Tinta,
				TextSecondary = TintaSuave,
				Divider = Borde,
				LinesDefault = Borde,
				TableLines = Borde,
			},

			PaletteDark = new PaletteDark
			{
				Primary = "#7FA6D9",
				Secondary = "#9FB0C2",
				AppbarBackground = "#101A26",
				AppbarText = "#E6EDF6",
				Background = "#0E1620",
				Surface = "#16202C",
				Error = "#F0736D",
				Warning = "#E0A143",
				Success = "#5BC08C",
				Info = "#7FA6D9",
				TextPrimary = "#E6EDF6",
				TextSecondary = "#9FB0C2",
				Divider = "#243444",
				LinesDefault = "#243444",
				TableLines = "#243444",
			},

			Typography = new Typography
			{
				Default = new DefaultTypography
				{
					FontFamily = new[] { "Segoe UI", "system-ui", "-apple-system", "sans-serif" },
				},
			},

			LayoutProperties = new LayoutProperties
			{
				DefaultBorderRadius = "10px",
				AppbarHeight = "60px",
			},
		};

		public static Color ColorOf(string dueState) => dueState switch
		{
			"Overdue" => Color.Error,
			"Due" => Color.Warning,
			"DueSoon" => Color.Info,
			"Completed" => Color.Success,
			_ => Color.Default,
		};

		public static string IconOf(string dueState) => dueState switch
		{
			"Overdue" => Icons.Material.Rounded.Error,
			"Due" => Icons.Material.Rounded.Schedule,
			"DueSoon" => Icons.Material.Rounded.Upcoming,
			"Completed" => Icons.Material.Rounded.CheckCircle,
			"Deferred" => Icons.Material.Rounded.Snooze,
			"Superseded" => Icons.Material.Rounded.HistoryToggleOff,
			_ => Icons.Material.Rounded.RadioButtonUnchecked,
		};
	}
}
