using System;
using System.IO;
using CodeWalker.GameFiles;
using CodeWalker.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using CodeWalker.API.Models;

// ✅ Create the builder
var builder = WebApplication.CreateBuilder(args);

// ✅ Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// ✅ Bind ApiConfig from configuration
builder.Services.Configure<ApiConfig>(builder.Configuration);
builder.Services.AddSingleton(serviceProvider =>
{
    return serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiConfig>>().Value;
});

// ✅ Load config directly from disk (overrides appsettings if saved)
var configService = new ConfigService();
var config = configService.Get();
string gtaPath = config.GTAPath;
int port = config.Port;

// ✅ Logging setup
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ✅ Ensure GTA V directory exists
if (!Directory.Exists(gtaPath))
{
    Console.Error.WriteLine($"[ERROR] GTA V directory not found at {gtaPath}");
    return;
}

// ✅ Load RPF decryption keys
try
{
    Console.WriteLine("[INFO] Loading RPF decryption keys...");
    GTA5Keys.LoadFromPath(gtaPath);
    Console.WriteLine("[INFO] RPF decryption keys loaded successfully.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] Failed to load RPF keys: {ex.Message}");
    return;
}

// ✅ Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CodeWalker API", Version = "v1" });
    c.EnableAnnotations();
});
builder.Services.AddControllers();

// ✅ Register GameFileCache
builder.Services.AddSingleton<GameFileCache>(serviceProvider =>
{
    long cacheSize = 2L * 1024 * 1024 * 1024; // 2GB Cache
    double cacheTime = 60.0;
    bool isGen9 = false;
    string dlc = "";
    bool enableMods = false;
    string excludeFolders = "";

    var gameFileCache = new GameFileCache(cacheSize, cacheTime, gtaPath, isGen9, dlc, enableMods, excludeFolders);

    gameFileCache.EnableDlc = true; // this ensures Init() runs InitDlc()

    gameFileCache.Init(
        message => Console.WriteLine($"[GameFileCache] {message}"),
        error => Console.Error.WriteLine($"[GameFileCache ERROR] {error}")
    );

    Console.WriteLine("[Startup] Archetypes loaded: " + gameFileCache.YtypDict?.Count);

    return gameFileCache;
});

builder.Services.AddSingleton<ConfigService>();

// ✅ Register RpfService with logging support
builder.Services.AddSingleton<RpfService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<RpfService>>();
    var configService = serviceProvider.GetRequiredService<ConfigService>();
    return new RpfService(logger, configService);
});


// ✅ Build the app
var app = builder.Build();

// ✅ Bind the server to the configured port
app.Urls.Add($"http://0.0.0.0:{port}");

// ✅ Logging API startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation($"API is starting on port {port}...");

// ✅ Swagger setup
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeWalker API v1");
    c.RoutePrefix = ""; // Swagger available at root
});

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// ✅ (Cache) preheating
var rpfService = app.Services.GetRequiredService<RpfService>();
int count = rpfService.Preheat();

try
{
    var gameFileCache = app.Services.GetRequiredService<GameFileCache>();
    Console.WriteLine("[Startup] Preloading cache with known meta types...");

    // Preload by hash
    uint hash = JenkHash.GenHash("prop_alien_egg_01");
    var ydr = gameFileCache.GetYdr(hash);
    if (ydr != null)
        Console.WriteLine("[Startup] YDR preloaded successfully.");
    else
        Console.WriteLine("[Startup] YDR not found in archive.");

    Console.WriteLine("[Startup] Archetype dict contains: " + gameFileCache.GetArchetype(hash)?.Name);
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup ERROR] Cache preloading failed: {ex.Message}");
}

logger.LogInformation($"[Startup] Preheated RPF with {count} entries.");

// ✅ Run the app
app.Run();
