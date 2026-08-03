using Cinedex.Auth.Identity.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinedex.Auth.Identity.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(AuthDatabaseConstants.RefreshToken.Table);

        builder.HasKey(token => token.Id).HasName(AuthDatabaseConstants.RefreshToken.PrimaryKey);

        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(128);

        builder.HasIndex(token => token.TokenHash)
            .HasDatabaseName(AuthDatabaseConstants.RefreshToken.TokenHashIndex)
            .IsUnique();

        builder.HasIndex(token => token.UserId)
            .HasDatabaseName(AuthDatabaseConstants.RefreshToken.UserIdIndex);

        // Not unique: a family holds every token in one login's rotation chain.
        builder.HasIndex(token => token.FamilyId)
            .HasDatabaseName(AuthDatabaseConstants.RefreshToken.FamilyIdIndex);

        // Serves the retention sweep's two predicates. RevokedAtUtc leads because it is what
        // separates them — IS NULL for expired-and-never-revoked rows, a range for revoked ones —
        // with ExpiresAtUtc narrowing within it. One composite rather than two indexes: this table
        // takes a write on every rotation, so each extra index is a tax on the hot auth path.
        // A partial index cannot help here; Postgres requires an IMMUTABLE predicate and the
        // interesting filter is against now().
        builder.HasIndex(token => new { token.RevokedAtUtc, token.ExpiresAtUtc })
            .HasDatabaseName(AuthDatabaseConstants.RefreshToken.RetentionIndex);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
