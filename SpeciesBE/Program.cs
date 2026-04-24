using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpeciesBE;
using SpeciesBE.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient());

// Singleton pour que les services partagent le même état dans toute l'app
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<FavoriteService>();
builder.Services.AddScoped<SpeciesApiService>();

var host = builder.Build();

// Initialise AuthService pour restaurer la session depuis localStorage
var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

await host.RunAsync();
