using Generation.Contracts;
using GenerationManager.Contracts;
using Wallet.Contracts;

namespace GenerationManager;

internal sealed class GenerationManagerService(
    IGenerationService generationService,
    IWalletService walletService) : IGenerationManager
{
    public async Task<GenerationResultDto> GenerateAsync(Guid userId, string modelSlug, string prompt)
    {
        var jobId = Guid.NewGuid();
        var cost = generationService.GetCost(modelSlug);

        await walletService.FreezeFundsAsync(userId, cost, jobId);

        try
        {
            var job = await generationService.RunAsync(jobId, modelSlug, prompt);
            await walletService.ChargeFrozenAsync(jobId);
            return new GenerationResultDto(job.JobId, job.ImageUrl);
        }
        catch
        {
            await walletService.UnfreezeAsync(jobId);
            throw;
        }
    }
}
