using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OCAP.Dashboard;
using OCAP.Dashboard.Authentication;
using OCAP.Dashboard.Services;
using OCAP.Dashboard.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Registra HttpClient configurado hacia la API Gateway
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Registra servicios de API, contenedor de estado y proveedor de autenticación
builder.Services.AddScoped<IDashboardApiService, DashboardApiService>();
builder.Services.AddSingleton<DashboardStateContainer>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();
