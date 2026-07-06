using GenerationManager.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Wallet.Contracts;

namespace Api.Hubs;

[Authorize]
public sealed class GenerationHub(IGenerationManager generationManager, ILogger<GenerationHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Join a group keyed by user id so the webhook can push results to this
        // user even though it's a different request with no Clients.Caller.
        var userId = (Guid)Context.GetHttpContext()!.Items["UserId"]!;
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
        await base.OnConnectedAsync();
    }

    public async Task Generate(string modelSlug, string prompt)
    {
        var userId = (Guid)Context.GetHttpContext()!.Items["UserId"]!;

        try
        {
            var jobId = await generationManager.GenerateAsync(userId, modelSlug, prompt);
            // Acknowledge the enqueue. "GenerationComplete"/"GenerationFailed"
            // arrive later from the webhook via SignalRGenerationNotifier.
            await Clients.Caller.SendAsync("GenerationAccepted", jobId);
        }
        catch (InsufficientFundsException)
        {
            await Clients.Caller.SendAsync("GenerationFailed", "InsufficientFunds");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue generation for user {UserId}", userId);
            await Clients.Caller.SendAsync("GenerationFailed", "EnqueueFailed");
        }
    }
}
