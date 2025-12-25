using System.Net.Http.Json;
using Web.UI.Models;

namespace Web.UI.Services;

public class BasketService : IBasketService
{
    private readonly HttpClient _httpClient;

    public BasketService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ShoppingCartDto> GetBasketAsync(string userName)
    {
        var response = await _httpClient.GetFromJsonAsync<ShoppingCartDto>($"api/baskets/{userName}");
        return response ?? new ShoppingCartDto { UserName = userName };
    }

    public async Task<ShoppingCartDto> StoreBasketAsync(ShoppingCartDto basket)
    {
        var response = await _httpClient.PostAsJsonAsync("api/baskets", basket);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShoppingCartDto>() 
            ?? throw new Exception("Failed to store basket");
    }

    public async Task<bool> CheckoutBasketAsync(CheckoutBasketCommand command)
    {
        var response = await _httpClient.PostAsJsonAsync("api/baskets/checkout", command);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> DeleteBasketAsync(string userName)
    {
        var response = await _httpClient.DeleteAsync($"api/baskets/{userName}");
        return response.IsSuccessStatusCode;
    }
}

