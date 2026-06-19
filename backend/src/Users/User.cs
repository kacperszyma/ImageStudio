namespace Users;

internal sealed class User
{
    public Guid Id { get; set; }
    public string Sub { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
