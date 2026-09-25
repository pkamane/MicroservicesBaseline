using System.Security.Claims;
using OrderApi.Models;
using OrderApi.Security;
using OrderApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithJwtBearer("OrderApi");
builder.Services.AddSingleton<OrderService>();
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

app.MapGet("/orders", (OrderService orders, ILogger<Program> logger) =>
{
    logger.LogInformation("Listing orders");
    return Results.Ok(orders.GetAll());
})
// Authorization: listing ALL orders is an administrative view; any authenticated user can place or read a single order.
.RequireAuthorization(policy => policy.RequireRole(Roles.Admin))
.WithSummary("List all orders (Admin only)")
.WithName("GetOrders")
.Produces<List<Order>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status403Forbidden)
.WithOpenApi();

app.MapGet("/orders/{id:int}", (int id, OrderService orders, ILogger<Program> logger) =>
{
    var order = orders.GetById(id);
    if (order is null)
    {
        logger.LogWarning("Order {OrderId} was not found", id);
        return Results.NotFound();
    }

    return Results.Ok(order);
})
.RequireAuthorization()
.WithName("GetOrderById")
.Produces<Order>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithOpenApi();

app.MapPost("/orders", (CreateOrderRequest request, OrderService orders, ILogger<Program> logger) =>
{
    if (request.ProductId <= 0)
    {
        return Results.BadRequest("ProductId must be greater than zero.");
    }

    if (request.Quantity <= 0)
    {
        return Results.BadRequest("Quantity must be greater than zero.");
    }

    var created = orders.Create(request.ProductId, request.Quantity);
    logger.LogInformation("Created order {OrderId}", created.Id);
    return Results.Created($"/orders/{created.Id}", created);
})
.RequireAuthorization()
.WithName("CreateOrder")
.Produces<Order>(StatusCodes.Status201Created)
.Produces<string>(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status401Unauthorized)
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

public record CreateOrderRequest(int ProductId, int Quantity);

public partial class Program;
