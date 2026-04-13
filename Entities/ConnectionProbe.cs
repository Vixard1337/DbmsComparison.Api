namespace DbmsComparison.Api.Entities;

public class ConnectionProbe
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
