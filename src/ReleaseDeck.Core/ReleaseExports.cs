using System.IO.Compression;
using System.Net;
using System.Text;

namespace ReleaseDeck.Core;

/// <summary>Creates metadata-only delivery artifacts; local media, access tokens and remote receipts are never included.</summary>
public static class ReleaseExports
{
    /// <summary>Builds a standalone readable release sheet with escaped creator text and actionable checks.</summary>
    public static string Html(ReleaseProject project)
    {
        ReleaseRules.Validate(project);
        static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
        StringBuilder html = new("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + E(project.Title) + "</title><style>html{color-scheme:dark}body{font:16px/1.6 system-ui;max-width:840px;margin:40px auto;padding:0 24px;color:#edf3f5;background:#121a20}article{border-top:2px solid #70868d;padding:24px 0}h1,h2{line-height:1.2}p{white-space:pre-wrap}dt{font-weight:700}dd{margin:0 0 12px;overflow-wrap:anywhere}.muted{color:#b7c8ce}@media print{html{color-scheme:light}article{break-inside:avoid}body{margin:0;color:#162127;background:#fff}.muted{color:#465b62}}</style><main><h1>" + E(project.Title) + "</h1><p>" + E(project.Description) + "</p><p class=\"muted\">Release plan · " + project.Episodes.Count + " episodes · Videos are supplied separately.</p>");
        var issues = ReleaseRules.Preflight(project);
        foreach (var episode in project.Episodes)
        {
            html.Append("<article><h2>").Append(project.Episodes.IndexOf(episode) + 1).Append(". ").Append(E(episode.Title.Length > 0 ? episode.Title : "Untitled episode")).Append("</h2><p>").Append(E(episode.Description)).Append("</p><dl><dt>Original language</dt><dd>").Append(E(episode.DefaultLanguage)).Append("</dd><dt>Planned release</dt><dd>").Append(E(episode.PublishAt?.ToString("u") ?? "Unscheduled")).Append("</dd><dt>File</dt><dd>").Append(E(episode.File?.Name ?? "Not attached")).Append("</dd><dt>Audience</dt><dd>").Append(episode.MadeForKids is null ? "Not chosen" : episode.MadeForKids.Value ? "Made for children" : "Not made for children").Append("</dd><dt>Altered or synthetic content</dt><dd>").Append(episode.ContainsSyntheticMedia is null ? "Not answered" : episode.ContainsSyntheticMedia.Value ? "Yes" : "No").Append("</dd></dl>");
            foreach (var text in episode.Localizations) html.Append("<h3>").Append(E(text.Language)).Append(" · ").Append(E(text.Title)).Append("</h3><p>").Append(E(text.Description)).Append("</p>");
            var remaining = issues.Where(issue => issue.EpisodeId == episode.Id).ToList();
            if (remaining.Count > 0) { html.Append("<h3>Before delivery</h3><ul>"); foreach (var issue in remaining) html.Append("<li>").Append(E(issue.Message)).Append("</li>"); html.Append("</ul>"); }
            html.Append("</article>");
        }
        return html.Append("</main></html>").ToString();
    }

    /// <summary>Packages the validated project and release sheet as an actual ZIP without embedding video bytes.</summary>
    public static byte[] Package(ReleaseProject project)
    {
        using MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, true))
        {
            Add("release-project.json", ReleaseRules.Export(project)); Add("release-sheet.html", Html(project));
            Add("README.txt", "Open release-sheet.html to read this plan. Import release-project.json into Release Deck to continue editing. Reattach each original video when continuing on another device. This package contains no videos or channel credentials and does not publish anything.");
            void Add(string name, string text) { var entry = zip.CreateEntry(name, CompressionLevel.Optimal); using StreamWriter writer = new(entry.Open(), new UTF8Encoding(false)); writer.Write(text); }
        }
        return output.ToArray();
    }
}
