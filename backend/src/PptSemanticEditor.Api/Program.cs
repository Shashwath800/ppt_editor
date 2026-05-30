using PptSemanticEditor.Agent;
using PptSemanticEditor.Api.Services;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Parser;
using PptSemanticEditor.Renderer;
using PptSemanticEditor.Semantic;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization for controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// CORS — allow frontend dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:4173")
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

// Ensure uploads directory exists
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "uploads");
Directory.CreateDirectory(uploadsPath);

var app = builder.Build();

app.UseCors();

app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
