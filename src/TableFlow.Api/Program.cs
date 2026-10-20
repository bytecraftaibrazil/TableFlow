using TableFlow.Api.Interfaces;
using TableFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using TableFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString(
        "TableFlowDatabase"
    )
    ?? throw new InvalidOperationException(
        "Connection string 'TableFlowDatabase' was not found."
    );

builder.Services.AddControllers(options =>
    options.ModelBinderProviders.Insert(0, new TableFlow.Api.Serialization.UtcDateTimeBinderProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new TableFlow.Api.Serialization.UtcDateTimeJsonConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConflictExceptionHandler>();

builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationEngine, ReservationEngine>();
builder.Services.AddScoped<IReservationWriteSession, ReservationWriteSession>();

builder.Services.AddDbContext<TableFlowDbContext>(
    options =>
        options.UseSqlServer(connectionString)
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Explicit administrative commands; normal startup never changes the schema or data.
if (args.Contains("--migrate") || args.Contains("--seed"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TableFlowDbContext>();
    if (args.Contains("--migrate")) await db.Database.MigrateAsync();
    if (args.Contains("--seed"))
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "scripts", "seed-large.sql"));
        await db.Database.ExecuteSqlRawAsync(sql);
    }
    return;
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "docs";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TableFlow.Api v1");
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }
