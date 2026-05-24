using OMS.Web.Components;
using OMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OMS.Web.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using OMS.Domain.Entities;
using OMS.Domain.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
    var authority = builder.Configuration["Authentication:Zitadel:Authority"] ?? throw new InvalidOperationException("Zitadel Authority is required.");
    var clientId = builder.Configuration["Authentication:Zitadel:ClientId"] ?? "";
    var clientSecret = builder.Configuration["Authentication:Zitadel:ClientSecret"] ?? "";
    var validateAudience = builder.Configuration.GetValue<bool>("Authentication:Zitadel:ValidateAudience", true);

    builder.Services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options => {
        options.Authority = authority;
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.TokenValidationParameters = new TokenValidationParameters {
            NameClaimType = "name",
            RoleClaimType = "role",
            ValidateIssuer = true,
            ValidateAudience = validateAudience,
            ValidAudience = clientId
        };

        options.Events = new OpenIdConnectEvents {
            OnTokenValidated = async context => {
                var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value 
                            ?? context.Principal?.FindFirst("email")?.Value;
                var name = context.Principal?.FindFirst(ClaimTypes.Name)?.Value 
                           ?? context.Principal?.FindFirst("name")?.Value 
                           ?? context.Principal?.FindFirst("preferred_username")?.Value;
                var zitadelId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                ?? context.Principal?.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(zitadelId)) {
                    var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    
                    var user = await db.Users.FirstOrDefaultAsync(u => u.ZitadelId == zitadelId || u.Email == email);
                    if (user == null) {
                        user = new User {
                            Email = email,
                            Name = name ?? email,
                            ZitadelId = zitadelId,
                            Role = UserRole.User
                        };
                        db.Users.Add(user);
                        await db.SaveChangesAsync();
                    } else if (user.ZitadelId == null) {
                        user.ZitadelId = zitadelId;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                    }
                }
            }
        };
    });

    builder.Services.AddCascadingAuthenticationState();
    // In Zitadel mode, standard ServerAuthenticationStateProvider handles reading the cookie directly.
} else {
    builder.Services.AddAuthentication(options => {
        options.DefaultScheme = "Cookies";
    }).AddCookie("Cookies", options => {
        options.LoginPath = "/login";
    });
    builder.Services.AddAuthorization();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// === Challenge Endpoints for Zitadel OIDC ===
// TODO: Remove controller logic from Program.cs
app.MapGet("/login-challenge", async (HttpContext context) => {
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties {
        RedirectUri = "/"
    });
});

app.MapGet("/logout-challenge", async (HttpContext context) => {
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties {
        RedirectUri = "/"
    });
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// === Auto-migrate in development ===
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment()) {
        db.Database.EnsureCreated();
    }
}

app.Run();
