namespace NewsProviders.Providers.Regeringen;


public sealed class RegeringenOptions
{
    public string ProviderName { get; set; } = "Regeringen";
    
    public string PressReleasesRssUrl { get; set; } =
        "https://www.regeringen.se/pressmeddelanden.rss"; 

    public int MaxItems { get; set; } = 30;
}