using System;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;
using KhduSouvenirShop.API.Controllers;

namespace KhduSouvenirShop.API.Tests;

/// <summary>
/// RBAC-перевірки: контролери мають відповідні [Authorize(Roles=...)] на рівні класу,
/// тож неавторизований/непривілейований доступ відхиляється ASP.NET Core.
/// </summary>
public class RbacTests
{
    private static string? RolesOf(Type controller) =>
        controller.GetCustomAttribute<AuthorizeAttribute>()?.Roles;

    [Theory]
    [InlineData(typeof(AdminProductsController), "Manager,Administrator,SuperAdmin")]
    [InlineData(typeof(AdminOrdersController), "Manager,Administrator,SuperAdmin")]
    [InlineData(typeof(AdminCategoriesController), "Manager,Administrator,SuperAdmin")]
    [InlineData(typeof(AdminPromotionsController), "Manager,Administrator,SuperAdmin")]
    [InlineData(typeof(AnalyticsController), "Manager,Administrator,SuperAdmin")]
    [InlineData(typeof(CompaniesController), "Manager,Administrator,SuperAdmin")]
    [InlineData(typeof(WarehouseDocumentsController), "Manager,Administrator,SuperAdmin")]
    [InlineData(typeof(AdminUsersController), "Administrator,SuperAdmin")]
    [InlineData(typeof(AdminUniversityController), "Administrator,SuperAdmin")]
    public void StaffControllers_RequireExpectedRoles(Type controller, string expectedRoles)
    {
        RolesOf(controller).Should().Be(expectedRoles);
    }

    [Theory]
    [InlineData(typeof(OrdersController))]
    [InlineData(typeof(CartController))]
    public void CustomerControllers_RequireAuthentication(Type controller)
    {
        var attribute = controller.GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().BeNullOrEmpty();
    }

    [Fact]
    public void AdminUsers_DoesNotGrantAccessToManager()
    {
        RolesOf(typeof(AdminUsersController)).Should().NotContain("Manager");
    }
}
