namespace DbmsComparison.Api.Entities;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public TimeSpan Duration { get; set; }

    public decimal TotalAmount { get; set; }

    public User User { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
