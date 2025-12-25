using Web.UI.Models;

namespace Web.UI.Services;

public interface IBasketService
{
    Task<ShoppingCartDto> GetBasketAsync(string userName);
    Task<ShoppingCartDto> StoreBasketAsync(ShoppingCartDto basket);
    Task<bool> CheckoutBasketAsync(CheckoutBasketCommand command);
    Task<bool> DeleteBasketAsync(string userName);
}

