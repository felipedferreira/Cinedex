namespace FoundryOceanus.Persistence.Tests.Fakes;

/// <summary>
/// A row with a unique name, so tests have something to duplicate.
/// </summary>
public sealed class Widget
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
