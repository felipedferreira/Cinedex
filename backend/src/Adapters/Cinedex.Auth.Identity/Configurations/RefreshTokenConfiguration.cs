using Cinedex.Auth.Identity.Constants;
using Cinedex.Auth.Identity.Entities;
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

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}