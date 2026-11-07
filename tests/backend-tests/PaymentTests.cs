using KhduSouvenirShop.API.Services;
using Xunit;

namespace KhduSouvenirShop.API.Tests;

public class PaymentStatesTests
{
    [Theory]
    [InlineData("Card", true)]
    [InlineData("CashOnDelivery", true)]
    [InlineData("cashondelivery", false)] // значення регістрозалежні
    [InlineData("Bitcoin", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsKnownMethod_ValidatesPaymentMethods(string? method, bool expected)
    {
        Assert.Equal(expected, PaymentStates.IsKnownMethod(method));
    }

    [Theory]
    [InlineData("Completed", true)]
    [InlineData("Pending", false)]
    [InlineData("RequiresAction", false)]
    [InlineData("Failed", false)]
    [InlineData("Cancelled", false)]
    [InlineData("Refunded", false)]
    [InlineData(null, false)]
    public void IsPaid_TrueOnlyForCompleted(string? status, bool expected)
    {
        Assert.Equal(expected, PaymentStates.IsPaid(status));
    }

    [Theory]
    [InlineData("CashOnDelivery", true)]
    [InlineData("Card", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsCashOnDelivery_DetectsCod(string? method, bool expected)
    {
        Assert.Equal(expected, PaymentStates.IsCashOnDelivery(method));
    }

    [Theory]
    [InlineData(199.99, 199.99, true)]
    [InlineData(0, 0, true)]
    [InlineData(100, 199.99, false)]
    [InlineData(200.00, 199.99, false)]
    public void IsAmountConsistent_ComparesAmounts(decimal paymentAmount, decimal orderTotal, bool expected)
    {
        Assert.Equal(expected, PaymentStates.IsAmountConsistent(paymentAmount, orderTotal));
    }
}

public class OrderStateMachineTests
{
    [Theory]
    [InlineData("Processing", "Shipped", true)]
    [InlineData("Paid", "Shipped", true)]
    [InlineData("Shipped", "Delivered", true)]
    [InlineData("Processing", "Cancelled", true)]
    [InlineData("Paid", "Cancelled", true)]
    [InlineData("Shipped", "Cancelled", true)]
    [InlineData("Processing", "Delivered", false)] // перестрибування через статус
    [InlineData("Delivered", "Shipped", false)]    // термінальний статус
    [InlineData("Delivered", "Cancelled", false)]  // термінальний статус
    [InlineData("Cancelled", "Processing", false)] // термінальний статус
    [InlineData("Unknown", "Shipped", false)]      // невідомий статус
    public void CanTransition_RespectsStateMachine(string from, string to, bool expected)
    {
        Assert.Equal(expected, OrderStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData("Processing", true)]
    [InlineData("Paid", true)]
    [InlineData("Shipped", true)]
    [InlineData("Delivered", true)]
    [InlineData("Cancelled", true)]
    [InlineData("processing", false)]
    [InlineData("Done", false)]
    [InlineData(null, false)]
    public void IsKnownStatus_ValidatesOrderStatuses(string? status, bool expected)
    {
        Assert.Equal(expected, OrderStateMachine.IsKnownStatus(status));
    }
}
