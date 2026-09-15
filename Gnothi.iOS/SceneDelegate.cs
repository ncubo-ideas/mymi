using Foundation;
using UIKit;

namespace Gnothi.iOS;

// LA VENTANA, Y LOS DOS CAMINOS POR LOS QUE ENTRA UN ENLACE. Antes esto vivia en AppDelegate.
//
// POR QUE SE MUDO, y no fue una preferencia: iOS 27 EXIGE el ciclo de vida de UIScene. Una app
// enlazada contra el SDK 27 que no lo adopta no arranca -- UIKit la mata a proposito con
// EXC_BREAKPOINT en __UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption, antes de que
// corra una sola linea nuestra. Medido el 2026-09-15 en el iPhone 13 Pro y en un simulador de
// iOS 27.0; el MISMO binario en un simulador de iOS 26.5 arrancaba bien.
//
// Y ojo con como se aplica el requisito, porque explica por que esto aparecio de golpe: iOS lo
// exige segun EL SDK CONTRA EL QUE SE ENLAZO la app, no segun la version del telefono. El build
// de la manana de ese dia, hecho con Xcode 26.6, declaraba `sdk 26.5` y corria en ese mismo
// iPhone con iOS 27. Xcode se auto-actualizo a 27 con macOS, el binario paso a declarar
// `sdk 27.0` -se ve con `vtool -show-build-version`- y perdio la excepcion.
//
// LO QUE HAY QUE NO OLVIDAR AL ADOPTAR SCENES, y es la parte que rompe en silencio: UIKit DEJA DE
// LLAMAR `OpenUrl` y `ContinueUserActivity` en el AppDelegate. Si uno declara el manifiesto y no
// mueve esos dos, la app arranca -- parece arreglada -- y el alta por enlace y por archivo dejan
// de funcionar sin un solo error. Por eso los dos estan aca abajo.
[Register("SceneDelegate")]
public sealed class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{
	[Export("window")]
	public UIWindow? Window { get; set; }

	private ViewController? face;

	// LA ESCENA SE CONECTA: aca se arma la ventana. Es el reemplazo exacto de lo que hacia
	// FinishedLaunching, con una diferencia que importa -- la ventana se crea CONTRA LA ESCENA y
	// no contra UIScreen.MainScreen, que es lo que la ata a este ciclo de vida.
	[Export("scene:willConnectToSession:options:")]
	public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		if (scene is not UIWindowScene windowScene) return;

		face = new ViewController();
		Window = new UIWindow(windowScene) { RootViewController = face };
		Window.MakeKeyAndVisible();

		// EL ARRANQUE EN FRIO. Si la app estaba cerrada y la abrio un enlace o un archivo, eso NO
		// llega por los metodos de abajo: viene adentro de las opciones de conexion, una sola vez.
		// Es el equivalente del OnCreate de Android, y es el camino que mas facil se olvida
		// porque a mano uno siempre prueba con la app ya abierta.
		foreach (NSUserActivity actividad in connectionOptions.UserActivities)
			Atender(actividad);
		Atender(connectionOptions.UrlContexts);
	}

	// EL ENLACE CON LA APP YA ABIERTA, por nuestro esquema o como archivo. En el AppDelegate esto
	// era OpenUrl; el gemelo de Android sigue siendo OnNewIntent.
	[Export("scene:openURLContexts:")]
	public void OpenUrlContexts(UIScene scene, NSSet<UIOpenUrlContext> urlContexts) => Atender(urlContexts);

	// EL UNIVERSAL LINK CON LA APP YA ABIERTA. En el AppDelegate era ContinueUserActivity.
	[Export("scene:continueUserActivity:")]
	public void ContinueUserActivity(UIScene scene, NSUserActivity userActivity) => Atender(userActivity);

	private void Atender(NSSet<UIOpenUrlContext>? contextos)
	{
		if (contextos is null) return;
		foreach (UIOpenUrlContext contexto in contextos)
			Atender(contexto.Url);
	}

	// NO SE FILTRA POR RUTA ACA. Si el enlace es /alta o /abrir lo decide quien lo atiende: esto
	// solo separa un archivo de una navegacion, que es la unica distincion que cambia el destino.
	private void Atender(NSUrl? url)
	{
		if (url is null) return;

		// UN DOCUMENTO, no un enlace: «Copiar en Mymi» desde Archivos, AirDrop o un chat entrega
		// el .gmod como una url file:// (CFBundleDocumentTypes en Info.plist).
		if (url.IsFileUrl) { face?.HandleFile(url); return; }

		// Si la cara todavia no existe, ViewController se lo guarda como pendiente y lo atiende
		// cuando el Host termino de levantar -- igual que altaPendiente en Android.
		face?.HandleLink(url.AbsoluteString ?? "");
	}

	private void Atender(NSUserActivity? actividad)
	{
		if (actividad?.ActivityType != NSUserActivityType.BrowsingWeb) return;
		Atender(actividad.WebPageUrl);
	}
}
