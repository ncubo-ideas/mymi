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

builder.Services.AddSingleton<ModoDeLaApp>();

builder.Services.AddSingleton<PuenteDeGnothi>(sp => new PuenteDeGnothi(sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddSingleton<PuenteDeJournal>(sp =>
{
	ModoDeLaApp modo = sp.GetRequiredService<ModoDeLaApp>();
	IJSRuntime js = sp.GetRequiredService<IJSRuntime>();

	return modo.Destino is null ? new PuenteDeJournal(js) : new PuenteDeJournal(js, modo.Destino);
});

builder.Services.AddSingleton<EstadoDeLaApp>();

var host = builder.Build();

PuenteDeGnothi puenteDeGnothi = host.Services.GetRequiredService<PuenteDeGnothi>();
if (await puenteDeGnothi.EstaHospedadaAsync())
	host.Services.GetRequiredService<ModoDeLaApp>().Hospedar(puenteDeGnothi);

SuperficieDelModulo.Registrar(host.Services.GetRequiredService<EstadoDeLaApp>());

await host.RunAsync();
