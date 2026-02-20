using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsProviders.Providers.Common;

namespace NewsProviders.Providers.Regeringen;

public class RegeringenProviderModule : INewsProviderModule
{
    public string Key => "Regeringen";
    public void RegisterNewsProvider(IServiceCollection services, IConfiguration config)
    {
        services.Configure<RegeringenOptions>(
            config.GetSection("RegeringenOptions"));
        services.AddHttpClient<RegeringenRssClient>();
        services.AddHttpClient<RegeringenNewsScraper>();
        services.AddTransient<INewsProvider, RegeringenNewsProvider>();
    }
}