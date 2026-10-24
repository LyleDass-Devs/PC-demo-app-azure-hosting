using Autofac;
using Autofac.Extensions.DependencyInjection;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using KubaPC.Client.Operator;
using KubaPC.SharedClient.Components;
using KubaPC.SharedClient.Services.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.ConfigureContainer(new AutofacServiceProviderFactory(RegisterServices));

string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in wwwroot/appsettings.json.");
}

builder.Services.AddScoped(serviceProvider => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddMudServices();
builder.Services.AddScoped<KubaPC.SharedClient.Components.Layout.PageHeader>();

builder.Services.AddSingleton(new AppInfo(KubaApp.Operator, "KubaPC", "Payment Certificate"));

builder.Services.AddScoped<AuthenticationStateProvider, KubaPcAuthenticationStateProvider>();

await builder.Build().RunAsync();

static void RegisterServices(ContainerBuilder containerBuilder)
{
    containerBuilder.RegisterModule(new KubaPC.SharedClient.Services.Factories.Services());
}
