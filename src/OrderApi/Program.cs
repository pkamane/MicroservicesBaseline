using OrderApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapHealthChecks("/health");

app.MapGet("/orders", (OrderService orders, ILogger<Program> logger) =>
{
    logger.LogInformation("Listing orders");
    return Results.Ok(orders.GetAll());
})
.WithName("GetOrders")
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
.WithName("GetOrderById")
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
.WithName("CreateOrder")
.WithOpenApi();

app.Run();

public record CreateOrderRequest(int ProductId, int Quantity);

public partial class Program;
