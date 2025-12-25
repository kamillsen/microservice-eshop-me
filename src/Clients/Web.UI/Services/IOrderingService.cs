using Web.UI.Models;

namespace Web.UI.Services;

public interface IOrderingService
{
    Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(string userName);
    Task<OrderDto> GetOrderByIdAsync(Guid id);
}

