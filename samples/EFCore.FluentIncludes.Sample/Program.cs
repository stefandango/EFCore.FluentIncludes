using EFCore.FluentIncludes;
using EFCore.FluentIncludes.Sample.Data;
using EFCore.FluentIncludes.Sample.Entities;
using EFCore.FluentIncludes.Sample.Specifications;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure SQLite in-memory database
builder.Services.AddDbContext<SampleDbContext>(options =>
    options.UseSqlite("Data Source=sample.db"));

var app = builder.Build();

// Initialize and seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    context.Database.EnsureCreated();
    SampleDataSeeder.Seed(context);
}

// ============================================================================
// API Endpoints demonstrating EFCore.FluentIncludes features
// ============================================================================

// GET /orders - Basic IncludePaths()
// Demonstrates: Simple navigation paths with Each() for collections
app.MapGet("/orders", async (SampleDbContext db) =>
{
    var orders = await db.Orders
        .IncludePaths(
            o => o.Customer,
            o => o.LineItems.Each())
        .ToListAsync();

    return orders.Select(o => new
    {
        o.Id,
        o.OrderNumber,
        o.OrderDate,
        o.TotalAmount,
        CustomerName = o.Customer?.Name,
        LineItemCount = o.LineItems.Count
    });
});

// GET /orders/{id} - WithSpec<OrderDetailSpec>()
// Demonstrates: Specification pattern with UseSplitQuery(), AsNoTracking(), deep paths
app.MapGet("/orders/{id:int}", async (int id, SampleDbContext db) =>
{
    var order = await db.Orders
        .WithSpec<Order, OrderDetailSpec>()
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order is null)
        return Results.NotFound();

    return Results.Ok(new
    {
        order.Id,
        order.OrderNumber,
        order.OrderDate,
        order.TotalAmount,
        Customer = order.Customer is null ? null : new
        {
            order.Customer.Name,
            order.Customer.Email,
            Address = order.Customer.Address is null ? null : new
            {
                order.Customer.Address.Street,
                order.Customer.Address.City,
                order.Customer.Address.Country
            }
        },
        LineItems = order.LineItems.Select(li => new
        {
            li.Id,
            li.Quantity,
            li.UnitPrice,
            ProductName = li.Product?.Name,
            CategoryName = li.Product?.Category?.Name
        })
    });
});

// GET /orders/{id}/summary - WithSpec<OrderSummarySpec>()
// Demonstrates: Simple specification with basic includes
app.MapGet("/orders/{id:int}/summary", async (int id, SampleDbContext db) =>
{
    var order = await db.Orders
        .WithSpec<Order, OrderSummarySpec>()
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order is null)
        return Results.NotFound();

    return Results.Ok(new
    {
        order.OrderNumber,
        order.OrderDate,
        order.TotalAmount,
        CustomerName = order.Customer?.Name
    });
});

// GET /products - ProductCatalogSpec with category hierarchy
// Demonstrates: To() marker for nullable navigation, category hierarchy
app.MapGet("/products", async (int? categoryId, SampleDbContext db) =>
{
    var query = db.Products.WithSpec<Product, ProductCatalogSpec>();

    if (categoryId.HasValue)
        query = query.Where(p => p.CategoryId == categoryId.Value);

    var products = await query.ToListAsync();

    return products.Select(p => new
    {
        p.Id,
        p.Name,
        p.Price,
        CategoryName = p.Category?.Name,
        ParentCategoryName = p.Category?.ParentCategory?.Name
    });
});

// GET /customers/{id}/orders - Conditional includes via query params
// Demonstrates: WithSpecIf() for conditional spec application
app.MapGet("/customers/{id:int}/orders", async (
    int id,
    bool includeProducts,
    bool includeAddress,
    SampleDbContext db) =>
{
    // Using WithSpecIf() to conditionally apply specifications
    var customer = await db.Customers
        .WithSpecIf<Customer, CustomerAddressSpec>(includeAddress)
        .WithSpecIf<Customer, CustomerOrdersSpec>(includeProducts)
        .IncludePathsIf(!includeProducts, c => c.Orders.Each().LineItems.Each())
        .FirstOrDefaultAsync(c => c.Id == id);

    if (customer is null)
        return Results.NotFound();

    return Results.Ok(new
    {
        customer.Id,
        customer.Name,
        customer.Email,
        Address = includeAddress && customer.Address is not null ? new
        {
            customer.Address.Street,
            customer.Address.City,
            customer.Address.Country
        } : null,
        Orders = customer.Orders.Select(o => new
        {
            o.Id,
            o.OrderNumber,
            o.OrderDate,
            o.TotalAmount,
            LineItems = o.LineItems.Select(li => new
            {
                li.Quantity,
                li.UnitPrice,
                ProductName = includeProducts ? li.Product?.Name : null
            })
        })
    });
});

// GET /orders/recent - Ordered includes
// Demonstrates: OrderByDescending() in include paths
app.MapGet("/orders/recent", async (SampleDbContext db) =>
{
    var orders = await db.Orders
        .IncludePaths(
            o => o.Customer,
            o => o.LineItems.OrderByDescending(li => li.UnitPrice).Each().Product)
        .OrderByDescending(o => o.OrderDate)
        .Take(5)
        .ToListAsync();

    return orders.Select(o => new
    {
        o.Id,
        o.OrderNumber,
        o.OrderDate,
        o.TotalAmount,
        CustomerName = o.Customer?.Name,
        LineItems = o.LineItems.Select(li => new
        {
            li.UnitPrice,
            ProductName = li.Product?.Name
        })
    });
});

// GET /orders/{id}/full - OrderFullSpec with spec inheritance
// Demonstrates: IncludeFrom<TSpec>() to inherit from another specification
app.MapGet("/orders/{id:int}/full", async (int id, SampleDbContext db) =>
{
    // OrderFullSpec inherits from OrderDetailSpec and adds more includes
    var order = await db.Orders
        .WithSpec<Order, OrderFullSpec>()
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order is null)
        return Results.NotFound();

    return Results.Ok(new
    {
        order.Id,
        order.OrderNumber,
        order.OrderDate,
        order.TotalAmount,
        Customer = order.Customer is null ? null : new
        {
            order.Customer.Name,
            order.Customer.Email,
            Address = order.Customer.Address is null ? null : new
            {
                order.Customer.Address.Street,
                order.Customer.Address.City,
                order.Customer.Address.Country
            }
        },
        LineItems = order.LineItems.Select(li => new
        {
            li.Id,
            li.Quantity,
            li.UnitPrice,
            ProductName = li.Product?.Name,
            CategoryName = li.Product?.Category?.Name,
            ParentCategoryName = li.Product?.Category?.ParentCategory?.Name
        })
    });
});

// GET /orders/high-value - Filtered includes with Where()
// Demonstrates: Where() in include paths for filtered eager loading
app.MapGet("/orders/high-value", async (decimal minPrice, SampleDbContext db) =>
{
    // Using Where() to filter which line items are included
    // Only includes line items where UnitPrice >= minPrice
    var orders = await db.Orders
        .IncludePaths(
            o => o.Customer,
            o => o.LineItems.Where(li => li.UnitPrice >= minPrice).Each().Product)
        .Where(o => o.LineItems.Any(li => li.UnitPrice >= minPrice))
        .ToListAsync();

    return orders.Select(o => new
    {
        o.Id,
        o.OrderNumber,
        o.TotalAmount,
        CustomerName = o.Customer?.Name,
        HighValueItems = o.LineItems.Select(li => new
        {
            li.UnitPrice,
            ProductName = li.Product?.Name
        })
    });
});

// GET /categories - Category hierarchy with sub-categories
// Demonstrates: IncludeFrom() with base path to reduce repetition
app.MapGet("/categories", async (SampleDbContext db) =>
{
    // Using IncludeFrom() to avoid repeating the base path
    // Instead of writing:
    //   .IncludePaths(
    //       c => c.SubCategories.Each().Products.Each(),
    //       c => c.SubCategories.Each().SubCategories.Each())
    // We can share the base path:
    var categories = await db.Categories
        .Where(c => c.ParentCategoryId == null)
        .IncludeFrom(
            c => c.SubCategories.Each(),
            sub => sub.Products.Each(),
            sub => sub.SubCategories.Each())
        .IncludePaths(c => c.Products.Each())
        .AsNoTracking()
        .ToListAsync();

    return categories.Select(MapCategory);

    object MapCategory(Category c) => new
    {
        c.Id,
        c.Name,
        ProductCount = c.Products.Count,
        SubCategories = c.SubCategories.Select(MapCategory)
    };
});

Console.WriteLine("Sample API running at http://localhost:5000");
Console.WriteLine();
Console.WriteLine("Available endpoints:");
Console.WriteLine("  GET /orders                                    - Basic IncludePaths() with Each()");
Console.WriteLine("  GET /orders/{id}                               - WithSpec<OrderDetailSpec>()");
Console.WriteLine("  GET /orders/{id}/summary                       - WithSpec<OrderSummarySpec>()");
Console.WriteLine("  GET /orders/{id}/full                          - Spec inheritance with IncludeFrom<TSpec>()");
Console.WriteLine("  GET /orders/recent                             - OrderBy() in include paths");
Console.WriteLine("  GET /orders/high-value?minPrice=100            - Where() filtered includes");
Console.WriteLine("  GET /products                                  - To() marker for nullable navigation");
Console.WriteLine("  GET /products?categoryId={id}                  - Filtered query with spec");
Console.WriteLine("  GET /customers/{id}/orders?includeProducts=true&includeAddress=true");
Console.WriteLine("                                                 - WithSpecIf() conditional specs");
Console.WriteLine("  GET /categories                                - IncludeFrom() with base path");

app.Run();
