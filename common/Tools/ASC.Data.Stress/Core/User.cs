namespace ASC.Data.Stress.Core;

public record User(string Email, string Password)
{
    public Guid Id { get; init; }
    public string? PasswordHash { get; set; }
}
