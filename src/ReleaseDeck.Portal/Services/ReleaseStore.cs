using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ReleaseDeck.Core;

namespace ReleaseDeck.Portal.Services;

/// <summary>Holds browser drafts and ephemeral file handles separately from portable or shared project documents.</summary>
public sealed class ReleaseStore(IJSRuntime js)
{
    const string StorageKey = "release-deck-library-v1";
    IJSObjectReference? module;
    Task? initialization;
    bool persistenceBlocked;
    static readonly JsonSerializerOptions LibraryJson = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 24 };
    readonly SemaphoreSlim persistence = new(1, 1);
    public List<LocalRelease> Library { get; private set; } = [];
    public LocalRelease Active { get; private set; } = new();
    public Workspace? Online { get; set; }
    public bool Dirty { get; set; }
    public bool Loaded { get; private set; }
    public string StorageWarning { get; private set; } = "";
    public bool DiagnosticLogging { get; set; }
    public long Generation { get; private set; }
    public Dictionary<string, MediaBinding> Bindings { get; } = [];
    public event Action? Changed;

    /// <summary>Loads the saved library once; a damaged copy is surfaced rather than silently replaced.</summary>
    public Task Initialize() => initialization ??= InitializeCore();
    async Task InitializeCore()
    {
        if (Loaded) return;
        module ??= await js.InvokeAsync<IJSObjectReference>("import", "./app.js");
        try
        {
            var saved = await module.InvokeAsync<string?>("read", StorageKey);
            if (!string.IsNullOrWhiteSpace(saved))
            {
                var library = JsonSerializer.Deserialize<List<LocalRelease>>(saved, LibraryJson);
                if (!ReadableLibrary(library)) throw new JsonException("The saved library is incomplete.");
                Library = library!;
            }
        }
        catch (Exception error) when (error is JSException or JsonException)
        {
            persistenceBlocked = true;
            StorageWarning = "Saved releases could not be opened and have not been replaced. You can start or import a release in this tab. Download the project before leaving to keep your work.";
            if (DiagnosticLogging) Console.Error.WriteLine("Local library unavailable: " + error.GetType().Name);
        }
        Loaded = true;
        if (Library.Count > 0) Active = Library.OrderByDescending(item => item.UpdatedAt).First();
        Notify();
    }

    /// <summary>Starts a separate local release and never replaces an earlier saved draft.</summary>
    public void New(ReleaseProject? body = null) { Active = new() { Body = body ?? new() }; Library.Add(Active); Online = null; Dirty = true; Generation++; Notify(); }
    public void Open(LocalRelease release) { Active = release; Online = null; Dirty = false; Generation++; Notify(); }
    public void Load(Workspace workspace)
    {
        Online = workspace;
        var json = ReleaseRules.Export(workspace.Body);
        var saved = Library.FirstOrDefault(item => item.WorkspaceId == workspace.Id && item.Revision == workspace.Revision && JsonSerializer.Serialize(item.Body, ReleaseRules.PortableJson) == json);
        Active = saved ?? new() { Body = ReleaseRules.Import(json), WorkspaceId = workspace.Id, Revision = workspace.Revision };
        if (saved is null) Library.Add(Active);
        Dirty = false; Generation++; Notify();
    }
    /// <summary>Applies only the returned identity/revision, leaving the caller's newer in-memory fields untouched.</summary>
    public void Saved(Workspace workspace) { Online = workspace; Active.WorkspaceId = workspace.Id; Active.Revision = workspace.Revision; Dirty = false; Notify(); }
    public void MarkChanged() { Dirty = true; Notify(); }
    public bool Editable => Online is null || Online.Role is "owner" or "editor" && Online.ArchivedAt is null;
    public ISet<string> BoundEpisodes => Bindings.Where(item => item.Value.LocalId == Active.Id && item.Value.Identified).Select(item => item.Key).ToHashSet();
    /// <summary>Writes authored metadata only; on failure the active document remains in memory for download.</summary>
    public async Task Persist()
    {
        await Initialize();
        await persistence.WaitAsync();
        try
        {
            if (!Library.Contains(Active)) Library.Add(Active);
            Active.UpdatedAt = DateTimeOffset.UtcNow;
            if (!persistenceBlocked)
            {
                try { await module!.InvokeVoidAsync("write", StorageKey, JsonSerializer.Serialize(Library, LibraryJson)); }
                catch (JSException error)
                {
                    persistenceBlocked = true;
                    StorageWarning = "Saving on this device was not confirmed. Your release is still open in this tab. Download the project before leaving to keep your work.";
                    if (DiagnosticLogging) Console.Error.WriteLine("Local library save unavailable: " + error.GetType().Name);
                }
            }
            Notify();
        }
        finally { persistence.Release(); }
    }
    public async Task Download(string name, string text, string mime = "application/json") { module ??= await js.InvokeAsync<IJSObjectReference>("import", "./app.js"); await module.InvokeVoidAsync("download", name, text, mime); }
    public async Task DownloadPackage() { module ??= await js.InvokeAsync<IJSObjectReference>("import", "./app.js"); await module.InvokeVoidAsync("downloadBase64", "release-package.zip", Convert.ToBase64String(ReleaseExports.Package(Active.Body)), "application/zip"); }
    public async Task DownloadFrame(string dataUrl) { const string prefix = "data:image/jpeg;base64,"; if (!dataUrl.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidDataException("The captured frame is not a JPEG image."); module ??= await js.InvokeAsync<IJSObjectReference>("import", "./app.js"); await module.InvokeVoidAsync("downloadBase64", "episode-frame.jpg", dataUrl[prefix.Length..], "image/jpeg"); }
    public async Task<string?> Import(ElementReference input) { module ??= await js.InvokeAsync<IJSObjectReference>("import", "./app.js"); return await module.InvokeAsync<string?>("readProject", input); }
    public async Task RememberInvite(string value) { module ??= await js.InvokeAsync<IJSObjectReference>("import", "./app.js"); await module.InvokeVoidAsync("rememberInvite", value); }
    public async Task<string?> RecallInvite() { module ??= await js.InvokeAsync<IJSObjectReference>("import", "./app.js"); return await module.InvokeAsync<string?>("recallInvite"); }
    public async Task ForgetInvite() { module ??= await js.InvokeAsync<IJSObjectReference>("import", "./app.js"); await module.InvokeVoidAsync("forgetInvite"); }
    // In-progress fields may be empty or invalid for publication; they must still be safe to reopen and correct.
    static bool ReadableLibrary(List<LocalRelease>? library)
    {
        if (library is null) return false;
        HashSet<string> releases = new(StringComparer.Ordinal);
        foreach (var release in library)
        {
            if (release is null || string.IsNullOrEmpty(release.Id) || !releases.Add(release.Id) || release.Body is not { Schema: 1 } body || body.Title is null || body.Description is null || body.Episodes is null) return false;
            HashSet<string> episodes = new(StringComparer.Ordinal);
            foreach (var episode in body.Episodes)
            {
                if (episode is null || string.IsNullOrEmpty(episode.Id) || !episodes.Add(episode.Id) || episode.Title is null || episode.Description is null || episode.DefaultLanguage is null || episode.CategoryId is null || episode.Tags is null || episode.Tags.Any(tag => tag is null) || episode.Localizations is null || episode.Localizations.Any(text => text is null || text.Language is null || text.Title is null || text.Description is null)) return false;
                if (episode.File is { } file && (file.Name is null || file.Mime is null || file.Algorithm is null || file.Fingerprint is null || file.Duration is { } seconds && (!double.IsFinite(seconds) || seconds < 0 || seconds > TimeSpan.MaxValue.TotalSeconds))) return false;
                if (episode.File is not null)
                {
                    try { ReleaseRules.Validate(new ReleaseProject { Episodes = [new Episode { File = episode.File }] }); }
                    catch (InvalidDataException) { return false; }
                }
            }
        }
        return true;
    }
    public void Notify() => Changed?.Invoke();
}
/// <summary>An in-memory browser file reference that cannot be exported or submitted with the project body.</summary>
public sealed class MediaBinding { public string LocalId { get; set; } = ""; public string ClientFileId { get; set; } = ""; public string Name { get; set; } = ""; public long Size { get; set; } public long LastModified { get; set; } public string MimeType { get; set; } = ""; public double? DurationMs { get; set; } public int? Width { get; set; } public int? Height { get; set; } public bool Identified { get; set; } }
public sealed class FileDescriptor { public string ClientFileId { get; set; } = ""; public string Name { get; set; } = ""; public long Size { get; set; } public long LastModified { get; set; } public string MimeType { get; set; } = ""; }
public sealed class MediaInspection { public double? DurationMs { get; set; } public int? Width { get; set; } public int? Height { get; set; } public string MimeType { get; set; } = ""; }
public sealed class FileFingerprint { public string Algorithm { get; set; } = ""; public string Fingerprint { get; set; } = ""; public long Size { get; set; } }
/// <summary>A temporary frame from the attached local video; never serialized into a release document.</summary>
public sealed class CapturedFrame { public string ClientFileId { get; set; } = ""; public string DataUrl { get; set; } = ""; public int Width { get; set; } public int Height { get; set; } public double TimeMs { get; set; } }
