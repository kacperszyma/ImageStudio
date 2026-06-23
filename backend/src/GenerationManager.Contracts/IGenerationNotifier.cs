namespace GenerationManager.Contracts;

/// <summary>
/// Port for telling a user about the outcome of their generation. Owned by the
/// manager (which decides <em>when</em> to notify); the delivery mechanism
/// (e.g. SignalR) is hidden behind an adapter in the host.
/// </summary>
public interface IGenerationNotifier
{
    Task CompletedAsync(Guid userId, Guid jobId, string imageUrl);
    Task FailedAsync(Guid userId, Guid jobId);
}
