using System.Security.Claims;
using ProductApi.Models;
using ProductApi.Security;
using ProductApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithJwtBearer("ProductApi");
builder.Services.AddSingleton<ProductService>();
builder.Services.AddHealthChecks();
builder.Services.AddJwtBearerAuthentication(builder.Configuration);

// Error responses as RFC 9457 ProblemDetails (application/problem+json).
// Client messages stay generic; the traceId in each response links to the server logs for the real reason.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Detail ??= context.ProblemDetails.Status switch
        {
            StatusCodes.Status401Unauthorized => "Authentication is required. Provide a valid bearer token.",
            StatusCodes.Status403Forbidden => "You do not have permission to perform this operation.",
            _ => null
        };
    };
});

var app = builder.Build();

// Turns empty 4xx responses (401 from JwtBearer, 403 from authorization, 404) into ProblemDetails bodies.
// Registered before authentication/authorization so it wraps them.
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapHealthChecks("/health");
app.MapGet("/version", () => Environment.GetEnvironmentVariable("APP_VERSION") ?? "v1");

app.MapGet("/products", (ProductService products, ILogger<Program> logger) =>
{
    logger.LogInformation("Listing products");
    return Results.Ok(products.GetAll());
})
.RequireAuthorization()
.WithName("GetProducts")
.Produces<List<Product>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.WithOpenApi();

app.MapGet("/products/{id:int}", (int id, ProductService products, ILogger<Program> logger) =>
{
    var product = products.GetById(id);
    if (product is null)
    {
        logger.LogWarning("Product {ProductId} was not found", id);
        return Results.NotFound();
    }

    return Results.Ok(product);
})
.RequireAuthorization()
.WithName("GetProductById")
.Produces<Product>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithOpenApi();

app.MapPost("/products", (CreateProductRequest request, ProductService products, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest("Name is required.");
    }

    var created = products.Create(request.Name, request.Price);
    logger.LogInformation("Created product {ProductId}", created.Id);
    return Results.Created($"/products/{created.Id}", created);
})
// Authorization: any authenticated user can read products; only Admin can change the catalog.
.RequireAuthorization(policy => policy.RequireRole(Roles.Admin))
.WithSummary("Create a product (Admin only)")
.WithName("CreateProduct")
.Produces<Product>(StatusCodes.Status201Created)
.Produces<string>(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status403Forbidden)
.WithOpenApi();

// Demo endpoint: shows the identity ASP.NET Core built from the validated JWT.
app.MapGet("/security/me", (ClaimsPrincipal user) => Results.Ok(new
{
    userId = user.FindFirstValue(AuthClaimTypes.UserId),
    username = user.Identity?.Name,
    role = user.FindFirstValue(AuthClaimTypes.Role),
    isAdmin = user.IsInRole(Roles.Admin)
}))
.RequireAuthorization()
.WithName("SecurityMe")
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.WithTags("Security")
.WithOpenApi();

app.Run();

public record CreateProductRequest(string Name, decimal Price);

public partial class Program;
