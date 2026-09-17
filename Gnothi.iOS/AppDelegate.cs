using Foundation;
using UIKit;

namespace Gnothi.iOS;

// EL DELEGADO DE LA APP, Y YA CASI NO HACE NADA. Desde que la app adopta el ciclo de vida de
// UIScene -obligatorio en iOS 27, ver SceneDelegate.cs-, lo que antes estaba aca se mudo:
//
//   - la ventana y el ViewController los arma ahora SceneDelegate.WillConnect;
//   - el enlace de la institucion y el archivo .gmod entran por SceneDelegate, porque UIKit
//     DEJA DE LLAMAR OpenUrl y ContinueUserActivity en esta clase apenas hay scenes. Dejarlos
//     escritos aca seria peor que borrarlos: compilarian, se leerian como el camino vigente, y
//     no correrian nunca.
//
// Sigue existiendo porque Main.cs se lo pasa a UIApplication.Main y porque es el lugar de lo que
// es de la APP y no de una ventana. Hoy no hay nada de eso; cuando lo haya, va aca.
//
// La configuracion de la escena NO se resuelve en codigo: la declara Info.plist en
// UIApplicationSceneManifest, que nombra a SceneDelegate. Una sola fuente, y se ve sin compilar.
[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) => true;
}
