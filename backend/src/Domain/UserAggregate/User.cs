namespace Cinedex.Domain.UserAggregate;

public class User
{
    private User()
    {
        // Required by EF Core for materialization.
    }

    public Guid Id { get; init; }

    public required string Email { get; set; }

    public required string UserName { get; set; }

    public bool EmailConfirmed { get; set; }

    public static User Create(string email, string userName)
    {
        return Create(Guid.CreateVersion7(), email, userName, emailConfirmed: false);
    }

    public static User Reconstitute(Guid id, string email, string userName, bool emailConfirmed)
    {
        return Create(id, email, userName, emailConfirmed);
    }

    private static User Create(Guid id, string email, string userName, bool emailConfirmed)
    {
        return new User
        {
            Id = id,
            Email = email,
            UserName = userName,
            EmailConfirmed = emailConfirmed,
        };
    }
}
