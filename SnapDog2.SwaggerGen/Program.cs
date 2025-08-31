using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

// Parse command line arguments
var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "swagger.json");
var verbose = false;
var showHelp = false;

for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];
    switch (arg.ToLowerInvariant())
    {
        case "--verbose":
        case "-v":
            verbose = true;
            break;
        case "--help":
        case "-h":
        case "/?":
            showHelp = true;
            break;
        case "--output":
        case "-o":
            if (i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
            else
            {
                Console.WriteLine("Error: --output requires a filename");
                Environment.Exit(1);
            }
            break;
        default:
            if (!arg.StartsWith("-"))
            {
                // First non-flag argument is the output path
                outputPath = arg;
            }
            else
            {
                Console.WriteLine($"Unknown option: {arg}");
                showHelp = true;
            }
            break;
    }
}

if (showHelp)
{
    Console.WriteLine("SnapDog OpenAPI Generator");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run [options] [output-file]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  output-file                 Output file path (default: swagger.json)");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -o, --output <file>         Output file path");
    Console.WriteLine("  -v, --verbose               Enable verbose output");
    Console.WriteLine("  -h, --help                  Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run                           # Generate swagger.json");
    Console.WriteLine("  dotnet run api-spec.json             # Generate api-spec.json");
    Console.WriteLine("  dotnet run -o docs/openapi.json      # Generate docs/openapi.json");
    Console.WriteLine("  dotnet run --verbose                 # Generate with verbose output");
    Environment.Exit(0);
}

// Console colors for npm-style output
const string CYAN = "\u001b[36m";
const string GREEN = "\u001b[32m";
const string RED = "\u001b[31m";
const string YELLOW = "\u001b[33m";
const string RESET = "\u001b[0m";
const string DIM = "\u001b[2m";

var stopwatch = Stopwatch.StartNew();

try
{
    Console.WriteLine($"{CYAN}SnapDog swagger-gen{RESET} {DIM}Generating OpenAPI specification{RESET}");
    Console.WriteLine();

    // Validate and resolve output path
    var fullOutputPath = Path.GetFullPath(outputPath);
    var outputDir = Path.GetDirectoryName(fullOutputPath);

    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
    {
        if (verbose)
        {
            Console.WriteLine($"{DIM}→ Creating directory: {outputDir}{RESET}");
        }

        Directory.CreateDirectory(outputDir);
    }

    if (verbose)
    {
        Console.WriteLine($"{DIM}→ Output path: {fullOutputPath}{RESET}");
    }

    // Locate SnapDog2 assembly with fallback paths
    var assemblyPaths = new[]
    {
        "../SnapDog2/bin/Debug/net9.0/SnapDog2.dll",
        "../SnapDog2/bin/Release/net9.0/SnapDog2.dll",
        "./SnapDog2.dll",
        Path.Combine(AppContext.BaseDirectory, "SnapDog2.dll")
    };

    string assemblyPath = null;
    foreach (var path in assemblyPaths)
    {
        var resolvedPath = Path.GetFullPath(path);
        if (File.Exists(resolvedPath))
        {
            assemblyPath = resolvedPath;
            break;
        }
    }

    if (assemblyPath == null)
    {
        throw new FileNotFoundException(
            "SnapDog2.dll not found. Please ensure the project is built.\n" +
            $"Searched paths:\n{string.Join("\n", assemblyPaths.Select(p => $"  - {Path.GetFullPath(p)}"))}");
    }

    if (verbose)
    {
        Console.WriteLine($"{DIM}→ Loading assembly: {assemblyPath}{RESET}");
    }

    var builder = WebApplication.CreateBuilder();

    // Configure minimal logging to reduce noise
    builder.Logging.ClearProviders();
    if (verbose)
    {
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
    }

    // Load and validate SnapDog2 assembly
    Assembly snapDogAssembly;
    try
    {
        snapDogAssembly = Assembly.LoadFrom(assemblyPath);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to load SnapDog2 assembly from '{assemblyPath}': {ex.Message}", ex);
    }

    if (verbose)
    {
        Console.WriteLine($"{DIM}→ Assembly loaded: {snapDogAssembly.GetName().Name} v{snapDogAssembly.GetName().Version}{RESET}");
    }

    // Configure services
    builder.Services.AddControllers()
        .AddApplicationPart(snapDogAssembly);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SnapDog2 API",
            Version = "v1"
        });
    });

    if (verbose)
    {
        Console.WriteLine($"{DIM}→ Building application...{RESET}");
    }

    var app = builder.Build();

    if (verbose)
    {
        Console.WriteLine($"{DIM}→ Generating OpenAPI specification...{RESET}");
    }

    // Generate swagger document with error handling
    OpenApiDocument swagger;
    try
    {
        var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
        swagger = swaggerProvider.GetSwagger("v1");

        if (swagger == null)
        {
            throw new InvalidOperationException("Generated OpenAPI document is null");
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to generate OpenAPI specification: {ex.Message}", ex);
    }

    if (swagger.Paths?.Count == 0)
    {
        Console.WriteLine($"{YELLOW}⚠ Warning: No API endpoints found in the specification{RESET}");
    }

    if (verbose && swagger.Paths != null)
    {
        Console.WriteLine($"{DIM}→ Found {swagger.Paths.Count} API paths{RESET}");
    }

    // Write swagger.json with atomic operation
    var tempPath = fullOutputPath + ".tmp";
    try
    {
        await using var writer = File.CreateText(tempPath);
        swagger.SerializeAsV3(new OpenApiJsonWriter(writer));
        await writer.FlushAsync();
    }
    catch (Exception ex)
    {
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
        throw new IOException($"Failed to write OpenAPI specification to '{tempPath}': {ex.Message}", ex);
    }

    // Atomic move
    if (File.Exists(fullOutputPath))
    {
        File.Delete(fullOutputPath);
    }
    File.Move(tempPath, fullOutputPath);

    stopwatch.Stop();

    var fileInfo = new FileInfo(fullOutputPath);
    Console.WriteLine($"{GREEN}✓{RESET} Generated {Path.GetFileName(fullOutputPath)} ({FormatFileSize(fileInfo.Length)}) in {stopwatch.ElapsedMilliseconds}ms");

    if (verbose)
    {
        Console.WriteLine($"{DIM}  {fullOutputPath}{RESET}");
    }

    Environment.Exit(0);
}
catch (Exception ex)
{
    stopwatch.Stop();
    Console.WriteLine();
    Console.WriteLine($"{RED}✗{RESET} SnapDog swagger-gen failed after {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine();
    Console.WriteLine($"{RED}Error:{RESET} {ex.Message}");

    if (verbose && ex.InnerException != null)
    {
        Console.WriteLine($"{DIM}Inner exception: {ex.InnerException.Message}{RESET}");
    }

    if (verbose)
    {
        Console.WriteLine();
        Console.WriteLine($"{DIM}Stack trace:{RESET}");
        Console.WriteLine($"{DIM}{ex.StackTrace}{RESET}");
    }
    else
    {
        Console.WriteLine($"{DIM}Use --verbose for detailed error information{RESET}");
    }

    Environment.Exit(1);
}

static string FormatFileSize(long bytes)
{
    if (bytes < 1024)
    {
        return $"{bytes} B";
    }
    if (bytes < 1024 * 1024)
    {
        return $"{bytes / 1024.0:F1} KB";
    }
    return $"{bytes / (1024.0 * 1024.0):F1} MB";
}
