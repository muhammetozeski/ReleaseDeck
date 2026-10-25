using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReleaseDeck.Core;

/// <summary>A portable authored release; media bytes, channel credentials and remote receipts are deliberately absent.</summary>
public sealed class ReleaseProject
{
    public int Schema { get; set; } = 1;
    public string Title { get; set; } = "Untitled release";
    public string Description { get; set; } = "";
    public List<Episode> Episodes { get; set; } = [];
}

/// <summary>Editable publication intent for one video, independent of any remote operation.</summary>
public sealed class Episode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DefaultLanguage { get; set; } = "en";
    public List<Translation> Localizations { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string CategoryId { get; set; } = "27";
    public bool? MadeForKids { get; set; }
    public bool? ContainsSyntheticMedia { get; set; }
    [JsonConverter(typeof(UtcReleaseTimeConverter))]
    public DateTimeOffset? PublishAt { get; set; }
    public MediaIdentity? File { get; set; }
}

/// <summary>A creator supplied title and description in another language; this is not an automatic translation.</summary>
public sealed class Translation
{
    public string Language { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>Rebind information for the original file; the fingerprint names its explicit tree-hash algorithm.</summary>
public sealed class MediaIdentity
{
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public long LastModified { get; set; }
    public string Mime { get; set; } = "";
    public string Algorithm { get; set; } = ReleaseRules.FingerprintAlgorithm;
    public string Fingerprint { get; set; } = "";
    public double? Duration { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

/// <summary>A concrete preflight issue linked to an episode and a field the creator can change.</summary>
public sealed record PreflightIssue(string EpisodeId, string Field, string Message, bool BlocksUpload);

/// <summary>A browser-only library entry; online revision information does not enter the portable document.</summary>
public sealed class LocalRelease
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? WorkspaceId { get; set; }
    public int? Revision { get; set; }
    public ReleaseProject Body { get; set; } = new();
}

/// <summary>Projects release instants as explicit UTC milliseconds in portable and API documents.</summary>
public sealed class UtcReleaseTimeConverter : JsonConverter<DateTimeOffset?>
{
    public override bool HandleNull => true;
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.String || !DateTimeOffset.TryParse(reader.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)) throw new JsonException("The release time must be an ISO date.");
        return value.ToUniversalTime();
    }
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
    }
}
