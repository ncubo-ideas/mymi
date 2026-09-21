using Compras.Wasm;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// El juego de URLs del proveedor sale de wwwroot/appsettings.json, no de una constante
// compilada: la app servida desde el contenedor tiene que poder apuntar a otro host sin
// recompilar.
var proveedor = new ConfiguracionDelProveedor();
builder.Configuration.GetSection("Proveedor").Bind(proveedor);
builder.Services.AddSingleton(proveedor);

// Como se llega al super: su sitio y su WhatsApp. Van en la config por la misma razon que
// el proveedor — son datos de la institucion, y un telefono cambia.
var institucion = new ConfiguracionDeLaInstitucion();
builder.Configuration.GetSection("Institucion").Bind(institucion);
builder.Services.AddSingleton(institucion);

// UN SOLO HttpClient, apuntado al proveedor. La app no es cliente de sus servicios
// generales: la unica lectura que hace es su propia narrativa, y esa la arma
// FuenteHttpDeEventos con una sola forma de URL.
//
// LA BASE ES RELATIVA por defecto, y eso resuelve tres cosas de una (medido por Renan en
// Examenes, con un iPhone): desde un telefono "localhost" es el telefono; una app servida
// por https no puede llamar a un http (contenido mixto); y deja de haber preflight de
// CORS. Resuelta contra el origen que sirvio la app, "eventos/" funciona igual en
// localhost, por IP y por https. Una URL absoluta en la config sigue ganando.
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

builder.Services.AddSingleton<Pantalla>(sp => new Pantalla(sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddSingleton<PuenteDeGnothi>(sp => new PuenteDeGnothi(sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddSingleton<Textos>();

// SINGLETON a proposito: el actor NO se crea por pantalla. Owna estado persistente, se
// construye una vez y se mantiene alcanzable mientras la app vive.
builder.Services.AddSingleton<EstadoDeLaApp>();

var host = builder.Build();

using (var deLaCaratula = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
{
	await host.Services.GetRequiredService<Textos>().NegociarAsync(
		host.Services.GetRequiredService<IJSRuntime>(), deLaCaratula);
}

// EN QUE HOSPEDAJE CORRE, decidido ANTES del render: el contenedor construye el puente del
// journal con el destino ya puesto, y despues seria tarde. Se pregunta por la presencia del
// puente que Gnothi inyecta, no por el origen.
var puenteDeGnothi = host.Services.GetRequiredService<PuenteDeGnothi>();
if (await puenteDeGnothi.EstaHospedada())
    host.Services.GetRequiredService<ModoDeLaApp>().Hospedar(puenteDeGnothi);

// EL SINGLETON VIVO, al alcance de la superficie que Gnothi llama. DotNet.invokeMethodAsync
// solo alcanza metodos estaticos, asi que la superficie necesita esta referencia; no hay otra
// manera de que el nativo llegue al actor sin abrir un segundo.
SuperficieDelModulo.Registrar(host.Services.GetRequiredService<EstadoDeLaApp>());

await host.RunAsync();
