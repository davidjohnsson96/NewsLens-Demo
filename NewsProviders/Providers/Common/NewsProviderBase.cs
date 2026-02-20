namespace NewsProviders.Providers.Common;

public abstract class NewsProviderBase : INewsProvider
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(5);

    protected NewsProviderBase(string providerName, TimeSpan? runInterval = null)
    {
        ProviderName = providerName;

        RunInterval = runInterval ?? DefaultInterval;

        LastRunUtc = null;
        NextRunUtc = DateTimeOffset.UtcNow;
    }

    public string ProviderName { get; }

    public DateTimeOffset? LastRunUtc { get; private set; }
    public TimeSpan RunInterval { get; }
    public DateTimeOffset NextRunUtc { get; private set; }
    
    public delegate Task<bool> DeduplicationCheck(
        string providerName,
        string url,
        CancellationToken ct);


    public abstract Task<IReadOnlyList<NewsItem>> GetNewsAsync(Func<string, string, CancellationToken, Task<bool>>? dedupCheck = null, CancellationToken ct = default);

    public void UpdateScheduleAfterRun(DateTimeOffset runFinishedAtUtc)
    {
        LastRunUtc = runFinishedAtUtc;
        NextRunUtc = runFinishedAtUtc + RunInterval;
    }

    public bool IsDueToRun(DateTimeOffset nowUtc)
        => nowUtc >= NextRunUtc;
    
    protected Task<bool> DefaultDedup(string provider, string url, CancellationToken ct)
        => Task.FromResult(true);
}