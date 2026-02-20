using FactRepository.Classes;
using Linkage;
using LLMIntegration.Clients;
using LLMIntegration.Configuration;
using LLMIntegration.Utilities;
using NewsLensAutomationService.Orchestrators;
using NewsLensAutomationService.Workflows;
using NewsProviders;
using Serilog;
using Serilog.Events;
using NewsLens.Common.Helpers;
using NewsProviders.Providers.Regeringen;

var builder = WebApplication.CreateBuilder(args);

// See what the host thinks
Console.WriteLine($"Host env: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Base dir: {AppContext.BaseDirectory}");

builder.Configuration.AddProjectConfiguration(builder.Environment);
builder.Logging.AddSharedSerilogLogging(builder.Configuration, builder.Environment); //NewsLens.Common -> LoggingExtensions

//Register providers
builder.Services.AddNewsProviders(builder.Configuration);

//  FactRepository

builder.Services.Configure<FactHandlerOptions>(
    builder.Configuration.GetSection("FactHandlerOptions"));

builder.Services.AddTransient<FactService>();

// FactHarvester
builder.Services.Configure<LocalFactExtractionOptions>(
    builder.Configuration.GetSection("LocalFactExtractor"));

builder.Services.Configure<ThreadLinkerOptions>(
    builder.Configuration.GetSection("ThreadLinkerOptions"));

builder.Services.AddTransient<LocalFactExtractionLlmClient>();


builder.Services.AddTransient<ArticleFactoryLlmClient>();
builder.Services.Configure<ArticleFactoryOptions>(
    builder.Configuration.GetSection("LlmOptions"));

//Factory 
builder.Services.Configure<FactExtractionOptions>(
    builder.Configuration.GetSection("LlmOptions"));

builder.Services.Configure<SystemInstructionsOptions>(
    builder.Configuration.GetSection("SystemInstructions"));

builder.Services.Configure<ArticleFactoryOptions>(
    builder.Configuration.GetSection("FactoryOptions"));  // Skapa factoryoptions

builder.Services.Configure<OrchestratorOptions>(
    builder.Configuration.GetSection("OrchestratorOptions"));



// Orchestrator + workflows
builder.Services.AddTransient<IThreadLinker, JaccardLinker>();
builder.Services.AddSingleton<FactHarvestWorkflow>();
builder.Services.AddSingleton<ArticleCreationWorkflow>();
builder.Services.AddSingleton<ThreadLinkerWorkflow>();
builder.Services.AddSingleton<WorkflowOrchestrator>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WorkflowOrchestrator>());

//DeduplicationService
builder.Services.AddTransient<ArticleDeduplicationService>();

builder.Services.AddLogging(lb =>
{
    lb.ClearProviders();      // important, removes default console logger
    lb.AddSerilog(dispose: true);
});

// Add controllers (this will no longer be red)
builder.Services.AddControllers();

try
{
    var app = builder.Build();
    
    // 👇 log every incoming request so we can SEE what's hitting this app
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"[{DateTimeOffset.Now}] {context.Request.Method} {context.Request.Path}");
        await next();
    });
    
    app.MapControllers();  // <-- enable your REST API
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}