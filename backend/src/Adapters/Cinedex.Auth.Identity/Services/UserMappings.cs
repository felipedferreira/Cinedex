using Cinedex.Auth.Identity.Entities;
using Cinedex.Domain.UserAggregate;

namespace Cinedex.Auth.Identity.Services;

internal static class UserMappings
{
    public static User ToDomainUser(this ApplicationUser user) =>
        User.Reconstitute(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            user.EmailConfirmed);
}
