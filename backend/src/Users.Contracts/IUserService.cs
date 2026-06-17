namespace Users.Contracts;

public interface IUserService
{
    Task EnsureProvisioned(string sub);
    Task<bool> TryCreate(string sub);
    Task<bool> Exists(string sub);
    Task<UserDto?> Get(string sub);
}
public record UserDto(string Sub, DateTime CreatedAt);