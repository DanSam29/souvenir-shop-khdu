using System;
using FluentValidation.TestHelper;
using KhduSouvenirShop.API.Controllers;
using KhduSouvenirShop.API.DTOs.Requests;
using KhduSouvenirShop.API.Validators;
using Xunit;

namespace KhduSouvenirShop.API.Tests;

public class CheckoutDtoValidatorTests
{
    private static CheckoutDto Valid() => new()
    {
        RecipientName = "Ivan Petrenko",
        RecipientPhone = "+380501112233",
        City = "Kherson",
        WarehouseNumber = "1",
        PaymentMethod = "Card",
        CityRef = "city-ref",
        WarehouseRef = "warehouse-ref"
    };

    [Fact]
    public void Valid_Checkout_Passes()
    {
        new CheckoutDtoValidator().TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyRecipientName_Fails()
    {
        var dto = Valid();
        dto.RecipientName = "";
        new CheckoutDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.RecipientName);
    }

    [Fact]
    public void BadPhoneFormat_Fails()
    {
        var dto = Valid();
        dto.RecipientPhone = "0501112233";
        new CheckoutDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.RecipientPhone);
    }

    [Fact]
    public void UnknownPaymentMethod_Fails()
    {
        var dto = Valid();
        dto.PaymentMethod = "Bitcoin";
        new CheckoutDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Fact]
    public void MissingNovaPoshtaRefs_Fails()
    {
        var dto = Valid();
        dto.CityRef = "";
        dto.WarehouseRef = "";
        var result = new CheckoutDtoValidator().TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CityRef);
        result.ShouldHaveValidationErrorFor(x => x.WarehouseRef);
    }
}

public class UpdateOrderStatusRequestValidatorTests
{
    [Fact]
    public void ValidStatus_Passes()
    {
        var dto = new UpdateOrderStatusRequest { Status = "Shipped", Comment = "ok", TrackingNumber = "TTN123" };
        new UpdateOrderStatusRequestValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UnknownStatus_Fails()
    {
        var dto = new UpdateOrderStatusRequest { Status = "Done" };
        new UpdateOrderStatusRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void TooLongComment_Fails()
    {
        var dto = new UpdateOrderStatusRequest { Status = "Shipped", Comment = new string('a', 501) };
        new UpdateOrderStatusRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Comment);
    }
}

public class RegisterRequestValidatorTests
{
    private static RegisterRequest Valid() => new()
    {
        FirstName = "Ivan",
        LastName = "Petrenko",
        Email = "ivan@example.com",
        Password = "Passw0rd",
        ConfirmPassword = "Passw0rd",
        Phone = "+380501112233"
    };

    [Fact]
    public void Valid_Register_Passes()
    {
        new RegisterRequestValidator().TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShortPassword_Fails()
    {
        var dto = Valid();
        dto.Password = "Pa0";
        dto.ConfirmPassword = "Pa0";
        new RegisterRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PasswordWithoutUppercase_Fails()
    {
        var dto = Valid();
        dto.Password = "passw0rd";
        dto.ConfirmPassword = "passw0rd";
        new RegisterRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PasswordWithoutDigit_Fails()
    {
        var dto = Valid();
        dto.Password = "Password";
        dto.ConfirmPassword = "Password";
        new RegisterRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void MismatchedConfirmation_Fails()
    {
        var dto = Valid();
        dto.ConfirmPassword = "Different1";
        new RegisterRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void BadEmail_Fails()
    {
        var dto = Valid();
        dto.Email = "not-an-email";
        new RegisterRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }
}

public class LoginRequestValidatorTests
{
    [Fact]
    public void Valid_Login_Passes()
    {
        new LoginRequestValidator().TestValidate(new LoginRequest { Email = "a@b.com", Password = "x" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyFields_Fail()
    {
        var result = new LoginRequestValidator().TestValidate(new LoginRequest { Email = "", Password = "" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

public class PasswordValidatorsTests
{
    [Fact]
    public void ForgotPassword_ValidEmail_Passes()
    {
        new ForgotPasswordRequestValidator().TestValidate(new ForgotPasswordRequest { Email = "a@b.com" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ResetPassword_Valid_Passes()
    {
        var dto = new ResetPasswordRequest { Token = "t", NewPassword = "Passw0rd", ConfirmPassword = "Passw0rd" };
        new ResetPasswordRequestValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ResetPassword_WeakPassword_Fails()
    {
        var dto = new ResetPasswordRequest { Token = "t", NewPassword = "weak", ConfirmPassword = "weak" };
        new ResetPasswordRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ChangePassword_SameAsOld_Fails()
    {
        var dto = new ChangePasswordDto { OldPassword = "Passw0rd", NewPassword = "Passw0rd" };
        new ChangePasswordDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void AdminReset_EmptyPassword_IsAllowed()
    {
        new AdminResetPasswordRequestValidator().TestValidate(new AdminResetPasswordRequest { NewPassword = null })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AdminReset_WeakPassword_Fails()
    {
        new AdminResetPasswordRequestValidator().TestValidate(new AdminResetPasswordRequest { NewPassword = "weak" })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}

public class UserValidatorsTests
{
    [Fact]
    public void UserUpdate_Valid_Passes()
    {
        var dto = new UserUpdateDto { FirstName = "Ivan", LastName = "Petrenko", Phone = "+380501112233" };
        new UserUpdateDtoValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserUpdate_BadPhone_Fails()
    {
        var dto = new UserUpdateDto { FirstName = "Ivan", LastName = "Petrenko", Phone = "123" };
        new UserUpdateDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Address_Valid_Passes()
    {
        var dto = new AddressRequest
        {
            City = "Kherson",
            WarehouseNumber = "1",
            RecipientName = "Ivan",
            RecipientPhone = "+380501112233"
        };
        new AddressRequestValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateRole_InvalidRole_Fails()
    {
        new UpdateRoleRequestValidator().TestValidate(new UpdateRoleRequest { Role = "Root" })
            .ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void UpdateRole_KnownRole_Passes()
    {
        new UpdateRoleRequestValidator().TestValidate(new UpdateRoleRequest { Role = "Manager" })
            .ShouldNotHaveAnyValidationErrors();
    }
}

public class PromotionDtoValidatorTests
{
    private static PromotionDto Valid() => new()
    {
        Name = "Spring Sale",
        Type = "PERCENTAGE",
        Value = 10,
        TargetType = "PRODUCT",
        AudienceType = "ALL",
        Priority = 50,
        PromoCode = "SAVE10"
    };

    [Fact]
    public void Valid_Promotion_Passes()
    {
        new PromotionDtoValidator().TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvalidType_Fails()
    {
        var dto = Valid();
        dto.Type = "BOGO";
        new PromotionDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void InvalidAudience_Fails()
    {
        var dto = Valid();
        dto.AudienceType = "ALIENS";
        new PromotionDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.AudienceType);
    }

    [Fact]
    public void NonPositiveValue_Fails()
    {
        var dto = Valid();
        dto.Value = 0;
        new PromotionDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void LowercasePromoCode_Fails()
    {
        var dto = Valid();
        dto.PromoCode = "save10";
        new PromotionDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PromoCode);
    }

    [Fact]
    public void PriorityOutOfRange_Fails()
    {
        var dto = Valid();
        dto.Priority = 101;
        new PromotionDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void StartAfterEnd_Fails()
    {
        var dto = Valid();
        dto.StartDate = DateTime.UtcNow.AddDays(5);
        dto.EndDate = DateTime.UtcNow.AddDays(1);
        new PromotionDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.StartDate);
    }
}

public class WarehouseValidatorsTests
{
    [Fact]
    public void Incoming_Valid_Passes()
    {
        var dto = new IncomingDocumentDto
        {
            ProductId = 1,
            Quantity = 10,
            PurchasePrice = 50,
            Reason = "INCOMING",
            CompanyId = 3
        };
        new IncomingDocumentDtoValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Incoming_WithoutCompany_Fails()
    {
        var dto = new IncomingDocumentDto
        {
            ProductId = 1,
            Quantity = 10,
            PurchasePrice = 50,
            Reason = "INCOMING",
            CompanyId = null
        };
        new IncomingDocumentDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.CompanyId);
    }

    [Fact]
    public void Incoming_ReturnWithoutCompany_Passes()
    {
        var dto = new IncomingDocumentDto
        {
            ProductId = 1,
            Quantity = 10,
            PurchasePrice = 50,
            Reason = "RETURN",
            CompanyId = null
        };
        new IncomingDocumentDtoValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Incoming_InvalidReason_Fails()
    {
        var dto = new IncomingDocumentDto
        {
            ProductId = 1,
            Quantity = 10,
            PurchasePrice = 50,
            Reason = "GIFT",
            CompanyId = 1
        };
        new IncomingDocumentDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Incoming_NonPositiveQuantity_Fails()
    {
        var dto = new IncomingDocumentDto
        {
            ProductId = 1,
            Quantity = 0,
            PurchasePrice = 50,
            Reason = "INCOMING",
            CompanyId = 1
        };
        new IncomingDocumentDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Outgoing_ReturnWithoutCompany_Fails()
    {
        var dto = new OutgoingDocumentDto { ProductId = 1, Quantity = 5, Reason = "Return", CompanyId = null };
        new OutgoingDocumentDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.CompanyId);
    }

    [Fact]
    public void Outgoing_ValidDamage_Passes()
    {
        var dto = new OutgoingDocumentDto { ProductId = 1, Quantity = 5, Reason = "Damaged" };
        new OutgoingDocumentDtoValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Outgoing_InvalidReason_Fails()
    {
        var dto = new OutgoingDocumentDto { ProductId = 1, Quantity = 5, Reason = "Order" };
        new OutgoingDocumentDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Company_Valid_Passes()
    {
        var dto = new CompanyDto { Name = "TOV Postachalnyk", Phone = "+380501112233", Email = "a@b.com" };
        new CompanyDtoValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Company_BadPhoneAndEmail_Fail()
    {
        var dto = new CompanyDto { Name = "TOV Postachalnyk", Phone = "123", Email = "bad" };
        var result = new CompanyDtoValidator().TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Phone);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Company_ShortName_Fails()
    {
        new CompanyDtoValidator().TestValidate(new CompanyDto { Name = "AB" })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}

public class ProductDtoValidatorTests
{
    private static ProductDto Valid() => new()
    {
        Name = "Souvenir Cup",
        Description = "A nice souvenir cup for students",
        Price = 150,
        Weight = 0.25m,
        CategoryId = 1,
        Stock = 10
    };

    [Fact]
    public void Valid_Product_Passes()
    {
        new ProductDtoValidator().TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShortName_Fails()
    {
        var dto = Valid();
        dto.Name = "AB";
        new ProductDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NonPositivePrice_Fails()
    {
        var dto = Valid();
        dto.Price = 0;
        new ProductDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void NonPositiveWeight_Fails()
    {
        var dto = Valid();
        dto.Weight = 0;
        new ProductDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Weight);
    }

    [Fact]
    public void NegativeStock_Fails()
    {
        var dto = Valid();
        dto.Stock = -1;
        new ProductDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Stock);
    }

    [Fact]
    public void MissingCategory_Fails()
    {
        var dto = Valid();
        dto.CategoryId = 0;
        new ProductDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.CategoryId);
    }
}

public class LengthLimitValidatorTests
{
    [Fact]
    public void Login_TooLongPassword_Fails()
    {
        var dto = new LoginRequest { Email = "a@b.com", Password = new string('a', 129) };
        new LoginRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Login_TooLongEmail_Fails()
    {
        var email = new string('a', 95) + "@b.com";
        var dto = new LoginRequest { Email = email, Password = "x" };
        new LoginRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ResetPassword_TooLongToken_Fails()
    {
        var dto = new ResetPasswordRequest
        {
            Token = new string('t', 201),
            NewPassword = "Passw0rd",
            ConfirmPassword = "Passw0rd"
        };
        new ResetPasswordRequestValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void ChangePassword_TooLongOldPassword_Fails()
    {
        var dto = new ChangePasswordDto { OldPassword = new string('a', 129), NewPassword = "Passw0rd" };
        new ChangePasswordDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.OldPassword);
    }

    [Fact]
    public void Checkout_TooLongCity_Fails()
    {
        var dto = new CheckoutDto
        {
            RecipientName = "Ivan Petrenko",
            RecipientPhone = "+380501112233",
            City = new string('c', 101),
            WarehouseNumber = "1",
            PaymentMethod = "Card",
            CityRef = "city-ref",
            WarehouseRef = "warehouse-ref"
        };
        new CheckoutDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void Checkout_BadPromoCodeChars_Fails()
    {
        var dto = new CheckoutDto
        {
            RecipientName = "Ivan Petrenko",
            RecipientPhone = "+380501112233",
            City = "Kherson",
            WarehouseNumber = "1",
            PaymentMethod = "Card",
            CityRef = "city-ref",
            WarehouseRef = "warehouse-ref",
            PromoCode = "SAVE 10!"
        };
        new CheckoutDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PromoCode);
    }

    [Fact]
    public void Checkout_LowercasePromoCode_Passes()
    {
        var dto = new CheckoutDto
        {
            RecipientName = "Ivan Petrenko",
            RecipientPhone = "+380501112233",
            City = "Kherson",
            WarehouseNumber = "1",
            PaymentMethod = "Card",
            CityRef = "city-ref",
            WarehouseRef = "warehouse-ref",
            PromoCode = "save10"
        };
        new CheckoutDtoValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Promotion_TooLongDescription_Fails()
    {
        var dto = new PromotionDto
        {
            Name = "Spring Sale",
            Description = new string('d', 501),
            Type = "PERCENTAGE",
            Value = 10,
            TargetType = "PRODUCT",
            AudienceType = "ALL"
        };
        new PromotionDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Promotion_TooLongPromoCode_Fails()
    {
        var dto = new PromotionDto
        {
            Name = "Spring Sale",
            Type = "PERCENTAGE",
            Value = 10,
            TargetType = "PRODUCT",
            AudienceType = "ALL",
            PromoCode = new string('A', 51)
        };
        new PromotionDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PromoCode);
    }

    [Fact]
    public void Product_TooLongEnglishName_Fails()
    {
        var dto = new ProductDto
        {
            Name = "Souvenir Cup",
            NameEn = new string('a', 201),
            Description = "A nice souvenir cup for students",
            Price = 150,
            Weight = 0.25m,
            CategoryId = 1
        };
        new ProductDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.NameEn);
    }

    [Fact]
    public void Company_TooLongNotes_Fails()
    {
        var dto = new CompanyDto { Name = "TOV Postachalnyk", Notes = new string('n', 1001) };
        new CompanyDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Incoming_TooLongNotes_Fails()
    {
        var dto = new IncomingDocumentDto
        {
            ProductId = 1,
            Quantity = 10,
            PurchasePrice = 50,
            Reason = "INCOMING",
            CompanyId = 1,
            Notes = new string('n', 1001)
        };
        new IncomingDocumentDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Notes);
    }
}

public class OrdersMiscValidatorTests
{
    [Fact]
    public void Calculate_ValidPromoCode_Passes()
    {
        var dto = new CalculateDto { PromoCode = "SAVE10", CityRef = "city-ref" };
        new CalculateDtoValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Calculate_BadPromoCode_Fails()
    {
        var dto = new CalculateDto { PromoCode = "save 10" };
        new CalculateDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PromoCode);
    }

    [Fact]
    public void Calculate_TooLongCityRef_Fails()
    {
        var dto = new CalculateDto { CityRef = new string('c', 101) };
        new CalculateDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.CityRef);
    }

    [Fact]
    public void CancelOrder_TooLongReason_Fails()
    {
        var dto = new CancelOrderDto { Reason = new string('r', 501) };
        new CancelOrderDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void CancelOrder_EmptyReason_Passes()
    {
        new CancelOrderDtoValidator().TestValidate(new CancelOrderDto { Reason = null })
            .ShouldNotHaveAnyValidationErrors();
    }
}

public class AdminCategoryDtoValidatorTests
{
    [Fact]
    public void Valid_Category_Passes()
    {
        var dto = new AdminCategoryDto { Name = "Souvenirs", Description = "All souvenirs" };
        new AdminCategoryDtoValidator().TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_Fails()
    {
        new AdminCategoryDtoValidator().TestValidate(new AdminCategoryDto { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void TooLongName_Fails()
    {
        new AdminCategoryDtoValidator().TestValidate(new AdminCategoryDto { Name = new string('n', 101) })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void TooLongDescription_Fails()
    {
        var dto = new AdminCategoryDto { Name = "Souvenirs", Description = new string('d', 501) };
        new AdminCategoryDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Description);
    }
}

public class ValidatePromoCodeDtoValidatorTests
{
    [Fact]
    public void Valid_Code_Passes()
    {
        new ValidatePromoCodeDtoValidator().TestValidate(new ValidatePromoCodeDto { Code = "SAVE10" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCode_Fails()
    {
        new ValidatePromoCodeDtoValidator().TestValidate(new ValidatePromoCodeDto { Code = "" })
            .ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void BadCodeChars_Fails()
    {
        new ValidatePromoCodeDtoValidator().TestValidate(new ValidatePromoCodeDto { Code = "SAVE-10" })
            .ShouldHaveValidationErrorFor(x => x.Code);
    }
}

public class LanguageUpdateDtoValidatorTests
{
    [Theory]
    [InlineData("ua")]
    [InlineData("en")]
    [InlineData("EN")]
    public void SupportedLanguage_Passes(string lang)
    {
        new LanguageUpdateDtoValidator().TestValidate(new LanguageUpdateDto { Language = lang })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UnsupportedLanguage_Fails()
    {
        new LanguageUpdateDtoValidator().TestValidate(new LanguageUpdateDto { Language = "fr" })
            .ShouldHaveValidationErrorFor(x => x.Language);
    }

    [Fact]
    public void EmptyLanguage_Fails()
    {
        new LanguageUpdateDtoValidator().TestValidate(new LanguageUpdateDto { Language = "" })
            .ShouldHaveValidationErrorFor(x => x.Language);
    }
}

public class RefreshTokenRequestValidatorTests
{
    [Fact]
    public void ValidToken_Passes()
    {
        new RefreshTokenRequestValidator().TestValidate(new RefreshTokenRequest { RefreshToken = "token" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyToken_Fails()
    {
        new RefreshTokenRequestValidator().TestValidate(new RefreshTokenRequest { RefreshToken = "" })
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void TooLongToken_Fails()
    {
        new RefreshTokenRequestValidator().TestValidate(new RefreshTokenRequest { RefreshToken = new string('t', 201) })
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
