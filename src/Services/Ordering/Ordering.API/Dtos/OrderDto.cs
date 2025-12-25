namespace Ordering.API.Dtos;

public class OrderDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public decimal Discount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = default!;
    public List<OrderItemDto> Items { get; set; } = new();
}

