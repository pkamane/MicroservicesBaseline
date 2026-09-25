using System.Security.Claims;
using AuthApi.Configuration;
using AuthApi.Models;
using AuthApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration: appsettings / env vars -> IConfiguration -> IOptions<JwtOptions> ----
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ---- Application services ----
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>(); // POC only: swap for a persistent store later
builder.Services.AddSingleton<ITokenService, TokenService>();

// ---- Authentication / authorization ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Validation parameters come from the same validated JwtOptions used to sign tokens,
// instead of reading raw configuration here.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        // Keep claim names exactly as issued ("sub", "role") instead of mapping them to long XML URIs.
        bearer.MapInboundClaims = false;

        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.CreateSigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AuthClaimTypes.Username,
            RoleClaimType = AuthClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// ---- Swagger with a Bearer "Authorize" button ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthApi", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken from POST /auth/login (without the 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health");

var auth = app.MapGroup("/auth").WithTags("Auth");

auth.MapPost("/login", async (LoginRequest request, IUserStore users, ITokenService tokens, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest("Username and password are required.");
    }

    var user = await users.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
    if (user is null)
    {
        // Same 401 for unknown user and wrong password, so callers can't discover valid usernames.
        logger.LogWarning("Failed login attempt for {Username}", request.Username);
        return Results.Unauthorized();
    }

    logger.LogInformation("User {UserId} logged in", user.UserId);
    return Results.Ok(tokens.CreateAccessToken(user));
})
.AllowAnonymous()
.WithName("Login")
.Produces<LoginResponse>(StatusCodes.Status200OK)
.Produces<string>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.WithOpenApi();

auth.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
{
    userId = user.FindFirstValue("sub"),
    username = user.Identity?.Name,
    role = user.FindFirstValue(AuthClaimTypes.Role),
    claims = user.Claims.Select(claim => new { claim.Type, claim.Value })
}))
.RequireAuthorization()
.WithName("Me")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.WithOpenApi();

app.Run();

public partial class Program;
