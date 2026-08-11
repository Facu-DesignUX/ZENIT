using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ZENIT.Blazor;

using ZENIT.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Servicios Base
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(10)
});

// Servicios de ZENIT
builder.Services.AddScoped<RadioApiService>();
builder.Services.AddScoped<PlayerStateService>();
builder.Services.AddScoped<FavoritesService>();

await builder.Build().RunAsync();
