using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using StickyDo.Widget.Interfaces;
using StickyDo.Widget.Utilities;
using Windows.Services.Store;

namespace StickyDo.Widget.Services;

/// <summary>
/// Checks the Microsoft Store update API first, falling back to the project's GitHub Releases
/// when the Store check is unavailable (e.g. running unpackaged, or before the app has a Store
/// listing). Any failure in either path is caught and reported as
/// <see cref="UpdateCheckStatus.Failed"/> rather than crashing the caller.
/// </summary>
public class UpdateService : IUpdateService
{
    private const string GitHubLatestReleasePath = "repos/anuviswan/sticky-todo/releases/latest";

    private readonly HttpClient _httpClient;

    public UpdateService(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        var storeResult = await TryCheckStoreAsync();
        return storeResult ?? await CheckGitHubReleaseAsync();
    }

    private async Task<UpdateCheckResult?> TryCheckStoreAsync()
    {
        try
        {
            var storeContext = StoreContext.GetDefault();
            var updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
            if (updates.Count == 0)
            {
                return new UpdateCheckResult { Status = UpdateCheckStatus.UpToDate };
            }

            var latestVersion = updates
                .Select(update => update.Package.Id.Version)
                .Select(version => new Version(version.Major, version.Minor, version.Build, version.Revision))
                .Max();

            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.UpdateAvailable,
                LatestVersion = latestVersion?.ToString()
            };
        }
        catch (Exception ex)
        {
            // Expected when the app has no Store package identity (e.g. running unpackaged, or
            // sideloaded before the app has a Store listing) - fall back to the GitHub check
            // instead of surfacing this as a failure to the user.
            LoggerHelper.LogException(ex, nameof(TryCheckStoreAsync));
            return null;
        }
    }

    private async Task<UpdateCheckResult> CheckGitHubReleaseAsync()
    {
        try
        {
            var release = await _httpClient.GetFromJsonAsync<GitHubReleaseResponse>(GitHubLatestReleasePath);
            var latestVersionText = release?.TagName?.TrimStart('v', 'V');
            if (string.IsNullOrEmpty(latestVersionText) || !Version.TryParse(latestVersionText, out var latestVersion))
            {
                return new UpdateCheckResult
                {
                    Status = UpdateCheckStatus.Failed,
                    ErrorMessage = "Could not determine the latest published version."
                };
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            return latestVersion > currentVersion
                ? new UpdateCheckResult { Status = UpdateCheckStatus.UpdateAvailable, LatestVersion = latestVersionText }
                : new UpdateCheckResult { Status = UpdateCheckStatus.UpToDate };
        }
        catch (Exception ex)
        {
            LoggerHelper.LogException(ex, nameof(CheckGitHubReleaseAsync));
            return new UpdateCheckResult { Status = UpdateCheckStatus.Failed, ErrorMessage = ex.Message };
        }
    }

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }
    }
}
