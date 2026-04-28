using FluentValidation;
using KhduSouvenirShop.API.Controllers;

namespace KhduSouvenirShop.API.Validators
{
    public class CheckoutDtoValidator : AbstractValidator<CheckoutDto>
    {
        public CheckoutDtoValidator()
        {
            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("Ім'я отримувача обов'язкове")
                .Length(2, 100).WithMessage("Ім'я має бути від 2 до 100 символів");

            RuleFor(x => x.RecipientPhone)
                .NotEmpty().WithMessage("Телефон отримувача обов'язковий")
                .Matches(@"^\+380\d{9}$").WithMessage("Телефон має бути у форматі +380XXXXXXXXX");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("Місто обов'язкове");

            RuleFor(x => x.WarehouseNumber)
                .NotEmpty().WithMessage("Номер відділення обов'язковий");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("Метод оплати обов'язковий")
                .Must(x => x == "Card" || x == "CashOnDelivery").WithMessage("Некоректний метод оплати");
            
            RuleFor(x => x.CityRef)
                .NotEmpty().WithMessage("CityRef обов'язковий для Nova Poshta");

            RuleFor(x => x.WarehouseRef)
                .NotEmpty().WithMessage("WarehouseRef обов'язковий для Nova Poshta");
        }
    }

    public class UpdateStatusDtoValidator : AbstractValidator<UpdateStatusDto>
    {
        public UpdateStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Статус обов'язковий")
                .Must(x => new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded" }.Contains(x))
                .WithMessage("Некоректний статус замовлення");

            RuleFor(x => x.Comment)
                .MaximumLength(500).WithMessage("Коментар не може бути довшим за 500 символів");
            
            RuleFor(x => x.TrackingNumber)
                .MaximumLength(50).WithMessage("Номер ТТН не може бути довшим за 50 символів");
        }
    }
}
