using Foundation;
using UIKit;

namespace Gnothi.iOS;

// Sin storyboard y sin scenes, a proposito: una ventana, un view controller, y listo. Un
// spike que falla tiene que fallar por la pregunta que se le hizo, no por el andamio.
[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
	public override UIWindow? Window { get; set; }

	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		Window = new UIWindow(UIScreen.MainScreen.Bounds)
		{
			RootViewController = new ViewController(),
		};
		Window.MakeKeyAndVisible();
		return true;
	}
}
