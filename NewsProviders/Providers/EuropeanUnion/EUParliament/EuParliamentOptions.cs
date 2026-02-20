namespace NewsProviders.Providers.EuropeanUnion.EUParliament;

public sealed class EuParliamentOptions
{
    public string RssUrl { get; set; } =
        "https://www.europarl.europa.eu/rss/doc/press-releases/en.xml";

    public int MaxItems { get; set; } = 50;
}