namespace Generation;

internal sealed class GenerationInstance
{
    public Guid Id { get; set; }
    public string UserSub { get; set; } = null!;
    public string ImageModel { get; set; } = null!;
}