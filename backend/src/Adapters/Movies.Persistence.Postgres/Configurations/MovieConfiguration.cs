using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Movies.Domain.MovieAggregate;

namespace Movies.Persistence.Postgres.Configurations;

internal sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("movies");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.YearOfRelease)
            .HasColumnName("year_of_release")
            .IsRequired();

        builder.Property(m => m.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        // Genre is a separate aggregate, referenced by identity only. The ids are stored as a
        // Postgres uuid[] column on the movies table — no join table, no cross-aggregate FK.
        builder.Property(m => m.GenreIds)
            .HasColumnName("genre_ids")
            .HasColumnType("uuid[]");
    }
}