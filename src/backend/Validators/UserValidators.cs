using FluentValidation;
using KhduSouvenirShop.API.Controllers;
using KhduSouvenirShop.API.DTOs.Requests;

namespace KhduSouvenirShop.API.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
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
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Підтвердження паролю обов'язкове")
                .Equal(x => x.Password).WithMessage("Паролі не співпадають");
            RuleFor(x => x.Phone)
                .Matches(@"^\+380\d{9}$").WithMessage("Телефон має бути у форматі +380XXXXXXXXX")
                .When(x => !string.IsNullOrEmpty(x.Phone));
        }
    }
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обов'язковий")
                .EmailAddress().WithMessage("Некоректний формат Email");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обов'язковий");
        }
    }
    public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
    {
        public ForgotPasswordRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обов'язковий")
                .EmailAddress().WithMessage("Некоректний формат Email");
        }
    }
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Token).NotEmpty().WithMessage("Токен скидання обов'язковий");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Новий пароль обов'язковий")
                .MinimumLength(8).WithMessage("Новий пароль має бути не менше 8 символів")
                .Matches(@"[A-Z]").WithMessage("Новий пароль має містити хоча б одну велику літеру")
                .Matches(@"[a-z]").WithMessage("Новий пароль має містити хоча б одну малу літеру")
                .Matches(@"[0-9]").WithMessage("Новий пароль має містити хоча б одну цифру");
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Підтвердження паролю обов'язкове")
                .Equal(x => x.NewPassword).WithMessage("Паролі не співпадають");
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
    public class AddressRequestValidator : AbstractValidator<AddressRequest>
    {
        public AddressRequestValidator()
        {
            RuleFor(x => x.City)
                .NotEmpty().WithMessage("Місто обов'язкове")
                .MaximumLength(100).WithMessage("Місто не може бути довшим за 100 символів");
            RuleFor(x => x.WarehouseNumber)
                .NotEmpty().WithMessage("Номер відділення обов'язковий")
                .MaximumLength(500).WithMessage("Номер відділення не може бути довшим за 500 символів");
            RuleFor(x => x.WarehouseAddress)
                .MaximumLength(500).WithMessage("Адреса відділення не може бути довшою за 500 символів");
            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("Ім'я отримувача обов'язкове")
                .MaximumLength(100).WithMessage("Ім'я отримувача не може бути довшим за 100 символів");
            RuleFor(x => x.RecipientPhone)
                .NotEmpty().WithMessage("Телефон отримувача обов'язковий")
                .Matches(@"^\+380\d{9}$").WithMessage("Телефон має бути у форматі +380XXXXXXXXX");
        }
    }
    public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
    {
        private static readonly string[] AllowedRoles = { "Customer", "Manager", "Administrator", "SuperAdmin" };
        public UpdateRoleRequestValidator()
        {
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Роль обов'язкова")
                .Must(r => AllowedRoles.Contains(r)).WithMessage("Неприпустиме значення ролі");
        }
    }
    public class AdminResetPasswordRequestValidator : AbstractValidator<AdminResetPasswordRequest>
    {
        public AdminResetPasswordRequestValidator()
        {
            RuleFor(x => x.NewPassword)
                .MinimumLength(8).WithMessage("Пароль має бути не менше 8 символів")
                .Matches(@"[A-Z]").WithMessage("Пароль має містити хоча б одну велику літеру")
                .Matches(@"[a-z]").WithMessage("Пароль має містити хоча б одну малу літеру")
                .Matches(@"[0-9]").WithMessage("Пароль має містити хоча б одну цифру")
                .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
        }
    }
}
