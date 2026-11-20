using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Moq;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Repositories;

namespace KhduSouvenirShop.API.Tests;

/// <summary>
/// Спільні хелпери для unit-тестів: ізольований InMemory-контекст, фейковий UnitOfWork
/// (InMemory-провайдер не підтримує реальні транзакції) та конфігурація в пам'яті.
/// </summary>
public static class TestDb
{
    public static AppDbContext Create() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    public static IConfiguration Config(params (string Key, string? Value)[] values)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (key, value) in values)
            dict[key] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    /// <summary>Заглушка транзакції: Commit/Rollback/Dispose — no-op.</summary>
    public static IDbContextTransaction FakeTransaction()
    {
        var tx = new Mock<IDbContextTransaction>();
        tx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        tx.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        tx.Setup(t => t.Dispose());
        return tx.Object;
    }
}

/// <summary>
/// UnitOfWork поверх реального InMemory-контексту: SaveChanges справді зберігає зміни,
/// а BeginTransactionAsync повертає заглушку транзакції (InMemory не має справжніх транзакцій).
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public FakeUnitOfWork(AppDbContext context)
    {
        _context = context;
        _transaction = TestDb.FakeTransaction();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_transaction);
}
