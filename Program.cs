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

// ✅ (No need to load appsettings.json manually!)

// ✅ Register ConfigService
builder.Services.AddSingleton<ConfigService>();

// ✅ Register ServiceFactory
builder.Services.AddSingleton<ServiceFactory>();

// ✅ Register ReloadableServiceContainer
builder.Services.AddSingleton<ReloadableServiceContainer>();

// ✅ Logging setup
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ✅ Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CodeWalker API", Version = "v1" });
    c.EnableAnnotations();
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Use PascalCase
    });

// ✅ Register RpfService as transient (will be managed by ReloadableServiceContainer)
builder.Services.AddTransient<RpfService>(serviceProvider =>
{
    var container = serviceProvider.GetRequiredService<ReloadableServiceContainer>();
    var logger = serviceProvider.GetRequiredService<ILogger<RpfService>>();
    return container.GetRpfService(logger);
});

// ✅ Register GameFileCache as transient (will be managed by ReloadableServiceContainer)
builder.Services.AddTransient<GameFileCache>(serviceProvider =>
{
    var container = serviceProvider.GetRequiredService<ReloadableServiceContainer>();
    return container.GetGameFileCache();
});

// ✅ Build the app
var app = builder.Build();

// ✅ Get config after app built
var configService = app.Services.GetRequiredService<ConfigService>();
var config = configService.Get();
string gtaPath = config.GTAPath;
int port = config.Port;

if (port == 0)
{
    Console.WriteLine("[WARN] Port is set to 0. Using default port 5555...");
    port = 5555;
}

// ✅ Check GTA V directory but don't exit if invalid
bool gtaPathValid = false;
if (!string.IsNullOrWhiteSpace(gtaPath))
{
    if (Directory.Exists(gtaPath))
    {
        gtaPathValid = true;
        Console.WriteLine($"[INFO] GTA V directory found at {gtaPath}");
    }
    else
    {
        Console.WriteLine($"[WARN] GTA V directory not found at {gtaPath}");
        Console.WriteLine("[INFO] API will start but services will not be initialized until a valid GTA path is configured.");
    }
}
else
{
    Console.WriteLine("[WARN] GTA path is not configured.");
    Console.WriteLine("[INFO] API will start but services will not be initialized until a valid GTA path is configured.");
}

// ✅ Try to load RPF decryption keys if GTA path is valid
if (gtaPathValid)
{
    try
    {
        Console.WriteLine("[INFO] Loading RPF decryption keys...");
        GTA5Keys.LoadFromPath(gtaPath);
        Console.WriteLine("[INFO] RPF decryption keys loaded successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] Failed to load RPF keys: {ex.Message}");
        Console.WriteLine("[INFO] API will start but services will not be initialized until a valid GTA path is configured.");
        gtaPathValid = false;
    }
}

// ✅ Bind the server to the configured port
app.Urls.Add($"http://0.0.0.0:{port}");

// ✅ Logging API startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation($"API is starting on port {port}...");

if (!gtaPathValid)
{
    logger.LogWarning("API is starting with invalid GTA path. Use /api/set-config to configure a valid GTA path.");
    Console.WriteLine("[INFO] API is ready. Use the config endpoints to set a valid GTA path.");
}

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

// ✅ Only preheat if GTA path is valid
if (gtaPathValid)
{
    try
    {
        var rpfService = app.Services.GetRequiredService<RpfService>();
        int count = rpfService.Preheat();
        
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
        
        logger.LogInformation($"[Startup] Preheated RPF with {count} entries.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup ERROR] Cache preloading failed: {ex.Message}");
        logger.LogError(ex, "[Startup] Failed to preheat services");
    }
}
else
{
    Console.WriteLine("[INFO] Skipping service preheating due to invalid GTA path.");
    Console.WriteLine("[INFO] Services will be initialized when a valid GTA path is configured.");
}

// ✅ Run the app
app.Run();
