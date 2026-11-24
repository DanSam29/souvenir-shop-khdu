using FluentValidation;
using KhduSouvenirShop.API.Controllers;
using KhduSouvenirShop.API.DTOs.Requests;
using KhduSouvenirShop.API.Services;

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
                .NotEmpty().WithMessage("Місто обов'язкове")
                .MaximumLength(100).WithMessage("Місто не може бути довшим за 100 символів");
            RuleFor(x => x.WarehouseNumber)
                .NotEmpty().WithMessage("Номер відділення обов'язковий")
                .MaximumLength(500).WithMessage("Номер відділення не може бути довшим за 500 символів");
            RuleFor(x => x.WarehouseAddress)
                .MaximumLength(500).WithMessage("Адреса відділення не може бути довшою за 500 символів");
            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("Метод оплати обов'язковий")
                .MaximumLength(50).WithMessage("Метод оплати не може бути довшим за 50 символів")
                .Must(x => PaymentStates.IsKnownMethod(x)).WithMessage("Некоректний метод оплати");           
            RuleFor(x => x.CityRef)
                .NotEmpty().WithMessage("CityRef обов'язковий для Nova Poshta")
                .MaximumLength(100).WithMessage("CityRef не може бути довшим за 100 символів");
            RuleFor(x => x.WarehouseRef)
                .NotEmpty().WithMessage("WarehouseRef обов'язковий для Nova Poshta")
                .MaximumLength(100).WithMessage("WarehouseRef не може бути довшим за 100 символів");
            RuleFor(x => x.PromoCode)
                .MaximumLength(50).WithMessage("Промокод не може бути довшим за 50 символів")
                .Matches(@"^[A-Za-z0-9]+$").WithMessage("Промокод може містити лише латинські літери та цифри")
                .When(x => !string.IsNullOrEmpty(x.PromoCode));
        }
    }
    public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
    {
        public UpdateOrderStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Статус обов'язковий")
                .Must(x => OrderStateMachine.IsKnownStatus(x))
                .WithMessage("Некоректний статус замовлення");
            RuleFor(x => x.Comment)
                .MaximumLength(500).WithMessage("Коментар не може бути довшим за 500 символів");
            RuleFor(x => x.TrackingNumber)
                .MaximumLength(100).WithMessage("Номер ТТН не може бути довшим за 100 символів");
        }
    }
    public class CalculateDtoValidator : AbstractValidator<CalculateDto>
    {
        public CalculateDtoValidator()
        {
            RuleFor(x => x.PromoCode)
                .MaximumLength(50).WithMessage("Промокод не може бути довшим за 50 символів")
                .Matches(@"^[A-Za-z0-9]+$").WithMessage("Промокод може містити лише латинські літери та цифри")
                .When(x => !string.IsNullOrEmpty(x.PromoCode));
            RuleFor(x => x.CityRef)
                .MaximumLength(100).WithMessage("CityRef не може бути довшим за 100 символів");
        }
    }
    public class CancelOrderDtoValidator : AbstractValidator<CancelOrderDto>
    {
        public CancelOrderDtoValidator()
        {
            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Причина скасування не може бути довшою за 500 символів");
        }
    }
}