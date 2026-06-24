using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Movies.Domain.TitleAggregate;
using Movies.Persistence.Postgres.Constants;

namespace Movies.Persistence.Postgres.Configurations;

internal sealed class TitleConfiguration : IEntityTypeConfiguration<Title>
{
    public void Configure(EntityTypeBuilder<Title> builder)
    {
        builder.HasKey(m => m.Id)
            .HasName(DatabaseConstants.Title.PrimaryKey);

        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.Name)
            .HasColumnName(DatabaseConstants.Title.Columns.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.Type)
            .HasColumnName(DatabaseConstants.Title.Columns.TitleType)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.YearOfRelease)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(2000);

        // Genre is a separate aggregate, referenced by identity only. The ids are stored as a
        // Postgres uuid[] column on the titles table; no join table, no cross-aggregate FK.
        builder.Property(m => m.GenreIds)
            .HasColumnType(DatabaseConstants.PostgresTypes.UuidArray);
    }
}
