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

builder.Services.AddControllers();

builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

builder.Services.AddDbContext<TableFlowDbContext>(
    options =>
        options.UseSqlServer(connectionString)
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
