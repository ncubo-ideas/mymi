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

	// The icon per business. A small explicit map -a module not here falls to the generic box.
	// It does NOT come from the manifest on purpose: the icon is the Host's business, not the
	// institution's. Same map as the phone (MainActivity.Icon).
	public static string Icon(string module) => module switch
	{
		"examenes" => "\U0001FA7A",   // stethoscope
		"facturas" => "\U0001F9FE",   // receipt
		"compras" => "\U0001F6D2",    // trolley
		_ => "\U0001F4E6",            // box
	};
}
