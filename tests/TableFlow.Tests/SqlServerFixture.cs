using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TableFlow.Api.Data;

namespace TableFlow.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TABLEFLOW_TEST_SQLSERVER")))
            Skip = "Set TABLEFLOW_TEST_SQLSERVER to run real SQL Server integration tests.";
    }
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private bool owned;
    public string ConnectionString { get; private set; } = "";
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public TableFlowDbContext OpenDb() => new(new DbContextOptionsBuilder<TableFlowDbContext>()
        .UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        var source = Environment.GetEnvironmentVariable("TABLEFLOW_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(source)) return;
        var name = "TableFlowTests_" + Guid.NewGuid().ToString("N");
        var builder = new SqlConnectionStringBuilder(source) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(builder.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{name}]";
            await command.ExecuteNonQueryAsync();
            owned = true;
        }
        builder.InitialCatalog = name;
        ConnectionString = builder.ConnectionString;
        await using var db = OpenDb();
        await db.Database.MigrateAsync();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment("Development");
            host.UseSetting("ConnectionStrings:TableFlowDatabase", ConnectionString);
        });
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (owned && ConnectionString.Length > 0)
        {
            // Only the randomly named database created by this fixture is deleted.
            await using var db = OpenDb();
            await db.Database.EnsureDeletedAsync();
        }
    }
}
