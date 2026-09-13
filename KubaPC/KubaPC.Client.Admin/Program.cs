using Autofac;
using Autofac.Extensions.DependencyInjection;
using Blazored.LocalStorage;
using KubaPC.Client.Admin;
using KubaPC.SharedClient.Components;
using KubaPC.SharedClient.Services.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Autofac replaces the built-in container, matching how the API is wired.
builder.ConfigureContainer(new AutofacServiceProviderFactory(RegisterServices));


string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in wwwroot/appsettings.json.");
}

builder.Services.AddScoped(serviceProvider => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddMudServices();

// The two strings that differ between the apps on shared pages.
builder.Services.AddSingleton(new AppInfo("KubaPC Admin", "Payment Certificate administration"));

builder.Services.AddScoped<AuthenticationStateProvider, KubaPcAuthenticationStateProvider>();

await builder.Build().RunAsync();

static void RegisterServices(ContainerBuilder containerBuilder)
{
    containerBuilder.RegisterModule(new KubaPC.SharedClient.Services.Factories.Services());
}
