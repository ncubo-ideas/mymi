using UIKit;

namespace Gnothi.iOS;

// The entry point, and it does nothing else. Everything interesting is in ViewController.cs, and
// further down still -- in Gnothi.MobileShared/Shell.cs, the file Android and iOS share.
public static class Application
{
	private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}
