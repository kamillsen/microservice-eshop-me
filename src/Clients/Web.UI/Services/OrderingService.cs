using System.Net.Http.Json;
using Web.UI.Models;

namespace Web.UI.Services;

public class OrderingService : IOrderingService
{
    private readonly HttpClient _httpClient;

    public OrderingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(string userName)
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<OrderDto>>($"api/orders/user/{userName}");
        return response ?? Enumerable.Empty<OrderDto>();
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid id)
    {
        var response = await _httpClient.GetFromJsonAsync<OrderDto>($"api/orders/{id}");
        return response ?? throw new Exception($"Order with id {id} not found");
    }
}

