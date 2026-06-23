using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Movies.Domain.GenreAggregate;

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

        // Seed the genres. Fixed ids are required by HasData so the seed is stable across runs.
        builder.HasData(
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111101"), Name = "Action" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111102"), Name = "Comedy" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111103"), Name = "Drama" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111104"), Name = "Fantasy" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111105"), Name = "Horror" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111106"), Name = "Romance" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111107"), Name = "SciFi" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111108"), Name = "Thriller" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111109"), Name = "Animation" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110a"), Name = "Adventure" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110b"), Name = "Crime" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110c"), Name = "Documentary" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110d"), Name = "Family" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110e"), Name = "History" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-11111111110f"), Name = "Musical" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111110"), Name = "Mystery" },
            new Genre { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Western" });
    }
}