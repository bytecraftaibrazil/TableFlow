using TableFlow.Api.Interfaces;
using TableFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add support for controllers and API endpoints
builder.Services.AddControllers();

builder.Services.AddScoped<IRestaurantService, RestaurantService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add authorization middleware and routing for controllers
app.MapControllers();

app.Run();