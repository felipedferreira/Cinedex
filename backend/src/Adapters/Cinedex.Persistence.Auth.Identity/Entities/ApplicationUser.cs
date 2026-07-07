using Microsoft.AspNetCore.Identity;

namespace Cinedex.Persistence.Auth.Identity.Entities;

// The ASP.NET Core Identity user entity, keyed by Guid to match the solution's Guid v7 convention.
internal sealed class ApplicationUser : IdentityUser<Guid>
{
}
