using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReleaseDeck.Portal;
using ReleaseDeck.Portal.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress).GetLeftPart(UriPartial.Authority)) });
builder.Services.AddScoped<ReleaseApi>();
builder.Services.AddScoped<ReleaseStore>();
await builder.Build().RunAsync();
