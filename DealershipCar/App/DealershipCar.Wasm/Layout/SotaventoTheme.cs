using MudBlazor;

namespace DealershipCar.Wasm.Layout
{
	// EL TEMA DE LA MARCA, en un solo lugar. Los colores son los que ya tenia css/app.css como
	// variables -- la marca no cambio al cambiar de biblioteca de componentes -- solo que ahora
	// viven donde MudBlazor los lee y los propaga a cada componente.
	//
	// LA TIPOGRAFIA ES LA DEL SISTEMA, no Roboto. La guia de MudBlazor enlaza la fuente desde
	// Google; esta app es una PWA que promete abrir sin red, y una fuente que se pide por red es
	// justo lo que no se puede prometer. index.html dice lo mismo del lado del HTML.
	public static class SotaventoTheme
	{
		// LOS COLORES DE LA HOJA ANTERIOR, con el mismo nombre que tenian ahi para que la
		// correspondencia se pueda seguir: marca, atencion, problema, bien.
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

			// EL MODO OSCURO NO ES ADORNO: esta app se abre en un telefono tanto como en un
			// escritorio, y un tablero blanco de noche es el que la persona cierra. Los estados
			// no se pintan con estos colores directamente -- se piden por nombre semantico
			// (Error, Warning, Success), asi que el tablero se lee igual en los dos modos sin
			// una segunda tabla de colores que mantener.
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

		// EL ESTADO SE PINTA POR EL NOMBRE QUE COMPONE EL DOMINIO, y lo que no reconoce cae al
		// color neutro. Es la misma propiedad que tenia la hoja de CSS -- ahi eran clases
		// `.state--notdue`, `.state--overdue` -- y existe por la misma razon: si el dominio agrega
		// un estado, la pantalla lo muestra apagado en vez de romperse.
		//
		// LOS DOS QUE SE PARECEN Y NO SON LO MISMO: DueSoon es un aviso de cortesia y NUNCA una
		// parada obligatoria, Overdue si. Por eso DueSoon sale Info y no Warning: pintarlo del
		// mismo color que Due convertiria una cortesia en una obligacion, que es exactamente el
		// error que el dominio se toma el trabajo de distinguir.
		public static Color ColorOf(string dueState) => dueState switch
		{
			"Overdue" => Color.Error,
			"Due" => Color.Warning,
			"DueSoon" => Color.Info,
			"Completed" => Color.Success,
			_ => Color.Default,
		};

		// El icono acompana al color por la misma razon por la que el color acompana al nombre:
		// un tablero que solo distingue por color no lo distingue para quien no ve el color.
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
