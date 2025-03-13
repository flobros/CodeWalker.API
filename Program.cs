using CodeWalker.GameFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

string gtaPath = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V";

// ✅ Ensure GTA folder exists
if (!Directory.Exists(gtaPath))
{
    Console.Error.WriteLine($"Error: GTA V directory not found at {gtaPath}");
    return;
}

// ✅ Load RPF decryption keys BEFORE registering services
try
{
    Console.WriteLine("Loading RPF decryption keys...");
    GTA5Keys.LoadFromPath(gtaPath);  // ✅ This must happen first
    Console.WriteLine("Keys loaded successfully.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error loading keys: {ex.Message}");
    return;
}

// ✅ Register services AFTER keys are loaded
builder.Services.AddControllers();
builder.Services.AddSingleton(new RpfService(gtaPath));  // ✅ Now safe to initialize
builder.Services.AddSingleton<TextureService>();  // ✅ No longer depends on GameFileCache

var app = builder.Build();

// ✅ Use top-level route registrations
app.MapGet("/", () => "API is running.");
app.MapControllers();

// Cleanup on shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Cleaning up resources...");
});

app.Run();
