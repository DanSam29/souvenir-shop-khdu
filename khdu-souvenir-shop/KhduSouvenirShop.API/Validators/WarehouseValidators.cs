using FluentValidation;
using KhduSouvenirShop.API.Controllers;

namespace KhduSouvenirShop.API.Validators
{
    public class IncomingDocumentDtoValidator : AbstractValidator<IncomingDocumentDto>
    {
        public IncomingDocumentDtoValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Необхідно вказати товар");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Кількість має бути більше 0");
            RuleFor(x => x.PurchasePrice).GreaterThan(0).WithMessage("Ціна закупівлі має бути більше 0");
            RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("Необхідно вказати компанію-постачальника");
            
            RuleFor(x => x.DocumentDate)
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("Дата документа не може бути в майбутньому")
                .When(x => x.DocumentDate.HasValue);
        }
    }

    public class OutgoingDocumentDtoValidator : AbstractValidator<OutgoingDocumentDto>
    {
        public OutgoingDocumentDtoValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Необхідно вказати товар");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Кількість має бути більше 0");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Причина обов'язкова")
                .Must(x => new[] { "Damaged", "Lost", "Return", "Inventory", "Other" }.Contains(x))
                .WithMessage("Некоректна причина списання");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("Для повернення (RETURN) обов'язково вказати компанію")
                .When(x => x.Reason == "Return");
            
            RuleFor(x => x.DocumentDate)
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("Дата документа не може бути в майбутньому")
                .When(x => x.DocumentDate.HasValue);
        }
    }

    public class CompanyDtoValidator : AbstractValidator<CompanyDto>
    {
        public CompanyDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва компанії обов'язкова")
                .Length(3, 200).WithMessage("Назва має бути від 3 до 200 символів");

            RuleFor(x => x.ContactPerson)
                .MaximumLength(100).WithMessage("Контактна особа не може бути довшою за 100 символів");

            RuleFor(x => x.Phone)
                .Matches(@"^\+380\d{9}$").WithMessage("Телефон має бути у форматі +380XXXXXXXXX")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Некоректний формат Email")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Адреса не може бути довшою за 500 символів");
        }
    }
}
