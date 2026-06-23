namespace Generation;

internal sealed class GenerationInstance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ImageModel { get; set; } = null!;
    public string Prompt { get; set; } = null!;

    /// <summary>Provider request id; correlates the webhook callback to this row.</summary>
    public string? FalRequestId { get; set; }

    /// <summary>Null until the generation completes and the image is recorded.</summary>
    public string? ResultUrl { get; set; }
}