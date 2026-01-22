# EFCore.FluentIncludes

Simplify Entity Framework Core `Include`/`ThenInclude` chains with clean, readable path-based syntax.

## Why?

EF Core's eager loading syntax becomes hard to read with nested navigation properties:

```csharp
// Hard to read - what's actually being loaded?
var order = await context.Orders
    .Include(o => o.Customer)
        .ThenInclude(c => c.Address)
    .Include(o => o.Customer)
        .ThenInclude(c => c.PaymentMethods)
    .Include(o => o.LineItems)
        .ThenInclude(li => li.Product)
            .ThenInclude(p => p.Category)
    .Include(o => o.LineItems)
        .ThenInclude(li => li.Product)
            .ThenInclude(p => p.Images)
    .FirstOrDefaultAsync(o => o.Id == id);
```

With **EFCore.FluentIncludes**, the same query becomes:

```csharp
// Clear and scannable
var order = await context.Orders
    .IncludePaths(
        o => o.Customer.To().Address,
        o => o.Customer.To().PaymentMethods,
        o => o.LineItems.Each().Product.To().Category,
        o => o.LineItems.Each().Product.To().Images)
    .FirstOrDefaultAsync(o => o.Id == id);
```

## Installation

```bash
dotnet add package EFCore.FluentIncludes
```

**Requirements:** EF Core 8.0+, .NET 8.0+

## Getting Started

Add the namespace:

```csharp
using EFCore.FluentIncludes;
```

### Basic Usage

```csharp
// Single property
.IncludePaths(o => o.Customer)

// Nested property (Customer -> Address)
.IncludePaths(o => o.Customer.To().Address)

// Multiple paths
.IncludePaths(
    o => o.Customer.To().Address,
    o => o.ShippingAddress,
    o => o.BillingAddress)
```

### Collections: The `Each()` Method

When your path goes through a collection, use `Each()` to indicate "for each item, include...":

```csharp
// Order has many LineItems, each LineItem has one Product
.IncludePaths(o => o.LineItems.Each().Product)

// Go deeper: Product -> Category -> ParentCategory
.IncludePaths(o => o.LineItems.Each().Product.To().Category.To().ParentCategory)

// Multiple collection paths
.IncludePaths(
    o => o.LineItems.Each().Product,
    o => o.Payments.Each().PaymentMethod)
```

> **Note:** `Each()` is just a marker - it tells the library "this is a collection, continue through it." It generates the same SQL as standard EF Core includes.

### Nullable Navigations: The `To()` Method

When navigating through nullable properties, use `To()` to indicate "navigate to this property":

```csharp
// Customer is nullable (Customer?) - use To() to navigate through it
.IncludePaths(o => o.Customer.To().Address)

// Chain multiple To() calls for deep nullable paths
.IncludePaths(o => o.LineItems.Each().Product.To().Supplier.To().Address)

// Combine with Each() naturally
.IncludePaths(
    o => o.Customer.To().Address,
    o => o.Customer.To().PaymentMethods,
    o => o.LineItems.Each().Product.To().Category.To().ParentCategory)
```

> **Note:** Like `Each()`, `To()` is just a marker - the lambda is never executed at runtime. It's only analyzed to extract the navigation path. If the property is actually null in the database, EF Core handles it gracefully.

### Alternative: The `!` Operator

You can also use the null-forgiving operator (`!`) instead of `To()`:

```csharp
// Using ! (also works)
.IncludePaths(o => o.Customer!.Address)

// Using To() (recommended - more readable)
.IncludePaths(o => o.Customer.To().Address)
```

Both are completely safe - the lambda is never executed at runtime. The `!` is shorter but less semantic; `To()` is consistent with `Each()` and makes the navigation intent clear.

> **Why not `?.`?** C# doesn't allow the null-conditional operator (`?.`) in expression trees - it's a language limitation, not something any library can work around.

---

## Conditional Includes

Use `IncludePathsIf()` when you need to include data based on a condition. The first parameter is a boolean - if `true`, the paths are included; if `false`, they're skipped.

### When to Use Conditional Includes

**1. Optional data loading** - Sometimes you don't need all the data:

```csharp
// Only load product details when showing the full order page
var showFullDetails = true;

var order = await context.Orders
    .IncludePaths(o => o.Customer)  // Always load customer
    .IncludePathsIf(showFullDetails,
        o => o.LineItems.Each().Product.To().Images,
        o => o.LineItems.Each().Product.To().Category)
    .FirstOrDefaultAsync(o => o.Id == id);
```

**2. Feature flags** - Different features need different data:

```csharp
var order = await context.Orders
    .IncludePaths(o => o.Customer)
    .IncludePathsIf(includeShipping,
        o => o.ShippingAddress,
        o => o.LineItems.Each().Product)
    .IncludePathsIf(includePayments,
        o => o.Payments.Each().PaymentMethod)
    .IncludePathsIf(includeNotes,
        o => o.Notes.Each().Author)
    .FirstOrDefaultAsync(o => o.Id == id);
```

**3. User permissions** - Show more data to admins:

```csharp
var isAdmin = user.Role == "Admin";
var isEmployee = user.Role == "Admin" || user.Role == "Employee";

var order = await context.Orders
    .IncludePaths(o => o.Customer)  // Everyone sees basic customer info
    .IncludePathsIf(isEmployee,
        o => o.Customer.To().Address,
        o => o.LineItems.Each().Product)
    .IncludePathsIf(isAdmin,
        o => o.Customer.To().PaymentMethods,  // Only admins see payment info
        o => o.Notes.Each().Author)           // Only admins see internal notes
    .FirstOrDefaultAsync(o => o.Id == id);
```

**4. API query parameters** - Let clients request what they need:

```csharp
// Controller: GET /api/orders/1?include=products,payments
[HttpGet("{id}")]
public async Task<Order> GetOrder(int id, [FromQuery] string? include)
{
    var includes = include?.Split(',').ToHashSet() ?? new HashSet<string>();

    return await context.Orders
        .IncludePaths(o => o.Customer)
        .IncludePathsIf(includes.Contains("products"),
            o => o.LineItems.Each().Product.To().Category,
            o => o.LineItems.Each().Product.To().Images)
        .IncludePathsIf(includes.Contains("payments"),
            o => o.Payments.Each().PaymentMethod)
        .IncludePathsIf(includes.Contains("shipping"),
            o => o.ShippingAddress,
            o => o.BillingAddress)
        .FirstOrDefaultAsync(o => o.Id == id);
}
```

---

## Reusable Specifications

For queries you use repeatedly, create a specification class:

```csharp
public class OrderSummarySpec : IncludeSpec<Order>
{
    public OrderSummarySpec()
    {
        Include(o => o.Customer);
        Include(o => o.LineItems.Each().Product);
    }
}
```

Use it with `WithSpec()`:

```csharp
var orders = await context.Orders
    .WithSpec<Order, OrderSummarySpec>()
    .Where(o => o.Status == "Pending")
    .ToListAsync();
```

### Building on Other Specs

Specs can include other specs using `IncludeFrom<T>()`:

```csharp
public class OrderSummarySpec : IncludeSpec<Order>
{
    public OrderSummarySpec()
    {
        Include(o => o.Customer);
    }
}

public class OrderDetailSpec : IncludeSpec<Order>
{
    public OrderDetailSpec()
    {
        IncludeFrom<OrderSummarySpec>();  // Include everything from OrderSummarySpec

        Include(o => o.Customer.To().Address);
        Include(o => o.LineItems.Each().Product.To().Category);
        Include(o => o.Payments.Each().PaymentMethod);
    }
}
```

### Combining Specs

Apply multiple specs to one query:

```csharp
// Two specs
.WithSpecs<Order, OrderDetailSpec, OrderAuditSpec>()

// Or with instances
.WithSpecs(detailSpec, auditSpec)

// Conditional spec
.WithSpecIf<Order, OrderDetailSpec>(needsDetails)
```

### Mixing Specs with Ad-hoc Paths

```csharp
var order = await context.Orders
    .WithSpec<Order, OrderSummarySpec>()
    .IncludePaths(o => o.Notes.Each().Author)  // Add one-off include
    .FirstOrDefaultAsync(o => o.Id == id);
```

---

## Split Queries

Works with `AsSplitQuery()` to avoid cartesian explosion:

```csharp
var orders = await context.Orders
    .IncludePaths(
        o => o.LineItems.Each().Product,
        o => o.Payments.Each().PaymentMethod)
    .AsSplitQuery()
    .ToListAsync();
```

---

## Quick Reference

| Method | Purpose |
|--------|---------|
| `IncludePaths(paths...)` | Include one or more navigation paths |
| `IncludePathsIf(condition, paths...)` | Include paths only when condition is true |
| `Each()` | Navigate through a collection in a path |
| `To()` | Navigate through a nullable property in a path |
| `WithSpec<TEntity, TSpec>()` | Apply a reusable specification |
| `WithSpecs<TEntity, TSpec1, TSpec2>()` | Apply multiple specifications |
| `WithSpecIf<TEntity, TSpec>(condition)` | Apply spec only when condition is true |

---

## Before & After

| Scenario | Standard EF Core | EFCore.FluentIncludes |
|----------|------------------|-------------------|
| Nested property | `.Include(o => o.Customer).ThenInclude(c => c.Address)` | `.IncludePaths(o => o.Customer.To().Address)` |
| Through collection | `.Include(o => o.Items).ThenInclude(i => i.Product)` | `.IncludePaths(o => o.Items.Each().Product)` |
| Deep nesting | 4+ lines of Include/ThenInclude | `.IncludePaths(o => o.Items.Each().Product.To().Category.To().Parent)` |
| Multiple branches | Repeat Include chain for each branch | List all paths in one `IncludePaths()` call |

---

## Performance

- Generates identical SQL to standard EF Core includes
- No runtime overhead - expressions are parsed once and cached
- Full compatibility with `AsSplitQuery()` and `AsSingleQuery()`

## License

Apache 2.0 - See [LICENSE](LICENSE) for details.
