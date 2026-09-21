using Examenes.Wasm;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// El juego de URLs del proveedor sale de wwwroot/appsettings.json, no de una constante
// compilada: la app servida desde el contenedor tiene que poder apuntar a otro host sin
// recompilar. APP2 cambia estos tres valores y su dominio, nada mas.
var proveedor = new ConfiguracionDelProveedor();
builder.Configuration.GetSection("Proveedor").Bind(proveedor);
builder.Services.AddSingleton(proveedor);

// Como se llega a Gnothi desde la app suelta. Tambien de appsettings: el dominio y el
// paquete cambian por despliegue.
var gnothi = new ConfiguracionDeGnothi();
builder.Configuration.GetSection("Gnothi").Bind(gnothi);
builder.Services.AddSingleton(gnothi);

// UN SOLO HttpClient, apuntado al proveedor. La app no es cliente de sus servicios
// generales: la unica lectura que hace es su propia narrativa, y esa la arma
// FuenteHttpDeEventos con una sola forma de URL.
//
// LA BASE PUEDE SER RELATIVA, y por defecto lo es. Una URL absoluta con localhost adentro
// solo funciona en la maquina que sirve la app: desde un telefono, "localhost" es el
// telefono. Y aunque se pusiera la IP, una app servida por https no puede llamar a un
// http -el browser lo bloquea por contenido mixto-.
//
// Resuelta contra el origen que sirvio la app, "eventos/" apunta al mismo host, el mismo
// protocolo y el mismo puerto, asi que funciona igual en localhost, por IP y por https.
// Una URL absoluta en la config sigue ganando, para poder apuntar a otro host sin
// recompilar.
builder.Services.AddSingleton(_ => new HttpClient
{
	BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), proveedor.BaseUrl),
});

// EL PUENTE DEL JOURNAL SE CONSTRUYE CON SU DESTINO YA ELEGIDO. Suelta, el destino es el
// storage del browser y de eso se ocupa el puente mismo; hospedada, son los archivos de
// Gnothi. Por eso el modo se decide antes de que nadie resuelva este singleton.
builder.Services.AddSingleton<ModoDeLaApp>();
builder.Services.AddSingleton<PuenteDeJournal>(sp =>
{
	var modo = sp.GetRequiredService<ModoDeLaApp>();
	var js = sp.GetRequiredService<IJSRuntime>();
	return modo.Destino is null ? new PuenteDeJournal(js) : new PuenteDeJournal(js, modo.Destino);
});

// LA CREDENCIAL ANTE EL PROVEEDOR vive aca -storage del browser del origen-, no en el
// journal. Singleton por la misma razon que el puente: es estado de la instalacion.
builder.Services.AddSingleton<AlmacenDeCredencial>(sp => new AlmacenDeCredencial(sp.GetRequiredService<IJSRuntime>()));

// SINGLETON a proposito: el actor NO se crea por pantalla. Owna estado persistente, se
// construye una vez y se mantiene alcanzable mientras la app vive.
builder.Services.AddSingleton<PuenteDeGnothi>(sp => new PuenteDeGnothi(sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddSingleton<EstadoDeLaApp>();

// LO QUE ESTA PANTALLA DICE CON SUS PROPIAS PALABRAS, en el idioma que la persona eligio en su
// dispositivo. Singleton por la misma razon que el resto: el catalogo se negocia una vez, al
// arrancar, y no por pantalla.
builder.Services.AddSingleton<Textos>();

// EL SINGLETON VIVO, al alcance de la superficie que Gnothi llama. DotNet.invokeMethodAsync
// solo alcanza metodos estaticos, asi que la superficie necesita esta referencia; no hay
// otra manera de que el nativo llegue al actor sin abrir un segundo.
var host = builder.Build();

// EL IDIOMA SE NEGOCIA ANTES DEL RENDER, igual que el hospedaje: una pantalla que se pinta y
// despues cambia de idioma es un parpadeo que nadie pidio. El HttpClient es OTRO que el del
// proveedor -este apunta al origen que sirvio la app, que es donde vive wwwroot/texto-.
await host.Services.GetRequiredService<Textos>().NegociarAsync(
	host.Services.GetRequiredService<IJSRuntime>(),
	new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// EN QUE HOSPEDAJE CORRE, decidido ANTES del render: el contenedor construye el puente del
// journal con el destino ya puesto, y despues seria tarde. Se pregunta por la presencia del
// puente que Gnothi inyecta, no por el origen.
var puenteDeGnothi = host.Services.GetRequiredService<PuenteDeGnothi>();
if (await puenteDeGnothi.EstaHospedada())
	host.Services.GetRequiredService<ModoDeLaApp>().Hospedar(puenteDeGnothi);

SuperficieDelModulo.Registrar(host.Services.GetRequiredService<EstadoDeLaApp>());

await host.RunAsync();
