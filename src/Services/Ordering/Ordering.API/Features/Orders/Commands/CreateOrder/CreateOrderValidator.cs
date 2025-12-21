using FluentValidation;

namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName boş olamaz");

        RuleFor(x => x.TotalPrice)
            .GreaterThan(0).WithMessage("TotalPrice 0'dan büyük olmalı");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items null olamaz");
            // .NotEmpty() kaldırıldı - Event'ten gelen siparişler için Items boş olabilir (gelecekte BasketCheckoutEvent'e Items eklenecek)

        RuleFor(x => x.EmailAddress)
            .NotEmpty().EmailAddress().WithMessage("Geçerli email adresi gerekli");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Adres boş olamaz");
    }
}

