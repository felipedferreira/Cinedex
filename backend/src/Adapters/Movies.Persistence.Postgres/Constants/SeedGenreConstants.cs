using Movies.Domain.GenreAggregate;

namespace Movies.Persistence.Postgres.Constants;

internal static class SeedGenreConstants
{
    public const string ActionName = "Action";
    public const string ComedyName = "Comedy";
    public const string DramaName = "Drama";
    public const string FantasyName = "Fantasy";
    public const string HorrorName = "Horror";
    public const string RomanceName = "Romance";
    public const string SciFiName = "SciFi";
    public const string ThrillerName = "Thriller";
    public const string AnimationName = "Animation";
    public const string AdventureName = "Adventure";
    public const string CrimeName = "Crime";
    public const string DocumentaryName = "Documentary";
    public const string FamilyName = "Family";
    public const string HistoryName = "History";
    public const string MusicalName = "Musical";
    public const string MysteryName = "Mystery";
    public const string WesternName = "Western";

    public static readonly Genre[] All =
    [
        new() { Id = new Guid("9d6c31eb-2c02-4a48-b4dc-24767675f82a"), Name = ActionName },
        new() { Id = new Guid("d7a0f348-959f-416b-8d50-8e35a0bdbb26"), Name = ComedyName },
        new() { Id = new Guid("6deddeee-0f11-4d07-a7c2-b70229ab58de"), Name = DramaName },
        new() { Id = new Guid("90837e33-57a3-4793-a07b-5c47a7daeff8"), Name = FantasyName },
        new() { Id = new Guid("65515a68-1b05-4d8b-94e0-5599d671a643"), Name = HorrorName },
        new() { Id = new Guid("80acabbb-c0f8-428e-bd12-23187789928f"), Name = RomanceName },
        new() { Id = new Guid("655b1256-859a-4c9f-bed2-31ac669d0f18"), Name = SciFiName },
        new() { Id = new Guid("535c3954-aa92-4b97-8a75-92cd5ef6f8c6"), Name = ThrillerName },
        new() { Id = new Guid("f271eb24-ef49-4cfb-af1c-edf8c45a4a37"), Name = AnimationName },
        new() { Id = new Guid("3687d461-e5cb-447a-81f4-c82716f4feb1"), Name = AdventureName },
        new() { Id = new Guid("aef33796-2504-403c-a2ac-99ff7ce966e8"), Name = CrimeName },
        new() { Id = new Guid("00482a38-516a-4e2b-a4c6-f0f6e6259d2a"), Name = DocumentaryName },
        new() { Id = new Guid("03875139-b79e-4a8d-9053-3d937c8ae165"), Name = FamilyName },
        new() { Id = new Guid("50267304-8ba6-495b-9ef4-806d2dff1656"), Name = HistoryName },
        new() { Id = new Guid("49ef5cc8-f94a-4066-ba86-0b27aa9bd642"), Name = MusicalName },
        new() { Id = new Guid("bba55f7e-81c0-4f11-a9c9-057f5ee74a05"), Name = MysteryName },
        new() { Id = new Guid("c469256f-6f96-4602-b546-82be46807a6f"), Name = WesternName },
    ];
}
