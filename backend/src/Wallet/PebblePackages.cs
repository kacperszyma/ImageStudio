namespace Wallet;

public abstract class PebblePackage
{
    private static readonly List<PebblePackage> _extent = [];

    public static readonly PebblePackage Basic = new BasicPebblePackage();
    public static readonly PebblePackage Medium = new MediumPebblePackage();
    public static readonly PebblePackage Large = new LargePebblePackage();

    public static IReadOnlyList<PebblePackage> All => _extent;
    private static decimal ConversionRate = 200;
    
    public decimal DollarPrice { get; }

    public decimal PebbleAmount => DollarPrice * ConversionRate * (1 -  DisountRate);

    public decimal DisountRate { get; }
    public string NameId { get; }

    protected PebblePackage(decimal dollarPrice, decimal disountRate, string nameId)
    {
        DollarPrice = dollarPrice;
        DisountRate = disountRate;
        
        NameId = nameId;
        
        _extent.Add(this);
    }

    public static PebblePackage FromName(string name) =>
        _extent.FirstOrDefault(p => string.Equals(p.NameId, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Unknown image model: '{name}'", nameof(name));

    public override string ToString() => $"Pebble package amount: {PebbleAmount} discount: {DisountRate} price: {DollarPrice}";

    private sealed class BasicPebblePackage() : PebblePackage(1m,0,"pebbles_200");
    private sealed class MediumPebblePackage() : PebblePackage(4.5m,0.1m,"pebbles_810");
    private sealed class LargePebblePackage() : PebblePackage(8m,0.2m,"pebbles_1280");
}