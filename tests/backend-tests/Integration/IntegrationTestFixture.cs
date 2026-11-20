using System;
using System.Threading.Tasks;
using KhduSouvenirShop.API.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using Xunit;

namespace KhduSouvenirShop.API.Tests.Integration;

/// <summary>
/// Спільна фікстура для інтеграційних тестів: піднімає тимчасову MySQL у Docker (TestContainers),
/// застосовує EF-міграції, а потім створює <see cref="ApiTestFactory"/> поверх цієї БД.
/// Один контейнер і один тестовий host використовуються всіма класами колекції "integration".
/// </summary>
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private const int MySqlPort = 3306;
    private MySqlContainer _container = null!;

    public ApiTestFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .WithDatabase("khdu_test")
            .WithUsername("khdu")
            .WithPassword("khdu_test_pass")
            // Спрощена автентифікація для локального контейнера (уникає проблем із caching_sha2_password)
            .WithCommand("--default-authentication-plugin=mysql_native_password")
            .Build();

        await _container.StartAsync();

        var connectionString =
            $"Server={_container.Hostname};Port={_container.GetMappedPublicPort(MySqlPort)};" +
            "Database=khdu_test;Uid=khdu;Pwd=khdu_test_pass;AllowPublicKeyRetrieval=true;SslMode=Preferred";

        // Схему БД (міграції + HasData: категорії/товари) створюємо до старту тестового host,
        // бо сама програма міграції не застосовує - лише додає тестові дані через DbSeeder.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        await using (var context = new AppDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        Factory = new ApiTestFactory(connectionString);

        // Примусовий старт host: виконується Program.cs (у т.ч. DbSeeder з тестовими користувачами)
        using var warmup = Factory.CreateApiClient();
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>Колекція інтеграційних тестів, що розділяють одну MySQL-контейнер та один тестовий host.</summary>
[CollectionDefinition("integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
