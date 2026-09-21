using UIKit;

namespace Gnothi.iOS;

// El punto de entrada, y no hace nada mas. Todo lo interesante esta en ViewController.cs, y
// mas abajo todavia -- en Gnothi.MobileShared/Shell.cs, que es el archivo que Android e iOS comparten.
public static class Application
{
	private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}
