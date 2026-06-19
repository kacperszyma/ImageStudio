using GenerationManager.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

[Authorize]
public sealed class GenerationHub(IGenerationManager generationManager) : Hub
{
    public async Task Generate(string modelSlug, string prompt)
    {
        var userId = (Guid)Context.GetHttpContext()!.Items["UserId"]!;

        try
        {
            await Clients.Caller.SendAsync("GenerationProgress", 70);
            var result = await generationManager.GenerateAsync(userId, modelSlug, prompt);
            await Clients.Caller.SendAsync("GenerationComplete", result.JobId, result.ImageUrl);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("GenerationFailed", ex.Message);
        }
    }
}
