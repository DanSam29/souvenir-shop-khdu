using FluentValidation;
using KhduSouvenirShop.API.Controllers;

namespace KhduSouvenirShop.API.Validators
{
    public class PromotionDtoValidator : AbstractValidator<PromotionDto>
    {
        public PromotionDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва акції обов'язкова")
                .Length(3, 100).WithMessage("Назва має бути від 3 до 100 символів");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Тип акції обов'язковий")
                .Must(x => new[] { "PERCENTAGE", "FIXED", "SPECIAL_PRICE" }.Contains(x))
                .WithMessage("Некоректний тип акції");

            RuleFor(x => x.Value)
                .GreaterThan(0).WithMessage("Значення акції має бути більше 0");

            RuleFor(x => x.TargetType)
                .NotEmpty().WithMessage("Тип цілі акції обов'язковий")
                .Must(x => new[] { "PRODUCT", "CATEGORY", "CART", "SHIPPING" }.Contains(x))
                .WithMessage("Некоректний тип цілі");

            RuleFor(x => x.AudienceType)
                .NotEmpty().WithMessage("Тип аудиторії обов'язковий")
                .Must(x => new[] { "ALL", "STUDENTS", "STAFF", "ALUMNI", "CUSTOM" }.Contains(x))
                .WithMessage("Некоректний тип аудиторії");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Дата початку має бути раніше або дорівнювати даті завершення")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.Priority)
                .InclusiveBetween(0, 100).WithMessage("Пріоритет має бути від 0 до 100");

            RuleFor(x => x.MinOrderAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Мінімальна сума замовлення не може бути від'ємною")
                .When(x => x.MinOrderAmount.HasValue);
            
            RuleFor(x => x.PromoCode)
                .Matches(@"^[A-Z0-9]+$").WithMessage("Промокод має складатися тільки з великих літер та цифр")
                .When(x => !string.IsNullOrEmpty(x.PromoCode));
        }
    }
}
