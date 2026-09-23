using Foundation;
using UIKit;

namespace Gnothi.iOS;

// THE WINDOW, AND THE TWO PATHS A LINK COMES IN BY. This used to live in AppDelegate.
//
// WHY IT MOVED, and it was not a preference: iOS 27 DEMANDS the UIScene life cycle. An app linked
// against SDK 27 that does not adopt it does not start -- UIKit kills it on purpose with
// EXC_BREAKPOINT in __UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption, before a
// single line of ours runs. Measured on 2026-09-15 on the iPhone 13 Pro and on an iOS 27.0
// simulator; the SAME binary on an iOS 26.5 simulator started fine.
//
// And beware of how the requirement is applied, because it explains why this appeared all at
// once: iOS demands it according to THE SDK THE APP WAS LINKED AGAINST, not the phone version.
// That morning build, made with Xcode 26.6, declared `sdk 26.5` and ran on that same iPhone with
// iOS 27. Xcode auto-updated to 27 along with macOS, the binary started declaring `sdk 27.0`
// -visible with `vtool -show-build-version`- and lost the exemption.
//
// WHAT MUST NOT BE FORGOTTEN WHEN ADOPTING SCENES, and it is the part that breaks in silence:
// UIKit STOPS CALLING `OpenUrl` and `ContinueUserActivity` on the AppDelegate. Declare the
// manifest and fail to move those two, and the app starts -- it looks fixed -- while enrolment by
// link and by file stop working without a single error. That is why both are down here.
[Register("SceneDelegate")]
public sealed class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{
	[Export("window")]
	public UIWindow? Window { get; set; }

	private ViewController? face;

	// THE SCENE CONNECTS: the window is built here. It is the exact replacement for what
	// FinishedLaunching did, with one difference that matters -- the window is created AGAINST THE
	// SCENE and not against UIScreen.MainScreen, which is what ties it to this life cycle.
	[Export("scene:willConnectToSession:options:")]
	public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		if (scene is not UIWindowScene windowScene) return;

		face = new ViewController();
		Window = new UIWindow(windowScene) { RootViewController = face };
		Window.MakeKeyAndVisible();

		// THE COLD START. If the app was closed and a link or a file opened it, that does NOT
		// arrive through the methods below: it comes inside the connection options, once only.
		// It is the equivalent of Android OnCreate, and it is the path most easily forgotten
		// because by hand one always tests with the app already open.
		foreach (NSUserActivity actividad in connectionOptions.UserActivities)
			Atender(actividad);
		Atender(connectionOptions.UrlContexts);
	}

	// THE LINK WITH THE APP ALREADY OPEN, through our scheme or as a file. On the AppDelegate this
	// was OpenUrl; the Android twin is still OnNewIntent.
	[Export("scene:openURLContexts:")]
	public void OpenUrlContexts(UIScene scene, NSSet<UIOpenUrlContext> urlContexts) => Atender(urlContexts);

	// THE UNIVERSAL LINK WITH THE APP ALREADY OPEN. On the AppDelegate it was ContinueUserActivity.
	[Export("scene:continueUserActivity:")]
	public void ContinueUserActivity(UIScene scene, NSUserActivity userActivity) => Atender(userActivity);

	private void Atender(NSSet<UIOpenUrlContext>? contextos)
	{
		if (contextos is null) return;
		foreach (UIOpenUrlContext contexto in contextos)
			Atender(contexto.Url);
	}

	// NO FILTERING BY PATH HERE. Whether the link is /alta or /abrir is decided by whoever handles
	// it: this only tells a file from a navigation, which is the only distinction that changes the destination.
	private void Atender(NSUrl? url)
	{
		if (url is null) return;

		// A DOCUMENT, not a link: «Copy to Mymi» from Files, AirDrop or a chat hands the .gmod
		// over as a file:// url (CFBundleDocumentTypes in Info.plist).
		if (url.IsFileUrl) { face?.HandleFile(url); return; }

		// If the face does not exist yet, ViewController keeps it as pending and handles it when
		// the Host has finished coming up -- just like altaPendiente on Android.
		face?.HandleLink(url.AbsoluteString ?? "");
	}

	private void Atender(NSUserActivity? actividad)
	{
		if (actividad?.ActivityType != NSUserActivityType.BrowsingWeb) return;
		Atender(actividad.WebPageUrl);
	}
}
