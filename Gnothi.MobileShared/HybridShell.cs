

namespace Gnothi.Host;

// LA CASCARA HIBRIDA: la cara de la institucion en el WebView, su superficie cargada EN
// PROCESO, y el motor prestado por Gnothi.
//
// POR QUE EXISTE. Las otras dos son medias respuestas para el telefono:
//
//   cargo     presta el motor pero no trae pantalla -- el Host renderiza view.json
//   hospedada trae la pantalla pero carga su propio motor: medido sobre examenes, correr .NET
//             en un navegador es el 88,8% del bundle y el motor otro 8,6%, contra 188 KB del
//             mismo modulo en proceso
//
// El hibrido es las dos mitades que nunca se habian dejado juntar. Y lo que gana no es peso:
// es que hay UN SOLO MOTOR sobre el journal, o sea un solo juego de semaforos y un solo dueno.
//
// QUIEN CONTESTA Y QUIEN PINTA, y es toda la decision de esta clase:
//
//   al asistente le contesta LA SUPERFICIE en proceso  -> Invoke y ActualTools delegan ahi
//   a la persona le pinta LA CARA en el WebView        -> vive, pero no contesta nada
//
// Es exactamente al reves que en la hospedada, donde la pagina es la que contesta. Aca la
// pagina PREGUNTA, por el verbo `consultar` del puente, y la respuesta sale de la misma
// superficie que contesta por las otras puertas. Una sola implementacion, tres puertas.
//
// LOS ARGUMENTOS, Y SU ORDEN NO ES EL QUE UNO ESCRIBIRIA PRIMERO.
//
//   [0]  la carpeta del bundle        lo que se sirve
//   [1]  el endpoint del publicador   `baseUrl|resource|id`
//   [2]  el dll de entrada            lo que se carga
//
// La convencion de las otras cascaras es que Arguments[0] es "la cosa", y aca la cosa deberia
// ser el dll porque es lo que contesta. Pero [0] y [1] ya los lee todo el lado web -- y sobre
// todo WebModuleHost.Open, que abre la pantalla desde el catalogo y no sabe de formas. Poner
// el dll ahi haria que abrir el modulo intentara servir un .dll como carpeta, y fallaria lejos
// de aca. El consumidor nuevo es el que se acomoda.
//
// ATENCION -- ESTO NO ESTA MEDIDO EN ANDROID. InProcessShell se compila dentro del APK y el
// stub de Android apunta a el ("Use \"type\": \"in-process\""), pero no hay ninguna corrida
// que demuestre que Mono carga un ensamblado ajeno en el telefono. Es justamente lo que este
// camino existe para averiguar.
//
// Y HAY UNA OBJECION QUE ESTA CLASE NO RESUELVE: un dll de la institucion ejecutandose nativo
// es codigo que llega de afuera. Contra el texto de Google Play -leido el 2026-09-16, ver B11-
// eso da acceso DIRECTO a las APIs de Android, y la excepcion de la politica es justamente para
// lo que da acceso INDIRECTO ("JavaScript in a webview"). El hibrido hereda esa objecion entera.
//
// Lo que la caja de wasm (F5) haria distinto es exactamente esto: un intermediario que solo
// concede los imports que Gnothi decide. Eso SI encaja en el texto.
internal sealed class HybridShell : IShell
{
	private readonly InProcessShell surface;
	private readonly WebViewShell face;

	public HybridShell(string name, Shell shell, string grantedFolder)
	{
		// La cara lee [0] y [1] tal cual: para WebViewShell este Shell es indistinguible del de
		// un modulo hospedado, y esa es la idea -- no se entera de que es hibrido.
		face = new WebViewShell(name, shell, grantedFolder);

		// Y la superficie recibe el dll movido a la primera posicion, que es la unica que
		// InProcessShell lee. Ninguna de las dos sabe de la otra.
		//
		// keepLoaded: la cara pregunta varias veces seguidas mientras la persona mira, y recibe
		// entregas en el medio. Descargar el ensamblado entre pintadas pone dos actores sobre el
		// mismo journal -- el Unload es cooperativo y no completa si el anterior dejo referencias
		// vivas -- y eso se ve como `FileNotFoundException: Temp file not found`. El modulo queda
		// cargado mientras la cara esta abierta, y se suelta al desalojarla.
		surface = new InProcessShell(name, SoloElDll(shell), grantedFolder, keepLoaded: true);
	}

	private static Shell SoloElDll(Shell shell) => new()
	{
		Type = shell.Type,
		Command = shell.Command,
		Arguments = shell.Arguments.Count > 2 ? [shell.Arguments[2]] : [],
	};

	// CORRE SI CORRE LA SUPERFICIE. La cara puede no estar viva -la persona no abrio la
	// pantalla- y el modulo sigue contestando: es la diferencia con la hospedada, donde sin
	// WebView no hay quien conteste.
	public bool IsRunning => surface.IsRunning;

	public Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> surface.Invoke(tool, arguments, accepts, ct);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct) => surface.ActualTools(ct);

	// LA ENTREGA VA A LA SUPERFICIE, y es la unica de las tres cascaras donde hace falta decirlo.
	//
	// En la hospedada el cartero le entrega a la PAGINA -- `window.gnothi.deliver` -- porque la
	// pagina lleva el motor. Aca la cara no tiene ninguno, asi que el sobre va al unico lado que
	// puede asentarlo. Es el mismo cartero, el mismo sobre y el mismo endpoint fijado al instalar:
	// lo unico que cambia es quien lo recibe.
	public Task<string> Deliver(string envelopeJson, CancellationToken ct) =>
		surface.Deliver(envelopeJson, ct);

	// Y HASTA DONDE YA LEYO, que sale del mismo actor que asienta. Es lo que hace que el cartero
	// pida de ahi en adelante: la cara no tiene con que saberlo, y tampoco con que moverlo.
	public Task<long> Cursor(CancellationToken ct) => surface.Cursor(ct);

	// Y LO QUE PIDE LA CARA. Va a la misma superficie que contesta al asistente, pero a OTRO
	// conjunto de metodos: los marcados [View]. Una sola implementacion del dominio, dos
	// publicos, y ninguno puede entrar por la puerta del otro.
	public Task<object> View(
		string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct) =>
		surface.View(name, arguments, accepts);

	// La cara, para quien tenga que abrirla. No la abre esta clase: abrirla es un acto de la
	// persona, no del despacho.
	internal WebViewShell Face => face;

	public async ValueTask DisposeAsync()
	{
		// Las dos, y la cara primero: desalojar un WebView con el runtime adentro libera ~150 MB
		// de PSS, y no tiene sentido sostenerlo mientras se descarga el contexto del dll.
		await face.DisposeAsync();
		await surface.DisposeAsync();
	}
}
