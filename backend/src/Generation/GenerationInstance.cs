namespace Generation;

internal sealed class GenerationInstance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ImageModel { get; set; } = null!;
    public string Prompt { get; set; } = null!;

    public string ResultUrl { get; set; } = null!;
}