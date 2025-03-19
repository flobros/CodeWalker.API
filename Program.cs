using System;
using System.IO;
using CodeWalker.GameFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
// ✅ Add Logging Service
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // Ensures logs appear in the console
builder.Logging.AddDebug();   // Allows logs to show in the Debug window (if using Visual Studio)


string gtaPath = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V"; // 🔹 Change this if needed

// ✅ Ensure GTA V directory exists
if (!Directory.Exists(gtaPath))
{
    Console.Error.WriteLine($"[ERROR] GTA V directory not found at {gtaPath}");
    return;
}

// ✅ Load RPF decryption keys BEFORE initializing services
try
{
    Console.WriteLine("[INFO] Loading RPF decryption keys...");
    GTA5Keys.LoadFromPath(gtaPath);  // ✅ Must be done before RPF operations
    Console.WriteLine("[INFO] RPF decryption keys loaded successfully.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] Failed to load RPF keys: {ex.Message}");
    return;
}

// ✅ Register services AFTER keys are loaded
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();  // ✅ Ensures controllers are registered
builder.Services.AddSingleton(new RpfService(gtaPath));
builder.Services.AddSingleton<GameFileCache>(serviceProvider =>
{
    long cacheSize = 2L * 1024 * 1024 * 1024; // 2GB Cache
    double cacheTime = 60.0; // 60 seconds
    string gtaFolderPath = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V";
    bool isGen9 = false; // Change if needed
    string dlc = ""; // No specific DLC
    bool enableMods = false;
    string excludeFolders = "";

    var gameFileCache = new GameFileCache(cacheSize, cacheTime, gtaFolderPath, isGen9, dlc, enableMods, excludeFolders);

    void UpdateStatus(string message) => Console.WriteLine($"[GameFileCache] {message}");
    void ErrorLog(string message) => Console.Error.WriteLine($"[GameFileCache ERROR] {message}");

    gameFileCache.Init(UpdateStatus, ErrorLog);
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
    c.RoutePrefix = ""; // Swagger will be available at http://localhost:5024
});

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
app.MapControllers();

// ✅ Cleanup on shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Cleaning up resources...");
});



app.Run();
