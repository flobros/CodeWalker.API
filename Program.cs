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

// ✅ Create the builder
var builder = WebApplication.CreateBuilder(args);

// ✅ Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
string gtaPath = builder.Configuration.GetValue<string>("GTAPath") ?? "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V";
int port = builder.Configuration.GetValue<int>("Port", 5024); // Default to 5024

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
    gameFileCache.Init(
        message => Console.WriteLine($"[GameFileCache] {message}"),
        error => Console.Error.WriteLine($"[GameFileCache ERROR] {error}")
    );
    return gameFileCache;
});

builder.Services.AddSingleton<ConfigService>();

// ✅ Register RpfService with logging support
builder.Services.AddSingleton<RpfService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<RpfService>>();
    return new RpfService(gtaPath, logger);
});

// ✅ Build the app
var app = builder.Build();

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
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup ERROR] Cache preloading failed: {ex.Message}");
}


logger.LogInformation($"[Startup] Preheated RPF with {count} entries.");

// ✅ Run the app
app.Run($"http://0.0.0.0:{port}");
