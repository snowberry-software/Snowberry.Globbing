using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Snowberry.Globbing.IntegrationTests.Infrastructure;

/// <summary>
/// A sample row whose <see cref="Value"/> is matched against generated regexes.
/// </summary>
public sealed class Sample
{
    /// <summary>
    /// Gets or sets the identifier, equal to the input's index in the conformance fixture.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the input text.
    /// </summary>
    public required string Value { get; set; }
}

/// <summary>
/// The EF Core context used to run generated regexes through the Npgsql query translation.
/// </summary>
/// <param name="options">The context options.</param>
public sealed class SampleContext(DbContextOptions<SampleContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the sample rows.
    /// </summary>
    public DbSet<Sample> Samples => Set<Sample>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sample>().Property(s => s.Id).ValueGeneratedNever();
    }
}

/// <summary>
/// Starts a PostgreSQL 17 container shared by all tests in the assembly.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Creates a context connected to the container's database.
    /// </summary>
    /// <returns>A new <see cref="SampleContext"/>; the caller disposes it.</returns>
    public SampleContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SampleContext>().UseNpgsql(_container.GetConnectionString()).Options;
        return new SampleContext(options);
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}