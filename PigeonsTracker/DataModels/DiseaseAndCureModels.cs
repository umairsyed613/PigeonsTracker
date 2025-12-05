using System.Text.Json.Serialization;

namespace PigeonsTracker.DataModels;

/// <summary>
/// Represents the root structure of the disease and cure data JSON file.
/// Contains version information for cache invalidation.
/// </summary>
public class DiseaseAndCureData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("diseases")]
    public List<DiseaseItem> Diseases { get; set; } = new();
}

/// <summary>
/// Represents a single disease with its cure information.
/// </summary>
public class DiseaseItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("cure")]
    public string Cure { get; set; } = string.Empty;
}

/// <summary>
/// Represents a formatted paragraph for displaying cure content.
/// </summary>
public class ContentParagraph
{
    public string Text { get; set; } = string.Empty;
    public bool IsHeading { get; set; }
    public bool IsBullet { get; set; }
}
