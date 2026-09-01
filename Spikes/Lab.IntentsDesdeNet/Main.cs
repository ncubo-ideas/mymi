using Foundation;
using UIKit;

namespace Lab.IntentsDesdeNet;

// LA APP ES CASI IRRELEVANTE. Una pantalla con instrucciones; la prueba pasa afuera, en Atajos
// y en Siri. El C# no llama a la intent, no la conoce y no la necesita: lo unico que hace por
// ella es EXISTIR en el mismo bundle y enlazarla estatica.
public static class Application
{
	private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
	public override UIWindow? Window { get; set; }

	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		Window = new UIWindow(UIScreen.MainScreen.Bounds) { RootViewController = new Pantalla() };
		Window.MakeKeyAndVisible();

		// LO UNICO QUE ESTA APP MIDE POR SI MISMA: si la metadata llego al bundle. El resto lo
		// mide Atajos. Sale por `log stream`, con el mismo prefijo que Gnothi.iOS.
		string metadata = System.IO.Path.Combine(NSBundle.MainBundle.BundlePath, "Metadata.appintents");
		bool hay = System.IO.Directory.Exists(metadata);
		Console.WriteLine($"GNOTHI: Metadata.appintents en el bundle: {(hay ? "SI" : "NO")} ({metadata})");
		if (hay)
			foreach (string f in System.IO.Directory.GetFiles(metadata))
				Console.WriteLine($"GNOTHI:   {System.IO.Path.GetFileName(f)}  {new System.IO.FileInfo(f).Length} bytes");

		return true;
	}
}

public sealed class Pantalla : UIViewController
{
	public override void ViewDidLoad()
	{
		base.ViewDidLoad();
		View!.BackgroundColor = UIColor.FromRGB(0xF5, 0xF3, 0xFB);

		var texto = new UILabel
		{
			Frame = View.Bounds.Inset(24, 80),
			Lines = 0,
			TextColor = UIColor.FromRGB(0x12, 0x15, 0x1B),
			Font = UIFont.SystemFontOfSize(15),
			Text =
				"Gnothi Intents · spike\n\n" +
				"Esta app no hace nada. La prueba es afuera:\n\n" +
				"1. Abrí Atajos y buscá «Gnothi Intents».\n" +
				"   ¿Aparece «Probar desde .NET»?\n\n" +
				"2. Decile a Siri «Probar Gnothi Intents».\n" +
				"   ¿Contesta «Hola desde Swift, adentro de una app de .NET»?\n\n" +
				"Si el 1 falla, mirá el log: dice si Metadata.appintents\n" +
				"entró al bundle. Sin eso, Atajos no tiene cómo saber\n" +
				"que la intent existe.",
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
		};
		View.AddSubview(texto);
	}
}
