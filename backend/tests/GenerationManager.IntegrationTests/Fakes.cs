using System.Collections.Concurrent;
using Generation.Contracts;
using GenerationManager.Contracts;

namespace GenerationManager.IntegrationTests;

/// <summary>
/// Stand-in for the Generation module. Lets a test control the provider request id
/// and, crucially, force <see cref="CompleteGenerationAsync"/> to fail once — that
/// is the "record the image artifact" step that runs AFTER the wallet is charged
/// and the job is flipped to Completed.
/// </summary>
internal sealed class FakeGenerationService : IGenerationService
{
    public long Cost { get; init; } = 50;
    public string NextRequestId { get; set; } = "req-1";

    /// <summary>When true, the next CompleteGenerationAsync throws, then resets.</summary>
    public bool FailNextArtifactWrite { get; set; }

    private readonly ConcurrentDictionary<string, (string Model, string Prompt)> _submitted = new();
    private readonly ConcurrentDictionary<string, string> _recordedImages = new();

    public string? RecordedImageFor(string requestId) =>
        _recordedImages.TryGetValue(requestId, out var url) ? url : null;

    public List<ModelDto> GetModels() => [];
    public long GetCost(string modelSlug) => Cost;

    public Task<string> SubmitAsync(Guid userId, string modelSlug, string prompt)
    {
        _submitted[NextRequestId] = (modelSlug, prompt);
        return Task.FromResult(NextRequestId);
    }

    public Task<string> CompleteGenerationAsync(string requestId, string imageUrl)
    {
        if (FailNextArtifactWrite)
        {
            FailNextArtifactWrite = false;
            throw new InvalidOperationException("Simulated failure recording the image artifact.");
        }

        _recordedImages[requestId] = imageUrl;
        return Task.FromResult(imageUrl);
    }
}

internal sealed class FakeGenerationQueryService : IGenerationQueryService
{
    public Task<GenerationSummary?> GetDetailsByRequestIdAsync(string falRequestId) =>
        Task.FromResult<GenerationSummary?>(null);

    public Task<IReadOnlyDictionary<string, GenerationSummary>> GetSummariesByRequestIdsAsync(IEnumerable<string> falRequestIds) =>
        Task.FromResult<IReadOnlyDictionary<string, GenerationSummary>>(new Dictionary<string, GenerationSummary>());
}

/// <summary>Counts the user-facing notifications the saga emits.</summary>
internal sealed class RecordingNotifier : IGenerationNotifier
{
    public int CompletedCount { get; private set; }
    public int FailedCount { get; private set; }

    public Task CompletedAsync(Guid userId, Guid jobId, string imageUrl)
    {
        CompletedCount++;
        return Task.CompletedTask;
    }

    public Task FailedAsync(Guid userId, Guid jobId)
    {
        FailedCount++;
        return Task.CompletedTask;
    }
}
