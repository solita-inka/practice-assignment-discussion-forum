using Azure;
using Azure.AI.ContentSafety;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ForumApi.Services;

public class ModerationResult
{
    public bool IsAllowed { get; set; }
    public string? RejectionReason { get; set; }
}

public interface IContentModerationService
{
    Task<ModerationResult> AnalyzeContentAsync(string content);
}

public class ContentModerationService : IContentModerationService
{
    private readonly ContentSafetyClient _client;
    private readonly ILogger<ContentModerationService> _logger;
    private const int SeverityThreshold = 2; // 0=safe, 2=low, 4=medium, 6=high

    public ContentModerationService(IConfiguration configuration, ILogger<ContentModerationService> logger)
    {
        _logger = logger;
        var endpoint = configuration["ContentSafety:Endpoint"]
            ?? throw new InvalidOperationException("ContentSafety:Endpoint is not configured.");
        var apiKey = configuration["ContentSafety:ApiKey"]
            ?? throw new InvalidOperationException("ContentSafety:ApiKey is not configured.");

        _client = new ContentSafetyClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<ModerationResult> AnalyzeContentAsync(string content)
    {
        var request = new AnalyzeTextOptions(content);

        try
        {
            var response = await _client.AnalyzeTextAsync(request);

            var categories = new Dictionary<string, int>();

            if (response.Value.CategoriesAnalysis != null)
            {
                foreach (var category in response.Value.CategoriesAnalysis)
                {
                    var severity = category.Severity ?? 0;
                    categories[category.Category.ToString()] = severity;

                    if (severity >= SeverityThreshold)
                    {
                        _logger.LogWarning(
                            "Content flagged: category={Category}, severity={Severity}",
                            category.Category, severity);

                        return new ModerationResult
                        {
                            IsAllowed = false,
                            RejectionReason = $"Content rejected: detected {category.Category} content (severity {severity})."
                        };
                    }
                }
            }

            _logger.LogDebug("Content passed moderation: {Categories}",
                string.Join(", ", categories.Select(c => $"{c.Key}={c.Value}")));

            return new ModerationResult { IsAllowed = true };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Content Safety API call failed");
            // Fail open — allow the message if the API is unavailable
            // Change to fail closed (IsAllowed = false) if you prefer stricter behavior
            return new ModerationResult { IsAllowed = true };
        }
    }
}
