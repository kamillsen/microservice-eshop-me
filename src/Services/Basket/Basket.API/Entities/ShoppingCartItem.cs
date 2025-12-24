namespace Basket.API.Entities;

public class ShoppingCartItem
{
    public Guid Id { get; set; }
    public Guid ShoppingCartId { get; set; }
    // Navigation property yok - Referans projeye göre best practice (döngüsel referans sorunu olmaz)
    
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

