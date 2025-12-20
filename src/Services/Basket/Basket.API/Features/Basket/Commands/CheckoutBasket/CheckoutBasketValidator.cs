using FluentValidation;

namespace Basket.API.Features.Basket.Commands.CheckoutBasket;

public class CheckoutBasketValidator : AbstractValidator<CheckoutBasketCommand>
{
    public CheckoutBasketValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName boş olamaz");

        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithMessage("Email adresi boş olamaz")
            .EmailAddress().WithMessage("Geçerli email adresi gerekli");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Adres boş olamaz");

        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("Kart numarası boş olamaz")
            .Length(16).WithMessage("Kart numarası 16 haneli olmalı");

        RuleFor(x => x.CVV)
            .NotEmpty().WithMessage("CVV boş olamaz")
            .Length(3).WithMessage("CVV 3 haneli olmalı");
    }
}

