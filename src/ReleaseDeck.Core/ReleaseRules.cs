using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReleaseDeck.Core;

/// <summary>Validates portable documents and separates editable draft limits from upload readiness.</summary>
public static class ReleaseRules
{
    public const string FingerprintAlgorithm = "rd-sha256-tree-4m-v1";
    public const int MaximumBytes = 512 * 1024;
    public static readonly JsonSerializerOptions PortableJson = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 20,
        WriteIndented = true
    };

    /// <summary>Reads only the defined schema and rejects unknown fields rather than silently dropping private or remote data.</summary>
    public static ReleaseProject Import(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new InvalidDataException("This project file is larger than 512 KB.");
        ReleaseProject project;
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 20 });
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("schema", out var schema) || !schema.TryGetInt32(out var number) || number != 1) throw new InvalidDataException("Choose a version 1 Release Deck project file.");
            CheckUniqueProperties(document.RootElement);
            project = JsonSerializer.Deserialize<ReleaseProject>(json, PortableJson) ?? throw new InvalidDataException("Choose a release project file.");
        }
        catch (JsonException error) { throw new InvalidDataException("This is not a valid Release Deck project. Check its format and remove unrecognized fields.", error); }
        Validate(project);
        return project;
    }

    /// <summary>Serializes a validated metadata-only document for downloading or cloud storage.</summary>
    public static string Export(ReleaseProject project)
    {
        Validate(project);
        var json = JsonSerializer.Serialize(project, PortableJson);
        if (Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new InvalidDataException("This release is larger than 512 KB. Shorten its descriptions.");
        return json;
    }

    /// <summary>Copies a release without sharing mutable episode or translation collections.</summary>
    public static ReleaseProject Clone(ReleaseProject project) => Import(Export(project));

    /// <summary>Checks structural and bounded field validity while allowing incomplete publication decisions in a draft.</summary>
    public static void Validate(ReleaseProject project)
    {
        if (project.Schema != 1) throw new InvalidDataException("This project uses an unsupported version.");
        Bounded(project.Title, 120, "Release title", false); Bounded(project.Description, 2000, "Release description");
        if (project.Episodes is null || project.Episodes.Count > 40) throw new InvalidDataException("A release can contain up to 40 episodes.");
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        foreach (var episode in project.Episodes)
        {
            if (episode is null || !Guid.TryParseExact(episode.Id, "D", out _) || !ids.Add(episode.Id)) throw new InvalidDataException("Each episode needs a distinct valid identity.");
            Bounded(episode.Title, 100, "Episode title"); Bounded(episode.Description, 5000, "Episode description");
            if (!Language(episode.DefaultLanguage)) throw new InvalidDataException("Choose an episode language such as en or en-GB.");
            if (episode.CategoryId is null || episode.CategoryId.Length is < 1 or > 4 || !episode.CategoryId.All(char.IsAsciiDigit)) throw new InvalidDataException("Choose a video category.");
            if (episode.Tags is null || episode.Tags.Count > 30 || episode.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 100) || episode.Tags.Sum(tag => tag.Length + (tag.Contains(' ') ? 2 : 0)) + Math.Max(0, episode.Tags.Count - 1) > 500) throw new InvalidDataException("Tags must fit within 500 characters in total.");
            if (episode.Localizations is null || episode.Localizations.Count > 20) throw new InvalidDataException("An episode can contain up to 20 translations.");
            HashSet<string> languages = new(StringComparer.OrdinalIgnoreCase);
            foreach (var item in episode.Localizations)
            {
                if (item is null || !Language(item.Language) || !languages.Add(item.Language) || string.Equals(item.Language, episode.DefaultLanguage, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Each translation needs its own language, different from the original.");
                Bounded(item.Title, 100, "Translated title"); Bounded(item.Description, 5000, "Translated description");
            }
            if (episode.File is { } file)
            {
                Bounded(file.Name, 255, "File name", false); Bounded(file.Mime, 120, "File type");
                if (file.Size is <= 0 or > 274877906944 || file.LastModified < 0 || file.Algorithm != FingerprintAlgorithm || file.Fingerprint is null || file.Fingerprint.Length != 64 || !file.Fingerprint.All(char.IsAsciiHexDigit)) throw new InvalidDataException("The video file identity is incomplete or unsupported. Reattach and identify the file.");
                if (file.Duration is { } duration && (!double.IsFinite(duration) || duration <= 0) || file.Width is <= 0 or > 32768 || file.Height is <= 0 or > 32768) throw new InvalidDataException("The saved video dimensions or duration are invalid.");
            }
        }
    }

    /// <summary>Reports required publication decisions and locally observable media problems without asserting remote readiness.</summary>
    public static List<PreflightIssue> Preflight(ReleaseProject project, ISet<string>? boundEpisodes = null, DateTimeOffset? now = null)
    {
        List<PreflightIssue> issues = [];
        foreach (var episode in project.Episodes)
        {
            void Add(string field, string text, bool blocking = true) => issues.Add(new(episode.Id, field, text, blocking));
            if (string.IsNullOrWhiteSpace(episode.Title)) Add("title", "Add a title.");
            if (episode.Title.Contains('<') || episode.Title.Contains('>')) Add("title", "Remove angle brackets from the title.");
            if (string.IsNullOrWhiteSpace(episode.Description)) Add("description", "Add a description to help viewers understand this episode.", false);
            if (Encoding.UTF8.GetByteCount(episode.Description) > 5000 || episode.Description.Contains('<') || episode.Description.Contains('>')) Add("description", "Descriptions must fit within 5,000 bytes and cannot contain angle brackets.");
            if (episode.MadeForKids is null) Add("audience", "Choose whether this episode is made for children.");
            if (episode.ContainsSyntheticMedia is null) Add("synthetic", "Answer the altered or synthetic content question.");
            if (episode.File is null) Add("file", "Attach and identify the original video.");
            else
            {
                if (boundEpisodes is not null && !boundEpisodes.Contains(episode.Id)) Add("file", "Reattach the matching video on this device.");
                if (episode.File.Duration is null) Add("file", "This browser could not read the video duration. Check the original before uploading.", false);
            }
            if (episode.PublishAt is { } time && time <= (now ?? DateTimeOffset.UtcNow)) Add("schedule", "The planned release time is in the past. Choose a new time or leave it unscheduled.");
            foreach (var translation in episode.Localizations)
            {
                if (string.IsNullOrWhiteSpace(translation.Title)) Add("translations", "Complete the " + translation.Language + " title.");
                if (translation.Title.Contains('<') || translation.Title.Contains('>') || translation.Description.Contains('<') || translation.Description.Contains('>') || Encoding.UTF8.GetByteCount(translation.Description) > 5000) Add("translations", "Check the " + translation.Language + " text: no angle brackets, and at most 5,000 bytes per description.");
            }
        }
        return issues;
    }

    /// <summary>Tests exact byte identity for reattachment; names and timestamps are informational rather than proof.</summary>
    public static bool Matches(MediaIdentity expected, string algorithm, string fingerprint, long size) => expected.Algorithm == algorithm && expected.Size == size && string.Equals(expected.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase);

    /// <summary>Accepts bounded language tags without pretending to enumerate every supported provider language.</summary>
    public static bool Language(string? value) => value is { Length: >= 2 and <= 35 } && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '-') && char.IsAsciiLetter(value[0]) && value[^1] != '-';

    /// <summary>Rejects null, oversized or required empty text without changing creator-authored content.</summary>
    static void Bounded(string? value, int maximum, string label, bool empty = true)
    {
        if (value is null || value.Length > maximum || !empty && string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(label + " must contain " + (empty ? "at most " : "1–") + maximum + " characters.");
    }

    /// <summary>Rejects ambiguous repeated keys in nested portable objects instead of accepting the last value.</summary>
    static void CheckUniqueProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject()) { if (!names.Add(property.Name)) throw new InvalidDataException("Repeated project fields are not accepted."); CheckUniqueProperties(property.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) CheckUniqueProperties(item);
    }
}
