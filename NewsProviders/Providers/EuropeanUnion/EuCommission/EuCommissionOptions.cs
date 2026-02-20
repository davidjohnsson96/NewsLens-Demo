namespace NewsProviders.Providers.EuropeanUnion.EuCommission;

public sealed class EuCommissionOptions
{
    public string ProviderName { get; set; } = "EUCommission";

    /// <summary>
    /// RSS feed URL for Commission press / news.
    /// Keep this configurable so you can swap feeds without code changes.
    /// </summary>
    public string RssUrl { get; set; } = string.Empty;

    /// <summary>
    /// Max number of RSS items to process per run.
    /// </summary>
    public int MaxItems { get; set; } = 30;
}