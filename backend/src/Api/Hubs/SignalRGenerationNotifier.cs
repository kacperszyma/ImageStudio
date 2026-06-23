using GenerationManager.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

/// <summary>
/// Delivers generation outcomes to the user over SignalR. Each connection joins
/// a group named by its user id (see <see cref="GenerationHub.OnConnectedAsync"/>),
/// so the webhook — a separate request with no "caller" — can still reach them.
/// </summary>
internal sealed class SignalRGenerationNotifier(IHubContext<GenerationHub> hub) : IGenerationNotifier
{
    public Task CompletedAsync(Guid userId, Guid jobId, string imageUrl) =>
        hub.Clients.Group(userId.ToString()).SendAsync("GenerationComplete", jobId, imageUrl);

    public Task FailedAsync(Guid userId, Guid jobId) =>
        hub.Clients.Group(userId.ToString()).SendAsync("GenerationFailed", jobId);
}
