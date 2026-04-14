using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpeciesBE;
using SpeciesBE.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient());

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FavoriteService>();
builder.Services.AddScoped<SpeciesApiService>();

await builder.Build().RunAsync();
