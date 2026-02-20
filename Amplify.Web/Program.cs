using Amplify.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Auth state
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// HTTP clients
var apiBase = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");

builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<SignalApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<DashboardApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<AdvisoryApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddHttpClient<OverrideApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<AnalyticsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<BacktestApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<SettingsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<UserAdminApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddHttpClient<PatternScannerApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddHttpClient("WatchlistAPI", client =>
{
    client.BaseAddress = new Uri(apiBase);
});

builder.Services.AddScoped<WatchlistApiClient>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("WatchlistAPI");
    var auth = sp.GetRequiredService<AuthStateProvider>();
    return new WatchlistApiClient(http, auth);
});

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (Exception ex) { Console.WriteLine($"MIDDLEWARE ERROR: {ex}"); throw; }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();
app.MapRazorComponents<Amplify.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();