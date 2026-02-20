using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsProviders.Providers.Common;

namespace NewsProviders.Providers.UnitedNations;

public class UNProviderModule : INewsProviderModule
{
    public string Key => "UnitedNations";
    public void RegisterNewsProvider(IServiceCollection services, IConfiguration config)
    {
        services.Configure<UnitedNationsOptions>(
            config.GetSection("UnitedNations"));

        services.AddHttpClient<UnitedNationsRssClient>();
        services.AddHttpClient<UnitedNationsNewsScraper>();
        services.AddTransient<INewsProvider, UnitedNationsNewsProvider>();
    }
}