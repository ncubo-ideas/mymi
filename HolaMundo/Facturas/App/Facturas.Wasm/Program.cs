using Facturas.Wasm;
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

// UN SOLO HttpClient, apuntado al proveedor. La app no es cliente de sus servicios
// generales: la unica lectura que hace es su propia narrativa, y esa la arma
// FuenteHttpDeEventos con una sola forma de URL.
builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(proveedor.BaseUrl) });

builder.Services.AddSingleton<PuenteDeJournal>(sp => new PuenteDeJournal(sp.GetRequiredService<IJSRuntime>()));

// SINGLETON a proposito: el actor NO se crea por pantalla. Owna estado persistente, se
// construye una vez y se mantiene alcanzable mientras la app vive.
builder.Services.AddSingleton<EstadoDeLaApp>();

await builder.Build().RunAsync();
