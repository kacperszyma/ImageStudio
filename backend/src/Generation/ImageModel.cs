namespace Generation;

public abstract class ImageModel
{
    private static readonly List<ImageModel> _extent = [];

    public static readonly ImageModel DallE3 = new DallE3Model();
    public static readonly ImageModel StableDiffusionXL = new StableDiffusionXLModel();
    public static readonly ImageModel MidjourneyV6 = new MidjourneyV6Model();
    public static readonly ImageModel FluxPro = new FluxProModel();

    public static IReadOnlyList<ImageModel> All => _extent;

    public string Slug { get; }

    protected ImageModel(string slug)
    {
        Slug = slug;
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

    private sealed class DallE3Model() : ImageModel("dall-e-3");
    private sealed class StableDiffusionXLModel() : ImageModel("stable-diffusion-xl");
    private sealed class MidjourneyV6Model() : ImageModel("midjourney-v6");
    private sealed class FluxProModel() : ImageModel("flux-pro");
}
