using ProductApi.Models;
using ProductApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapHealthChecks("/health");
app.MapGet("/version", () => Environment.GetEnvironmentVariable("APP_VERSION") ?? "v1");

app.MapGet("/products", (ProductService products, ILogger<Program> logger) =>
{
    logger.LogInformation("Listing products");
    return Results.Ok(products.GetAll());
})
.WithName("GetProducts")
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
.WithName("GetProductById")
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
.WithName("CreateProduct")
.WithOpenApi();

app.Run();

public record CreateProductRequest(string Name, decimal Price);

public partial class Program;
