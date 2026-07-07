using Cinedex.Domain.UserAggregate;
using Cinedex.Persistence.Auth.Identity.Entities;

namespace Cinedex.Persistence.Auth.Identity.Services;

internal static class UserMappings
{
    public static User ToDomainUser(this ApplicationUser user) =>
        User.Reconstitute(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            user.EmailConfirmed);
}
