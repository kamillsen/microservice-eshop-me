namespace Basket.API.Dtos;

public class ShoppingCartDto
{
    public string UserName { get; set; } = default!;
    public List<ShoppingCartItemDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public decimal Discount { get; set; }
}



