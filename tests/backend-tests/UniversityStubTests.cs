using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;

namespace KhduSouvenirShop.API.Tests;

// Перевірка заглушки University API (Етап 9.5): активна в Development без ApiKey та BaseUrl
public class UniversityStubTests
{
    private static UniversityService CreateService(string? apiKey = null, string environmentName = "Development")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["University:BaseUrl"] = "",
                ["University:ApiKey"] = apiKey,
                ["University:AllowedDomains:0"] = "ksu.edu.ua"
            })
            .Build();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        return new UniversityService(
            new UserRepository(context),
            new Repository<VerificationQueue>(context),
            new Mock<IEmailService>().Object,
            NullLogger<UniversityService>.Instance,
            configuration,
            hostEnvironment.Object,
            new HttpClient());
    }

    [Theory]
    [InlineData("student.regular@ksu.edu.ua", "REGULAR")]
    [InlineData("student.scholarship@ksu.edu.ua", "SCHOLARSHIP")]
    [InlineData("student.high@ksu.edu.ua", "HIGH_ACHIEVER")]
    public async Task GetStudentInfoAsync_Stub_ReturnsExpectedStatus(string email, string expectedStatus)
    {
        var service = CreateService();
        var info = await service.GetStudentInfoAsync(email);
        info.Should().NotBeNull();
        info!.IsActive.Should().BeTrue();
        info.StudentStatus.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task GetStudentInfoAsync_Stub_ReturnsNullForNotFoundEmail()
    {
        var service = CreateService();
        (await service.GetStudentInfoAsync("notfound@ksu.edu.ua")).Should().BeNull();
    }

    [Fact]
    public async Task GetStudentInfoAsync_Stub_ReturnsNullForNonUniversityDomain()
    {
        var service = CreateService();
        (await service.GetStudentInfoAsync("student.regular@gmail.com")).Should().BeNull();
    }

    [Fact]
    public async Task GetStudentInfoAsync_StubDisabled_WhenApiKeyConfigured()
    {
        var service = CreateService(apiKey: "test-key");
        var act = async () => await service.GetStudentInfoAsync("student.regular@ksu.edu.ua");
        await act.Should().ThrowAsync<UniversityApiUnavailableException>();
    }
}
