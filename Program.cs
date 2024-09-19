using LLama.Native;
using LLMRP.Components;
using LLMRP.Components.Models;
using LLMRP.Components.Models.Misc;
using LLMRP.Components.Models.Services;
using LLMRP.Components.Models.Services.URLHandle;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.VisibleStateDuration = 1000;

});
builder.Services.AddMudMarkdownServices();
builder.Services.AddMudExtensions();
var d = builder.Configuration.AddJsonFile(Environment.CurrentDirectory + "/wwwroot/config/User.json");
builder.Services.AddLocalization();
builder.Services.AddScoped<ChatState>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<ScreenSize>();
builder.Services.AddScoped<AlertServices>();
builder.Services.AddScoped<TranslatorService>();
builder.Services.AddScoped<URLImporterService>();
builder.Services.AddSingleton<ProviderService>();
builder.Services.AddSingleton<DiagnosticsService>();
builder.Services.AddSingleton<UploaderService>();
builder.Services.AddSingleton<AdvancedQueryService>();
string[] supportedCul = ["en-US", "ru-RU"];
var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCul[0])
    .AddSupportedCultures(supportedCul).AddSupportedUICultures(supportedCul);

builder.Services.AddMvc();
var app = builder.Build();


app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();



AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    using (var scope = app.Services.CreateScope())
    {
        var settings = scope.ServiceProvider.GetRequiredService<UploaderService>();
        settings.SavePresets();
    }

};


app.Run();



