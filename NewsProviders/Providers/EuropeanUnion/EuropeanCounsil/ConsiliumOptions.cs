namespace NewsProviders.Providers.EuropeanUnion.EuropeanCounsil;


public sealed class ConsiliumOptions
{
    public string RssUrl { get; set; } =
        "https://www.consilium.europa.eu/en/rss/pressreleases.ashx";

    public int MaxItems { get; set; } = 50;
}