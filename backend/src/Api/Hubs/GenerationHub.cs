using GenerationManager.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

[Authorize]
public sealed class GenerationHub(IGenerationManager generationManager) : Hub
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
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("GenerationFailed", ex.Message);
        }
    }
}
