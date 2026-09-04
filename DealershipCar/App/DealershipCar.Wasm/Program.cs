using DealershipCar.Wasm;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

var dealership = new DealershipSettings();
builder.Configuration.GetSection("Dealership").Bind(dealership);
builder.Services.AddSingleton(dealership);

builder.Services.AddSingleton(_ => new HttpClient
{
	BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), dealership.BaseUrl),
});

// EL PUENTE DEL JOURNAL SE CONSTRUYE CON SU DESTINO YA ELEGIDO. Suelta, el destino es el
// storage del browser y de eso se ocupa el puente mismo; hospedada, son los archivos de
// Gnothi. Por eso el modo se decide antes de que nadie resuelva este singleton.
builder.Services.AddSingleton<HostingMode>();
builder.Services.AddSingleton<JournalBridge>(sp =>
{
    var mode = sp.GetRequiredService<HostingMode>();
    var js = sp.GetRequiredService<IJSRuntime>();
    return mode.Destination is null ? new JournalBridge(js) : new JournalBridge(js, mode.Destination);
});

// LA CREDENCIAL ANTE EL CONCESIONARIO vive aca -storage del browser del origen-, no en el
// journal. Singleton por la misma razon que el bridge: es estado de la instalacion.
builder.Services.AddSingleton<CredentialStore>(sp => new CredentialStore(sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddSingleton<GnothiBridge>(sp => new GnothiBridge(sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddSingleton<AppState>();

var host = builder.Build();

// EN QUE HOSPEDAJE CORRE, decidido ANTES del render: el contenedor construye el puente del
// journal con el destino ya puesto, y despues seria tarde. Se pregunta por la presencia del
// puente que Gnothi inyecta, no por el origen.
var gnothi = host.Services.GetRequiredService<GnothiBridge>();
if (await gnothi.IsHostedAsync())
    host.Services.GetRequiredService<HostingMode>().HostIn(gnothi);

// EL SINGLETON VIVO, al alcance de la superficie que Gnothi llama. DotNet.invokeMethodAsync
// solo alcanza metodos estaticos, asi que la superficie necesita esta referencia; no hay otra
// manera de que el nativo llegue al actor sin abrir un segundo.
ModuleSurface.Register(host.Services.GetRequiredService<AppState>());

await host.RunAsync();
