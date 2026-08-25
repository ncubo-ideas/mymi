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

builder.Services.AddSingleton<PuenteDeJournal>(sp => new PuenteDeJournal(sp.GetRequiredService<IJSRuntime>()));

// SINGLETON a proposito: el actor NO se crea por pantalla. Owna estado persistente, se
// construye una vez y se mantiene alcanzable mientras la app vive.
builder.Services.AddSingleton<EstadoDeLaApp>();

await builder.Build().RunAsync();
