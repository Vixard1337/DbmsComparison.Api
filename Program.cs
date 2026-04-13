using DbmsComparison.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var defaultProviderName = builder.Configuration["Database:DefaultProvider"];
if (!DbContextOptionsFactory.TryParse(defaultProviderName, out var defaultProvider))
{
    defaultProvider = DbmsProvider.SqlServer;
}

builder.Services.AddDbContext<AppDbContext>(options =>
    DbContextOptionsFactory.ConfigureProvider(options, builder.Configuration, defaultProvider));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
