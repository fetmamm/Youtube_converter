using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace YoutubeConverter.Services;

public sealed record UpdateInfo(string LatestVersion, string ReleaseUrl);

public static class UpdateService
{
    // Ändra till ditt eget repo när du publicerar releases på GitHub.
    public const string GitHubOwner = "your-github-username";
    public const string GitHubRepo = "Youtube_converter";

    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "YoutubeConverter-UpdateCheck" },
            { "Accept", "application/vnd.github+json" }
        },
        Timeout = TimeSpan.FromSeconds(8)
    };

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        if (GitHubOwner.StartsWith("your-")) return null;

        try
        {
            var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
            using var response = await Http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            var htmlUrl = doc.RootElement.GetProperty("html_url").GetString();
            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(htmlUrl)) return null;

            var clean = tag.TrimStart('v', 'V');
            if (!Version.TryParse(clean, out var latest)) return null;

            return latest > CurrentVersion ? new UpdateInfo(latest.ToString(), htmlUrl) : null;
        }
        catch
        {
            return null;
        }
    }
}
