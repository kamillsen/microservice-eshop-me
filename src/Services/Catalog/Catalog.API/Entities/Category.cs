namespace Catalog.API.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Navigation Property
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

