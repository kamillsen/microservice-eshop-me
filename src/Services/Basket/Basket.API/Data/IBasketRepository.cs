using Basket.API.Entities;

namespace Basket.API.Data;

public interface IBasketRepository
{
    Task<ShoppingCart?> GetBasket(string userName);
    Task<ShoppingCart> SaveBasket(ShoppingCart basket);
    Task<bool> DeleteBasket(string userName);
}