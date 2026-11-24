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
            RuleFor(x => x.NameEn)
                .MaximumLength(200).WithMessage("Англійська назва не може бути довшою за 200 символів");
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Опис товару обов'язковий")
                .Length(10, 5000).WithMessage("Опис має бути від 10 до 5000 символів");
            RuleFor(x => x.DescriptionEn)
                .MaximumLength(5000).WithMessage("Англійський опис не може бути довшим за 5000 символів");
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
    public class AdminCategoryDtoValidator : AbstractValidator<AdminCategoryDto>
    {
        public AdminCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва категорії обов'язкова")
                .MaximumLength(100).WithMessage("Назва категорії не може бути довшою за 100 символів");
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Опис категорії не може бути довшим за 500 символів");
        }
    }
}