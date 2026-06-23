using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Movies.Domain.Genres;

namespace Movies.Persistence.Postgres.Configurations;

internal sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("genres");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(g => g.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(g => g.Name)
            .IsUnique();

        builder.Property(g => g.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        // Seed the genres that previously existed only as an enum, preserving their
        // descriptions. Fixed ids are required by HasData so the seed is stable across runs.
        builder.HasData(
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111101"), Name = "Action", Description = "Action genre featuring physical conflict and adventure." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111102"), Name = "Comedy", Description = "Comedy genre focused on humor and entertainment." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111103"), Name = "Drama", Description = "Drama genre focused on emotional storytelling." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111104"), Name = "Fantasy", Description = "Fantasy genre with magical and imaginative elements." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111105"), Name = "Horror", Description = "Horror genre designed to evoke fear." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111106"), Name = "Romance", Description = "Romance genre focused on relationships and love." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111107"), Name = "SciFi", Description = "Science fiction genre exploring futuristic or scientific concepts." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111108"), Name = "Thriller", Description = "Thriller genre designed to create suspense." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111109"), Name = "Animation", Description = "Animation genre using animated characters and storytelling." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110a"), Name = "Adventure", Description = "Adventure genre featuring exploration and quests." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110b"), Name = "Crime", Description = "Crime genre focused on criminal activities and investigations." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110c"), Name = "Documentary", Description = "Documentary genre presenting factual information." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110d"), Name = "Family", Description = "Family genre appropriate for all ages." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110e"), Name = "History", Description = "History genre based on historical events." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110f"), Name = "Musical", Description = "Musical genre featuring songs and musical performances." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111110"), Name = "Mystery", Description = "Mystery genre focused on solving puzzles or crimes." },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Western", Description = "Western genre set in the Old West." });
    }
}
