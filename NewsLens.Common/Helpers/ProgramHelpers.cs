using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace NewsLens.Common.Helpers;

public static class ProgramHelpers
{

    public static IConfigurationBuilder AddSharedConfiguration(
        this IConfigurationBuilder config,
        IHostEnvironment env)
    {
        
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        
        var sharedDir = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "shared"));
        var sharedFolderBase = Path.Combine(sharedDir, "appsettings.json");
        var sharedFolderEnv = Path.Combine(sharedDir, $"appsettings.{env.EnvironmentName}.json");
        
        var localSharedFile = Path.Combine(env.ContentRootPath, "appsettings.shared.json");
        var localEnvOverride = Path.Combine(env.ContentRootPath, $"appsettings.{env.EnvironmentName}.json");

        var sharedFolderExists =
            File.Exists(sharedFolderBase) || File.Exists(sharedFolderEnv);

        var localSharedFileExists = File.Exists(localSharedFile);

        if (sharedFolderExists)
        {
            config.AddJsonFile(sharedFolderBase, optional: !File.Exists(sharedFolderBase), reloadOnChange: true)
                .AddJsonFile(sharedFolderEnv, optional: true, reloadOnChange: true);
        }
        else if (localSharedFileExists)
        {
            config.AddJsonFile(localSharedFile, optional: true, reloadOnChange: true)
                .AddJsonFile(localEnvOverride, optional: true, reloadOnChange: true);
        }
        else
        {
            throw new FileNotFoundException(
                "No shared configuration layout found.\n" +
                $"Looked for shared-folder files:\n  - {sharedFolderBase}\n  - {sharedFolderEnv}\n" +
                $"And shared-file:\n  - {localSharedFile}\n" +
                $"ContentRootPath: {env.ContentRootPath}");
        }
        config.AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables();

        return config;
    }
    
    public static IConfigurationBuilder AddProjectConfiguration(
        this IConfigurationBuilder config,
        IHostEnvironment env)
    {
        config
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables();

        return config;
    }


}