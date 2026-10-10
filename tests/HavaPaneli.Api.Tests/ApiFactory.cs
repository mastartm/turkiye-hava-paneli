using HavaPaneli.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HavaPaneli.Api.Tests;

/// <summary>
/// Tek bir geçici PostgreSQL konteyneri açar (Infrastructure testlerindeki gibi). Her test kendi boş veritabanını
/// ve onu kullanan, saati sabitlenmiş bir API örneğini alır; testler birbirinin verisini görmez.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task<TestApi> CreateApiAsync(DateTimeOffset now)
    {
        var dbName = "t_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", admin);
            await cmd.ExecuteNonQueryAsync();
        }

        var cs = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = dbName }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options;
        var dbFactory = new SimpleDbFactory(options);
        await using (var db = dbFactory.CreateDbContext())
            await db.Database.MigrateAsync();

        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Default", cs);
            b.ConfigureTestServices(s =>
            {
                s.RemoveAll<TimeProvider>();
                s.AddSingleton<TimeProvider>(new FixedTime(now));
            });
        });
        return new TestApi(factory, dbFactory);
    }

    private sealed class SimpleDbFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

public sealed class TestApi(WebApplicationFactory<Program> factory, IDbContextFactory<AppDbContext> db) : IAsyncDisposable
{
    public HttpClient Client { get; } = factory.CreateClient();
    public IDbContextFactory<AppDbContext> Db { get; } = db;

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return factory.DisposeAsync();
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
