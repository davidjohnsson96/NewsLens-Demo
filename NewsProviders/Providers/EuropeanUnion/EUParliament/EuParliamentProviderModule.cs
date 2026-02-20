using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsProviders.Providers.Common;
using NewsProviders.Providers.EuropeanUnion.EuParliament;

namespace NewsProviders.Providers.EuropeanUnion.EUParliament;

public class EuParliamentProviderModule : INewsProviderModule
{
    public string Key  => "EuParliament";
    public void RegisterNewsProvider(IServiceCollection services, IConfiguration config)
    {
        services.Configure<EuParliamentOptions>(config.GetSection("EuParliament"));

        services.AddHttpClient<EuParliamentRSSClient>();
        services.AddHttpClient<EuParliamentNewsScraper>();

        services.AddTransient<EuParliamentNewsProvider>();
        services.AddTransient<INewsProvider, EuParliamentNewsProvider>();
    }
}