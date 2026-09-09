using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Municipalidad.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var ajustes = new AjustesDeLaMunicipalidad();
builder.Configuration.GetSection("Municipalidad").Bind(ajustes);
builder.Services.AddSingleton(ajustes);

builder.Services.AddSingleton(_ => new HttpClient
{
	BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), ajustes.BaseUrl),
});

builder.Services.AddSingleton<PuenteDeJournal>(sp => new PuenteDeJournal(sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddSingleton<EstadoDeLaApp>();

await builder.Build().RunAsync();
