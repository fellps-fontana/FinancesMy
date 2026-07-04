using MyFinances.Data;
using MyFinances.Infrastructure.Filters;
using MyFinances.Repositories;
using MyFinances.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<GlobalExceptionFilter>());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<MyFinancesDbContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddScoped<IContaRepository, ContaRepository>();
builder.Services.AddScoped<IContaService, ContaService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();

public partial class Program { }
