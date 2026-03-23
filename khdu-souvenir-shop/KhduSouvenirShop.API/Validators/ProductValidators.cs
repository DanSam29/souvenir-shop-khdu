using FluentValidation;
using KhduSouvenirShop.API.Controllers;

namespace KhduSouvenirShop.API.Validators
{
    public class ProductDtoValidator : AbstractValidator<ProductDto>
    {
        public ProductDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва товару обов'язкова")
                .Length(3, 200).WithMessage("Назва має бути від 3 до 200 символів");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Опис товару обов'язковий")
                .Length(10, 5000).WithMessage("Опис має бути від 10 до 5000 символів");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Ціна має бути більше 0");

            RuleFor(x => x.Weight)
                .GreaterThan(0).WithMessage("Вага має бути більше 0");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("Необхідно вказати категорію");

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("Кількість на складі не може бути від'ємною");
        }
    }
}
