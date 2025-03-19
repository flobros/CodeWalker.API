using System;
using System.IO;
using CodeWalker.GameFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

string gtaPath = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V";
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

// ✅ Register Swagger with Query Parameter Examples
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CodeWalker API", Version = "v1" });

    // ✅ Enable Query Parameter Descriptions
    c.EnableAnnotations();
});

builder.Services.AddControllers();
builder.Services.AddSingleton(new RpfService(gtaPath));
builder.Services.AddSingleton<GameFileCache>(serviceProvider =>
{
    long cacheSize = 2L * 1024 * 1024 * 1024; // 2GB Cache
    double cacheTime = 60.0;
    string gtaFolderPath = gtaPath;
    bool isGen9 = false;
    string dlc = "";
    bool enableMods = false;
    string excludeFolders = "";

    var gameFileCache = new GameFileCache(cacheSize, cacheTime, gtaFolderPath, isGen9, dlc, enableMods, excludeFolders);
    gameFileCache.Init(
        message => Console.WriteLine($"[GameFileCache] {message}"),
        error => Console.Error.WriteLine($"[GameFileCache ERROR] {error}")
    );

    return gameFileCache;
});

var app = builder.Build();

// ✅ Use Logging in Application Lifecycle
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("API is starting...");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeWalker API v1");
    c.RoutePrefix = ""; // Swagger available at http://localhost:5024
});

app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();
