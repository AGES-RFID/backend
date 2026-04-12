using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Backend.Features.Users;
using Backend.Features.Auth;
using Backend.Database;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

    // Configure JWT authentication for Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
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
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey not configured in appsettings.json");
}

var key = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "backend",
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"] ?? "frontend",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
