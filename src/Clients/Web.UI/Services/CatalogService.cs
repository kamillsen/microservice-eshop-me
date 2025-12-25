using System.Net.Http.Json;
using Web.UI.Models;

namespace Web.UI.Services;

public class CatalogService : ICatalogService
{
    private readonly HttpClient _httpClient;

    public CatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<ProductDto>>("api/products");
        return response ?? Enumerable.Empty<ProductDto>();
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id)
    {
        var response = await _httpClient.GetFromJsonAsync<ProductDto>($"api/products/{id}");
        return response ?? throw new Exception($"Product with id {id} not found");
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(Guid categoryId)
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<ProductDto>>($"api/products/category/{categoryId}");
        return response ?? Enumerable.Empty<ProductDto>();
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<CategoryDto>>("api/categories");
        return response ?? Enumerable.Empty<CategoryDto>();
    }
}

