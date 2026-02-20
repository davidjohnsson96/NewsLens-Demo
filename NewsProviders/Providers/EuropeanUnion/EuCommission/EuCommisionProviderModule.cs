using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsProviders.Providers.Common;

namespace NewsProviders.Providers.EuropeanUnion.EuCommission;

public class EuCommisionProviderModule : INewsProviderModule
{
    public string Key => "EuCommission";
    public void RegisterNewsProvider(IServiceCollection services, IConfiguration config)
    {
        services.Configure<EuCommissionOptions>(
            config.GetSection("EuCommissionOptions"));

        services.AddHttpClient<EuCommissionRSSClient>();
        services.AddHttpClient<EuCommissionNewsScraper>();
        services.AddTransient<INewsProvider, EuCommissionNewsProvider>();
    }
}