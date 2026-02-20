using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsProviders.Providers.Common;

namespace NewsProviders.Providers.EuropeanUnion.EuropeanCounsil;

public class ConsiliumProviderModule : INewsProviderModule
{
    public string Key => "Consilium";
    public void RegisterNewsProvider(IServiceCollection services, IConfiguration config)
    {
        services.Configure<ConsiliumOptions>(config.GetSection("EuConsilium"));
        services.AddHttpClient<ConsiliumRssClient>();
        services.AddHttpClient<ConsiliumNewsScraper>();
        services.AddTransient<INewsProvider, ConsiliumNewsProvider>();
    }
}