namespace Cinedex.Domain.UserAggregate;

public class User
{
    private User()
    {
        // Private so callers cannot bypass the factory with an object initializer.
    }

    public Guid Id { get; init; }

    public required string Email { get; set; }

    public required string UserName { get; set; }

    public bool EmailConfirmed { get; set; }

    // Rebuilds an existing user from stored state. Users are created by ASP.NET Core Identity in the
    // auth adapter rather than by this aggregate, so there is deliberately no Create factory.
    public static User Reconstitute(Guid id, string email, string userName, bool emailConfirmed)
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