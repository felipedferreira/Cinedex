using Cinedex.Domain.TitleAggregate;
using Cinedex.Persistence.Postgres.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinedex.Persistence.Postgres.Configurations;

internal sealed class TitleConfiguration : IEntityTypeConfiguration<Title>
{
    public void Configure(EntityTypeBuilder<Title> builder)
    {
        builder.HasKey(m => m.Id)
            .HasName(DatabaseConstants.Title.PrimaryKey);

        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.Name)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.Type)
            .HasColumnName("titleType")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.YearOfRelease)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(2000);

        builder.Ignore(m => m.GenreIds);
    }
}