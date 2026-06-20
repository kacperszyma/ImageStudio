namespace Generation;

public abstract class ImageModel
{
    private static readonly List<ImageModel> _extent = [];

    public static readonly ImageModel FluxSchnell = new FluxSchnellModel();
    public static readonly ImageModel GrokImage = new GrokImageModel();
    public static readonly ImageModel Flux2Pro = new Flux2ProModel();
    public static readonly ImageModel NanoBanana2 = new NanoBanana2Model();
    public static readonly ImageModel NanoBananaPro = new NanoBananaProModel();
    public static readonly ImageModel GptImage2 = new GptImage2Model();

    public static IReadOnlyList<ImageModel> All => _extent;

    public string Slug { get; }
    public long CreditCost { get; }
    public string FalModelId { get; }

    protected ImageModel(string slug, long creditCost, string falModelId)
    {
        Slug = slug;
        CreditCost = creditCost;
        FalModelId = falModelId;
        _extent.Add(this);
    }

    public static ImageModel FromString(string slug) =>
        _extent.FirstOrDefault(m => string.Equals(m.Slug, slug, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Unknown image model: '{slug}'", nameof(slug));

    public static bool TryFromString(string slug, out ImageModel model)
    {
        model = _extent.FirstOrDefault(m => string.Equals(m.Slug, slug, StringComparison.OrdinalIgnoreCase))!;
        return model is not null;
    }

    public override string ToString() => Slug;

    private sealed class FluxSchnellModel() : ImageModel("flux-schnell", 3, "fal-ai/flux/schnell");
    private sealed class GrokImageModel() : ImageModel("grok-image", 22, "xai/grok-imagine-image");
    private sealed class Flux2ProModel() : ImageModel("flux-2-pro", 30, "fal-ai/flux-2-pro");
    private sealed class NanoBanana2Model() : ImageModel("nano-banana-2", 80, "fal-ai/nano-banana-2");
    private sealed class NanoBananaProModel() : ImageModel("nano-banana-pro", 150, "fal-ai/nano-banana-pro");
    private sealed class GptImage2Model() : ImageModel("gpt-image-2", 167, "openai/gpt-image-2");
}
