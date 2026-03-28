using Microsoft.EntityFrameworkCore;

using Backend.Features.Users;
using Backend.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'Default'. Configure ConnectionStrings:Default or set the ConnectionStrings__Default environment variable"
    );
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// Register feature services
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
