using FluentValidation;
using KhduSouvenirShop.API.Controllers;

namespace KhduSouvenirShop.API.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ім'я обов'язкове")
                .Length(2, 50).WithMessage("Ім'я має бути від 2 до 50 символів");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Прізвище обов'язкове")
                .Length(2, 50).WithMessage("Прізвище має бути від 2 до 50 символів");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обов'язковий")
                .EmailAddress().WithMessage("Некоректний формат Email")
                .MaximumLength(100).WithMessage("Email не може бути довшим за 100 символів");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обов'язковий")
                .MinimumLength(8).WithMessage("Пароль має бути не менше 8 символів")
                .Matches(@"[A-Z]").WithMessage("Пароль має містити хоча б одну велику літеру")
                .Matches(@"[a-z]").WithMessage("Пароль має містити хоча б одну малу літеру")
                .Matches(@"[0-9]").WithMessage("Пароль має містити хоча б одну цифру");

            RuleFor(x => x.Phone)
                .Matches(@"^\+380\d{9}$").WithMessage("Телефон має бути у форматі +380XXXXXXXXX")
                .When(x => !string.IsNullOrEmpty(x.Phone));
        }
    }

    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обов'язковий")
                .EmailAddress().WithMessage("Некоректний формат Email");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обов'язковий");
        }
    }

    public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
    {
        public UserUpdateDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ім'я обов'язкове")
                .Length(2, 50).WithMessage("Ім'я має бути від 2 до 50 символів");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Прізвище обов'язкове")
                .Length(2, 50).WithMessage("Прізвище має бути від 2 до 50 символів");

            RuleFor(x => x.Phone)
                .Matches(@"^\+380\d{9}$").WithMessage("Телефон має бути у форматі +380XXXXXXXXX")
                .When(x => !string.IsNullOrEmpty(x.Phone));
        }
    }

    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.OldPassword).NotEmpty().WithMessage("Старий пароль обов'язковий");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Новий пароль обов'язковий")
                .MinimumLength(8).WithMessage("Новий пароль має бути не менше 8 символів")
                .Matches(@"[A-Z]").WithMessage("Новий пароль має містити хоча б одну велику літеру")
                .Matches(@"[a-z]").WithMessage("Новий пароль має містити хоча б одну малу літеру")
                .Matches(@"[0-9]").WithMessage("Новий пароль має містити хоча б одну цифру")
                .NotEqual(x => x.OldPassword).WithMessage("Новий пароль не може співпадати зі старим");
        }
    }
}
