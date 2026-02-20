using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NewsProviders.Providers.Common;
public interface INewsProvider
{
    string ProviderName { get; }

    DateTimeOffset? LastRunUtc { get; }
    TimeSpan RunInterval { get; }
    DateTimeOffset NextRunUtc { get; }

    Task<IReadOnlyList<NewsItem>> GetNewsAsync(Func<string, string, CancellationToken, Task<bool>>? dedupCheck = null, CancellationToken ct = default);

    void UpdateScheduleAfterRun(DateTimeOffset runFinishedAtUtc);

    public bool IsDueToRun(DateTimeOffset nowUtc);
    
}

public interface INewsProviderModule
{
    string Key { get; }
    void RegisterNewsProvider(IServiceCollection services, IConfiguration config);
}