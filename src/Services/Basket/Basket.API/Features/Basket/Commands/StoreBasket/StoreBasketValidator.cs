using Basket.API.Dtos;
using FluentValidation;

namespace Basket.API.Features.Basket.Commands.StoreBasket;

public class StoreBasketValidator : AbstractValidator<StoreBasketCommand>
{
    public StoreBasketValidator()
    {
        RuleFor(x => x.Basket)
            .NotNull().WithMessage("Basket boş olamaz");

        RuleFor(x => x.Basket.UserName)
            .NotEmpty().WithMessage("UserName boş olamaz");

        RuleFor(x => x.Basket.Items)
            .NotNull().WithMessage("Items null olamaz");

        RuleForEach(x => x.Basket.Items)
            .SetValidator(new ShoppingCartItemValidator());
    }
}

public class ShoppingCartItemValidator : AbstractValidator<ShoppingCartItemDto>
{
    public ShoppingCartItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId boş olamaz");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName boş olamaz");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity 0'dan büyük olmalı");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price 0'dan büyük olmalı");
    }
}

