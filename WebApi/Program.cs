using Swashbuckle.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Rently.Management.Domain.Repositories;
using Rently.Management.Infrastructure.Repositories;
using Rently.Management.WebApi;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Rently.Management.Domain.Entities;
using Rently.Management.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile(Path.Combine("WebApi", "appsettings.json"), optional: false, reloadOnChange: true)
    .AddJsonFile(Path.Combine("WebApi", $"appsettings.{builder.Environment.EnvironmentName}.json"), optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddHttpClient("paymob", client =>
{
    var baseUrl = builder.Configuration["Paymob:BaseUrl"] ?? "https://accept.paymob.com/api";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddSingleton<PaymobService>();
builder.Services.AddSingleton<PasswordService>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "";
var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
var jwtExpireMinutes = int.TryParse(builder.Configuration["Jwt:ExpiresMinutes"], out var m) ? m : 60;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Rently API", Version = "v1" });
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    };
    c.AddSecurityRequirement(securityRequirement);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"CS = {cs}");

var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    // Ensure required columns exist in Users table to avoid runtime errors
    try
    {
        var conn = ctx.Database.GetDbConnection();
        conn.Open();
        string[] cols = ["PasswordHash","PasswordSalt","PasswordResetToken","PasswordResetTokenExpires"];
        foreach (var col in cols)
        {
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = @c";
            var p = checkCmd.CreateParameter();
            p.ParameterName = "@c";
            p.Value = col;
            checkCmd.Parameters.Add(p);
            var existsCount = Convert.ToInt32(checkCmd.ExecuteScalar());
            if (existsCount == 0)
            {
                using var alter = conn.CreateCommand();
                var sqlType = col == "PasswordResetTokenExpires" ? "datetime2 NULL" : "nvarchar(500) NULL";
                alter.CommandText = $"ALTER TABLE [Users] ADD [{col}] {sqlType}";
                alter.ExecuteNonQuery();
            }
        }
        conn.Close();
    }
    catch { }
    var adminEmail = config["Admin:Email"] ?? "";
    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        var exists = ctx.Users.FirstOrDefault(u => u.Email == adminEmail);
        if (exists == null)
        {
            var adminUser = new User
            {
                Name = "Admin",
                Email = adminEmail,
                Role = "Admin",
                ApprovalStatus = "Approved",
                CreatedAt = DateTime.UtcNow
            };
            ctx.Users.Add(adminUser);
            ctx.SaveChanges();
        }
    }
}

app.MapControllers();

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
