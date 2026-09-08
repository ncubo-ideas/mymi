using Foundation;
using UIKit;

namespace Gnothi.iOS;

// Sin storyboard y sin scenes, a proposito: una ventana, un view controller, y listo. Un
// spike que falla tiene que fallar por la pregunta que se le hizo, no por el andamio.
[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
	public override UIWindow? Window { get; set; }

	private ViewController? face;

	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		face = new ViewController();

		Window = new UIWindow(UIScreen.MainScreen.Bounds)
		{
			RootViewController = face,
		};
		Window.MakeKeyAndVisible();
		return true;
	}

	// THE INSTITUTION'S LINK ARRIVES HERE, the twin of OnCreate/OnNewIntent on Android.
	//
	// BOTH PATHS ARE THIS ONE METHOD. On Android they are two different methods -- OnCreate
	// when the app was closed, OnNewIntent when it was already open -- and both had to be
	// tested separately because they are different code. On iOS both arrive through
	// ContinueUserActivity: with the app closed, UIKit launches it and THEN calls this. One
	// less path to get wrong, but the two states still need testing, because what changes is
	// whether the face already exists.
	//
	// NO PATH FILTERING HERE. Whether the link is /alta or /abrir is for whoever attends it to
	// decide: this method only makes sure it is a real web navigation, and passes it whole.
	public override bool ContinueUserActivity(
		UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
	{
		if (userActivity.ActivityType != NSUserActivityType.BrowsingWeb) return false;

		NSUrl? url = userActivity.WebPageUrl;
		if (url is null) return false;

		// If the face does not exist yet, ViewController itself keeps this as pending and
		// attends it once the Host has come up -- the same as altaPendiente on Android.
		face?.HandleLink(url.AbsoluteString ?? "");
		return true;
	}

	// THE SAME LINK, ARRIVING BY OUR OWN SCHEME. The reasoning and the security note are on
	// CFBundleURLTypes in Info.plist; the short version is that go.gnothi.app does not resolve,
	// so the universal link cannot be tested at all and iOS has no `pm set-app-links`.
	//
	// IT GOES TO THE SAME HandleLink, deliberately: a second parser is a second thing that can
	// be wrong, and the two would drift. Verified on .NET 10 that System.Uri gives a custom
	// scheme the same Query as https -- `gnothi://alta?bundle=...` yields the identical bundle
	// URL -- so HandleLink needed no change whatsoever.
	//
	// Only the schemes declared in Info.plist reach this, so there is nothing to filter here.
	public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
	{
		if (url is null) return false;

		face?.HandleLink(url.AbsoluteString ?? "");
		return true;
	}
}
