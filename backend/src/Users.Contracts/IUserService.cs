namespace Users.Contracts;

public interface IUserService
{
    Task<(bool wasCreated, Guid userId)> EnsureProvisionedAsync(string sub, string email);
    Task<bool> ExistsAsync(string sub);
    Task<UserDto?> GetAsync(string sub);
}

public record UserDto(Guid Id, string Sub, string Email, DateTime CreatedAt);