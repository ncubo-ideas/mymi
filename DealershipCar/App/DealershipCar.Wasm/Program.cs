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

builder.Services.AddSingleton<JournalBridge>(sp => new JournalBridge(sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddSingleton<AppState>();

await builder.Build().RunAsync();
