using HavaPaneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HavaPaneli.Infrastructure.Tests;

/// <summary>
/// Tek bir geçici PostgreSQL Docker konteyneri açar; her test kendi boş veritabanını alır.
/// Böylece testler birbirinin verisini görmez ve gerçek PostgreSQL davranışı (migration, tarih tipleri) denenir.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Yeni bir boş veritabanı açar, migration'ları uygular ve context fabrikasını döndürür.</summary>
    public async Task<IDbContextFactory<AppDbContext>> CreateDatabaseAsync()
    {
        var dbName = "t_" + Guid.NewGuid().ToString("N");

        await using (var admin = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", admin);
            await cmd.ExecuteNonQueryAsync();
        }

        var cs = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = dbName }.ConnectionString;
        var factory = new TestDbFactory(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options);

        // Gerçek migration'ı çalıştırmak, migration dosyasının da doğru olduğunu kanıtlar.
        await using var db = factory.CreateDbContext();
        await db.Database.MigrateAsync();
        return factory;
    }

    private sealed class TestDbFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
