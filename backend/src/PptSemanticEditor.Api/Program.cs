using Microsoft.AspNetCore.RateLimiting;
using PptSemanticEditor.Agent;
using PptSemanticEditor.Api.Services;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Parser;
using PptSemanticEditor.Renderer;
using PptSemanticEditor.Semantic;

var builder = WebApplication.CreateBuilder(args);

// Fix 3 — Bind to the PORT env var supplied by Render (and other PaaS hosts)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Configure JSON serialization for controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Fix 2 — CORS: read additional origins from config; keep dev origins as a baseline.
// In production, override via env vars AllowedOrigins__0, AllowedOrigins__1, etc.
var configuredOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var devOrigins = new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost:4173" };
var allowedOrigins = devOrigins.Union(configuredOrigins).ToArray();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Groq settings
builder.Services.Configure<GroqSettings>(builder.Configuration.GetSection("Groq"));

// Register singleton services
builder.Services.AddSingleton<SessionStore>();

// Register scoped/transient services
builder.Services.AddScoped<IOpenXmlParser, OpenXmlParser>();
builder.Services.AddScoped<ISlideClassifier, SlideClassifier>();
builder.Services.AddScoped<IGraphDetector, GraphDetector>();
builder.Services.AddScoped<ISemanticTreeBuilder, SemanticTreeBuilder>();
builder.Services.AddScoped<IPipelineStateManager, PipelineStateManager>();
builder.Services.AddScoped<IOpenXmlRenderer, PptSemanticEditor.Renderer.OpenXmlRenderer>();
builder.Services.AddScoped<JsonGenerator>();
builder.Services.AddScoped<AnalysisAgent>();
builder.Services.AddScoped<EditPlanAgent>();

builder.Services.AddScoped<IAstBuilder, AstBuilder>();
builder.Services.AddScoped<IGraphBuilder, GraphBuilder>();
builder.Services.AddScoped<IGeometryAnalyzer, GeometryAnalyzer>();
builder.Services.AddScoped<IArchitectureDiagramDetector, ArchitectureDiagramDetector>();
builder.Services.AddScoped<IActionExecutor, ActionExecutor>();
builder.Services.AddScoped<IValidationEngine, ValidationEngine>();

// Register LLM service with HttpClient
builder.Services.AddHttpClient<ILlmService, GroqLlmService>();
builder.Services.AddScoped<IEditApplier, EditApplier>();

// Fix 1 — Single source of truth for the uploads/storage path.
// Override at deploy time via STORAGE_PATH env var or Storage:Path in config.
var storagePath = builder.Configuration["Storage:Path"]
    ?? Environment.GetEnvironmentVariable("STORAGE_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");
Directory.CreateDirectory(storagePath);
builder.Services.AddSingleton(new StorageSettings { Path = storagePath });

// Fix 4 — Per-IP rate limiting for Groq-calling endpoints (15 req/min).
// No new NuGet package needed — Microsoft.AspNetCore.RateLimiting ships with .NET 8.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("agent", opt =>
    {
        opt.PermitLimit = 15;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

// Fix 4 — UseRateLimiter must come before UseCors so the limiter fires first
app.UseRateLimiter();

app.UseCors();

app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Fix 5 — Serve the compiled React SPA from wwwroot (populated by the Dockerfile).
// MapFallbackToFile only activates for paths that don't match any controller route,
// so /api/* requests are never affected.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
