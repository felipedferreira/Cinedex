using Microsoft.AspNetCore.Identity;

namespace Cinedex.Auth.Identity.Constants;

// Seed data for the three RBAC roles. The Ids and ConcurrencyStamps are fixed literals rather than
// generated at runtime so re-running migrations produces the same rows and does not create diffs.
internal static class SeedRoleConstants
{
    public static readonly IdentityRole<Guid>[] All =
    [
        new()
        {
            Id = new Guid("a5f0c1a0-1000-7000-8000-000000000001"),
            Name = RoleNames.User,
            NormalizedName = "USER",
            ConcurrencyStamp = "f6b1c2a1-2000-7000-8000-000000000001",
        },
        new()
        {
            Id = new Guid("a5f0c1a0-1000-7000-8000-000000000002"),
            Name = RoleNames.Moderator,
            NormalizedName = "MODERATOR",
            ConcurrencyStamp = "f6b1c2a1-2000-7000-8000-000000000002",
        },
        new()
        {
            Id = new Guid("a5f0c1a0-1000-7000-8000-000000000003"),
            Name = RoleNames.Administrator,
            NormalizedName = "ADMINISTRATOR",
            ConcurrencyStamp = "f6b1c2a1-2000-7000-8000-000000000003",
        },
    ];
}