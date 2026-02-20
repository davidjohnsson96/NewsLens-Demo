using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsProviders.Providers.Common;
using NewsProviders.Providers.SVTNews.SVTNewsInterfaces;

namespace NewsProviders.Providers.SVTNews;

public class SVTProviderModule : INewsProviderModule
{
    public string Key => "SVT";
    public void RegisterNewsProvider(IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<ISvtrssFeed, SvtrssFeed>();
        services.AddTransient<ISvtNewsScraper, SvtNewsScraper>();
        services.AddTransient<INewsProvider, SvtNewsProvider>();
    }
}