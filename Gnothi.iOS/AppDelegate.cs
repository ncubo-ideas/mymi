using Foundation;
using UIKit;

namespace Gnothi.iOS;

// Sin storyboard y sin scenes, a proposito: una ventana, un view controller, y listo. Un
// spike que falla tiene que fallar por la pregunta que se le hizo, no por el andamio.
[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
	public override UIWindow? Window { get; set; }

	private ViewController? cara;

	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		cara = new ViewController();

		Window = new UIWindow(UIScreen.MainScreen.Bounds)
		{
			RootViewController = cara,
		};
		Window.MakeKeyAndVisible();
		return true;
	}

	// EL ENLACE DE LA INSTITUCION LLEGA ACA, y es el gemelo de OnCreate/OnNewIntent en Android.
	//
	// LOS DOS CAMINOS SON ESTE MISMO. En Android son dos metodos distintos -OnCreate cuando la
	// app estaba cerrada, OnNewIntent cuando ya estaba abierta- y los dos habia que probarlos
	// por separado porque son codigo distinto. En iOS los dos entran por
	// ContinueUserActivity: con la app cerrada, UIKit la lanza y DESPUES llama a esto. Un
	// camino menos que equivocarse, pero hay que probar los dos estados igual, porque lo que
	// cambia es si la cara ya existe.
	//
	// NO SE FILTRA POR PATH ACA. Que el enlace sea /alta o /abrir lo decide quien lo atiende:
	// este metodo solo se asegura de que sea una navegacion web de verdad y la pasa entera.
	public override bool ContinueUserActivity(
		UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
	{
		if (userActivity.ActivityType != NSUserActivityType.BrowsingWeb) return false;

		NSUrl? url = userActivity.WebPageUrl;
		if (url is null) return false;

		// Si la cara todavia no existe, la propia ViewController lo guarda como pendiente y lo
		// atiende cuando el Host termino de levantar -- igual que altaPendiente en Android.
		cara?.AtenderEnlace(url.AbsoluteString ?? "");
		return true;
	}
}
