using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Ti4Companion.Web;
using Ti4Companion.Web.Localization;
using Ti4Companion.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Same-origin API (the API hosts this WASM client).
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<Ti4ApiClient>();
builder.Services.AddSingleton<Loc>();
builder.Services.AddScoped<SessionStore>();

await builder.Build().RunAsync();
