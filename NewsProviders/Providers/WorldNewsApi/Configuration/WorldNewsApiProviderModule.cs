using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsProviders.Providers.Common;

namespace NewsProviders.Providers.WorldNewsApi.Configuration;

public class WorldNewsApiProviderModule : INewsProviderModule
{
    public string Key => "WorldNewsApi";
    public void RegisterNewsProvider(IServiceCollection services, IConfiguration config)
    {
        services.Configure<WorldNewsApiOptions>(
            config.GetSection("WorldNewsApiOptions"));
        
        services.AddTransient<INewsProvider, WorldNewsApiProvider>();
    }
}