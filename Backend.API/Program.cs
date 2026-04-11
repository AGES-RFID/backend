using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

using Backend.Features.Users;
using Backend.Database;

var builder = WebApplication.CreateBuilder(args);

const string DevCorsPolicyName = "DevCors";

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );
    });
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.UseInlineDefinitionsForEnums();
});

var connectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'Default'. Configure ConnectionStrings:Default or set the ConnectionStrings__Default environment variable"
    );
}

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DevCorsPolicyName, policy =>
        {
            policy
                .WithOrigins("*") // in dev mode we allow everything
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, o =>
        o.MapEnum<UserRole>("user_role", "public")
    )
    .UseSnakeCaseNamingConvention()
);

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

    app.UseCors(DevCorsPolicyName);
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
