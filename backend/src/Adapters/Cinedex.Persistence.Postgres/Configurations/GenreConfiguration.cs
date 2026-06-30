using Cinedex.Domain.GenreAggregate;
using Cinedex.Persistence.Postgres.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinedex.Persistence.Postgres.Configurations;

internal sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.HasKey(g => g.Id)
            .HasName(DatabaseConstants.Genre.PrimaryKey);

        builder.Property(g => g.Id)
            .ValueGeneratedOnAdd();

        builder.Property(g => g.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(g => g.Name)
            .HasDatabaseName(DatabaseConstants.Genre.NameIndex)
            .IsUnique();

        // Seed the genres. Fixed ids are required by HasData so the seed is stable across runs.
        builder.HasData(SeedGenreConstants.All);
    }
}