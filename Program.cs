using DbmsComparison.Api.Data;
using DbmsComparison.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddOpenApiDocument();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<BenchmarkRunner>();
builder.Services.AddScoped<BenchmarkBatchRunner>();
builder.Services.AddScoped<ImplementationCostReportService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<DiagramService>();

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
    if (string.Equals(Environment.GetEnvironmentVariable("DBMSCOMPARISON_DOCKER"), "true", StringComparison.OrdinalIgnoreCase))
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose up -d",
                WorkingDirectory = app.Environment.ContentRootPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
    }

    app.MapOpenApi();
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
