namespace NewsProviders.Providers.UnitedNations;

public sealed class UnitedNationsOptions
{
    public List<string> RssUrls { get; init; } = new();
    public int MaxItemsPerFeed { get; init; } = 30;
}