using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OMS.API.Middleware;
using OMS.Application;
using OMS.Application.Interfaces;
using OMS.Application.Services;
using OMS.Domain.Interfaces;
using OMS.Infrastructure;
using OMS.Infrastructure.Data;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// === Database ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// === DI ===
var useZitadelAuth = builder.Configuration.GetValue<bool>("Authentication:UseZitadelAuth");
OMS.Application.DISetup.Setup(builder.Services);
OMS.Infrastructure.DISetup.Setup(builder.Services, builder.Configuration);

// === Auth ===
if (useZitadelAuth) {
    // Zitadel mode: validate Zitadel-issued JWTs using OIDC discovery (JWKS)
    var zitadelAuthority = builder.Configuration["Authentication:Zitadel:Authority"] ?? throw new InvalidOperationException("Zitadel Authority is required when UseZitadelAuth is enabled.");
    var zitadelAudience = builder.Configuration["Authentication:Zitadel:Audience"] ?? "";
    var validateAudience = builder.Configuration.GetValue<bool>("Authentication:Zitadel:ValidateAudience", true);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => {
            options.Authority = zitadelAuthority;
            options.Audience = zitadelAudience;
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidateAudience = validateAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });
} else {
    // In-app mode: validate OMS-issued JWTs using symmetric key
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "OMS_SuperSecretKey_2026_MustBe32Chars!!";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "OMS",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "OMS",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });
}
builder.Services.AddAuthorization();

// === Controllers ===
builder.Services.AddControllers();

// === CORS (allow Blazor frontend) ===
builder.Services.AddCors(options => {
    options.AddPolicy("AllowBlazor", policy => {
        policy.WithOrigins(
            "https://localhost:5002",
            "http://localhost:5003",
            "https://localhost:7001",
            "http://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// === Swagger ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "Order Management System API",
        Version = "v1",
        Description = "Full-stack Order Management System with JWT authentication"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// === Middleware ===
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OMS API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseAuthentication();
app.UseMiddleware<TokenValidationMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.UseRouting();
app.UseHttpMetrics();

app.MapMetrics();

// === Auto-migrate in development ===
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment()) {
        db.Database.EnsureCreated();
    }
}

app.Run();
