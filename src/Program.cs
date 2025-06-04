using Figgle;
using MousyHub;
using MousyHub.Components;
using MudBlazor;
using MudBlazor.Services;
using MousyHub.Models;
using MousyHub.Models.Misc;
using MousyHub.Models.Services;
using MousyHub.Models.Services.URLHandle;
using MudExtensions.Services;
using Microsoft.KernelMemory;
using MousyHub.Classes.Services.TTS;
using LLama.Native;
using System.Diagnostics;
using System.Reflection;
using MousyHub.Classes.Services.TelegramExtra;




var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.VisibleStateDuration = 1000;

});

builder.Services.AddSpeechRecognitionServices();

string baseDir = AppDomain.CurrentDomain.BaseDirectory;
var config = LLama.Native.NativeLibraryConfig.All
    .WithSearchDirectories(new[] { baseDir });
builder.Services.AddMudExtensions();
var configuration = builder.Configuration;
builder.Services.AddMudMarkdownServices();
builder.Services.AddLocalization();
builder.Services.AddScoped<ChatState>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<ScreenSize>();
builder.Services.AddScoped<AlertServices>();
builder.Services.AddScoped<TranslatorService>();
builder.Services.AddScoped<URLImporterService>();
builder.Services.AddScoped<STTService>();
builder.Services.AddScoped<AudioService>();
builder.Services.AddScoped<TTSManagerService>();
builder.Services.AddSingleton<HFDownloaderService>();
builder.Services.AddSingleton<ProviderService>();
builder.Services.AddSingleton<UpdaterService>();
builder.Services.AddSingleton<DiagnosticsService>();
builder.Services.AddSingleton<UploaderService>();
builder.Services.AddSingleton<RAGService>();
builder.Services.AddSingleton<AdvancedQueryService>();
builder.Services.AddSingleton<KokoroService>();
builder.Services.AddSingleton<TelegramService>();
string[] supportedCul = ["en-US", "ru-RU"];
var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCul[0])
    .AddSupportedCultures(supportedCul).AddSupportedUICultures(supportedCul);
AppVersion.SetVersion(configuration["Version"]); 
builder.Services.AddMvc();
var app = builder.Build();
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    Util.OpenBrowser("http://localhost:5000");
}


app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


Console.WriteLine(FiggleFonts.Larry3d.Render("MousyHub"));
AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var settings = scope.ServiceProvider.GetRequiredService<UploaderService>();
            settings.SavePresets();
        }
    }
    catch (Exception ex )
    {
        Console.WriteLine($"Ошибка при сохранении настроек");
    }


};


app.Run();



