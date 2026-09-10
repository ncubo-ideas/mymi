using Gnothi.Host;

namespace Gnothi.Desktop;

// THE HOST, HELD FOR THE WINDOW. One catalog, one dispatcher, shared by every component.
//
// It is deliberately thin: it owns NOTHING of its own about modules. The catalog and the
// dispatcher are the Host's, the very same ones the MCP front end and the phone build. If the
// window had its own way of reading modules there would be two truths, and the one that is
// wrong is always the one nobody looks at.
//
// It mirrors, on the desktop, exactly what MainActivity.Start does on Android:
//   new ModuleCatalog(root)  ->  .Load()  ->  new Dispatcher(catalog, new Meter())
internal sealed class HostState
{
	private readonly string root;

	public HostState(string root) => this.root = root;

	public string Root => root;
	public ModuleCatalog Catalog { get; private set; } = null!;
	public Dispatcher Dispatcher { get; private set; } = null!;
	public IReadOnlyList<string> Warnings { get; private set; } = [];

	// RELOAD, not "load once". The catalog reads installation.json off disk, and the person
	// can edit that file to switch a module on or off -that is the Host's stated way to change
	// consent (HostTools: "It is changed by editing installation.json"). Re-reading on demand
	// is how the window reflects an edit without a restart.
	public void Reload()
	{
		Catalog = new ModuleCatalog(root);
		Warnings = Catalog.Load();
		Dispatcher = new Dispatcher(Catalog, new Meter());
	}

	// EL ICONO POR RUBRO, no por nombre de modulo.
	//
	// Este switch decia 'examenes', 'facturas' y 'compras', con un comentario que aclaraba que
	// el icono NO venia del manifiesto a proposito. Era la tercera copia del mismo mapa -las
	// otras dos en Shell.cs y en ui.html- y su costo se veia: el Host tenia que CONOCER a cada
	// institucion por nombre para pintarla, asi que un modulo de una institucion nueva entraba
	// anonimo. Ahora ella declara su rubro y el Host sigue eligiendo el dibujo.
	//
	// EL ESCRITORIO NO PINTA EL LOGO todavia, y es una diferencia real con el telefono: la
	// aduana -Module.LogoDataUri- ya esta disponible aca, porque vive en Gnothi.Host, pero esta
	// ventana es Razor y no comparte la cara con los dos telefonos. Es la unica capa que no sale
	// gratis, y se dijo por adelantado en el plan.
	public static string Icon(Manifest m) => (m.About?.Category ?? "") switch
	{
		"laboratorio" or "salud" => "\U0001FA7A",   // stethoscope
		"supermercado" => "\U0001F6D2",             // trolley
		"comercio" => "\U0001F9FE",                 // receipt
		"banca" or "seguros" => "\U0001F3E6",       // bank
		"combustible" => "⛽",                  // pump
		"taller" => "\U0001F527",                   // wrench
		"telecom" => "\U0001F4F1",                  // phone
		"educacion" => "\U0001F393",                // cap
		"gobierno" => "\U0001F3DB",                 // classical building
		_ => "\U0001F4E6",                          // box
	};
}
