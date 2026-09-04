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

		// LO QUE 6b VA A LEER. El C# deja un archivo en la carpeta propia de la app; la intent
		// de Swift lo lee y lo dice en voz alta. Si lo dice, el camino .NET -> Swift dentro de
		// UNA sola app esta probado -- que es justo lo que Gnothi.iOS haria con la foto de
		// Instantanea, pero sin App Group, porque ya no hace falta que sean dos apps.
		//
		// Misma carpeta que Swift busca: Library/Application Support. Si los dos lados no
		// coinciden, 6b lo dice con la ruta, no falla en silencio.
		string soporte = NSSearchPath.GetDirectories(NSSearchPathDirectory.ApplicationSupportDirectory, NSSearchPathDomain.User, true)[0];
		System.IO.Directory.CreateDirectory(soporte);
		string desdeNet = System.IO.Path.Combine(soporte, "desde-net.txt");
		System.IO.File.WriteAllText(desdeNet, $"escrito por C# el {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}, proceso {Environment.ProcessId}");
		Console.WriteLine($"GNOTHI: escribi {desdeNet}");

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
				"   ¿Contesta «Hola desde Swift…» Y repite lo que\n" +
				"   el C# dejó escrito (fecha y proceso)?\n" +
				"   Si repite eso, .NET → Swift en UNA app está probado.\n\n" +
				"Si el 1 falla, mirá el log: dice si Metadata.appintents\n" +
				"entró al bundle. Sin eso, Atajos no tiene cómo saber\n" +
				"que la intent existe.",
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
		};
		View.AddSubview(texto);
	}
}
