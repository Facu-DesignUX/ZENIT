using System.Text.Json.Serialization;

namespace ZENIT.Blazor.Models;

public class RadioServer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}
