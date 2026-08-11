using System.Text.Json.Serialization;

namespace ZENIT.Blazor.Models;

public class RadioStation
{
    [JsonPropertyName("stationuuid")]
    public string StationUuid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("url_resolved")]
    public string UrlResolved { get; set; } = string.Empty;

    [JsonPropertyName("homepage")]
    public string Homepage { get; set; } = string.Empty;

    [JsonPropertyName("favicon")]
    public string Favicon { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("votes")]
    public int Votes { get; set; }

    [JsonPropertyName("bitrate")]
    public int Bitrate { get; set; }
}
