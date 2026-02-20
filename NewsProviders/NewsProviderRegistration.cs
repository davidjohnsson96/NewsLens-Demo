using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsProviders.Providers.Common;
using NewsProviders.Providers.EuropeanUnion.EuCommission;
using NewsProviders.Providers.EuropeanUnion.EUParliament;
using NewsProviders.Providers.EuropeanUnion.EuropeanCounsil;
using NewsProviders.Providers.Regeringen;
using NewsProviders.Providers.SVTNews;
using NewsProviders.Providers.UnitedNations;
using NewsProviders.Providers.WorldNewsApi.Configuration;

namespace NewsProviders;

public static class NewsProviderRegistration
{
    public static IServiceCollection AddNewsProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var modules = new INewsProviderModule[]
        {
            new RegeringenProviderModule(),
            new SVTProviderModule(),
            new UNProviderModule(),
            new WorldNewsApiProviderModule(),
            new ConsiliumProviderModule(),
            new EuCommisionProviderModule(),
            new EuParliamentProviderModule(),
        };

        var moduleMap = modules.ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);
        
        var enabled = configuration
            .GetSection("NewsProviders:Enabled")
            .Get<string[]>() ?? Array.Empty<string>();
        
        foreach (var key in enabled)
        {
            if (!moduleMap.TryGetValue(key, out var module))
            {
                throw new InvalidOperationException(
                    $"Unknown NewsProvider '{key}'. Available: {string.Join(", ", moduleMap.Keys)}");
            }

            module.RegisterNewsProvider(services, configuration);
        }

        return services;
    }
}