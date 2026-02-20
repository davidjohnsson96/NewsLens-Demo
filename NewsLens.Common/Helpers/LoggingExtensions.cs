using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace NewsLens.Common.Helpers;
public static class LoggingExtensions
{
    
    public static ILoggingBuilder AddSharedSerilogLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        var baseLogRoot = ResolveLogRoot(configuration, env);
        var providerLogRoot = Path.Combine(baseLogRoot, "ServiceLogs");

        Directory.CreateDirectory(providerLogRoot);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("environment", env.EnvironmentName)
            .WriteTo.Console()
            .WriteTo.Map(
                keyPropertyName: "serviceName",
                defaultKey: "UnknownService",
                configure: (serviceName, wt) =>
                    wt.File(
                        path: Path.Combine(providerLogRoot, $"{serviceName}-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        buffered: false))
            .CreateLogger();

        logging.ClearProviders();
        logging.AddSerilog(Log.Logger, dispose: true);

        return logging;
    }

    private static string ResolveLogRoot(IConfiguration config, IHostEnvironment env)
    {
        var configured = config["Logging:LogRoot"] ?? "logs";

        if (Path.IsPathRooted(configured))
            return configured;
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configured));
    }

}