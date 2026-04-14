namespace DbmsComparison.Api.Entities;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public byte[]? ProfileImage { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}
