using Web.UI.Models;

namespace Web.UI.Services;

public interface ICatalogService
{
    Task<IEnumerable<ProductDto>> GetProductsAsync();
    Task<ProductDto> GetProductByIdAsync(Guid id);
    Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(Guid categoryId);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
}

