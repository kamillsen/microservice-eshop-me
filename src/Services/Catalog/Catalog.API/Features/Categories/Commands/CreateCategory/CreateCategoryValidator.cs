using FluentValidation;

namespace Catalog.API.Features.Categories.Commands.CreateCategory;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı boş olamaz")
            .MaximumLength(50).WithMessage("Kategori adı en fazla 50 karakter olabilir");
    }
}

