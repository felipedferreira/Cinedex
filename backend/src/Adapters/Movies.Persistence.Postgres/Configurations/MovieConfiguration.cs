using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Movies.Domain;

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

        builder.HasMany(m => m.Genres)
            .WithMany(g => g.Movies)
            .UsingEntity(
                "movie_genres",
                right => right.HasOne(typeof(Genre)).WithMany().HasForeignKey("genre_id"),
                left => left.HasOne(typeof(Movie)).WithMany().HasForeignKey("movie_id"));
    }
}