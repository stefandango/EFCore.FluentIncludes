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

### Filtered Collections: The `Where()` Method

Filter collections during eager loading using standard LINQ `Where()`:

```csharp
// Only load active line items
.IncludePaths(o => o.LineItems.Where(li => li.IsActive).Each())

// Filter and continue the path
.IncludePaths(o => o.LineItems.Where(li => li.Quantity > 0).Each().Product)

// Filter at any collection level
.IncludePaths(o => o.LineItems.Each().Product.To().Images.Where(i => i.IsPrimary).Each())
```

This generates EF Core's filtered includes, loading only matching items from the database.

**Common use cases:**

```csharp
// Only published posts
.IncludePaths(b => b.Posts.Where(p => p.IsPublished).Each().Comments)

// Recent orders only
.IncludePaths(c => c.Orders.Where(o => o.Date >= cutoffDate).Each().Items)

// Primary images only
.IncludePaths(p => p.Images.Where(i => i.IsPrimary).Each())
```

> **Note:** EF Core allows only one unique filter per navigation in a single query. If you need different subsets, use separate queries.

### Ordered Collections: The `OrderBy()` Method

Order collections during eager loading using standard LINQ ordering methods:

```csharp
// Order line items by creation date
.IncludePaths(o => o.LineItems.OrderBy(li => li.CreatedAt).Each())

// Descending order
.IncludePaths(o => o.LineItems.OrderByDescending(li => li.CreatedAt).Each())

// Order and continue the path
.IncludePaths(o => o.LineItems.OrderBy(li => li.DisplayOrder).Each().Product)

// Order at any collection level
.IncludePaths(o => o.LineItems.Each().Product.To().Images.OrderBy(i => i.SortOrder).Each())
```

**Multiple sort criteria** using `ThenBy` and `ThenByDescending`:

```csharp
// Primary sort, then secondary sort
.IncludePaths(o => o.LineItems
    .OrderBy(li => li.Category)
    .ThenByDescending(li => li.DisplayOrder)
    .Each()
    .Product)
```

**Combined with filtering** - filter first, then order:

```csharp
// Filter to active items, then order them
.IncludePaths(o => o.LineItems
    .Where(li => li.IsActive)
    .OrderBy(li => li.DisplayOrder)
    .Each()
    .Product)
```

This generates EF Core's ordered includes, controlling the order of loaded items.

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

## Grouping Paths with `IncludeFrom`

When multiple paths share the same base (especially with filters), use `IncludeFrom` to avoid repetition:

```csharp
// Instead of repeating the filter:
context.Orders.IncludePaths(
    o => o.LineItems.Where(li => li.IsActive).Each().Product,
    o => o.LineItems.Where(li => li.IsActive).Each().Discounts,
    o => o.LineItems.Where(li => li.IsActive).Each().Supplier)

// Use IncludeFrom - define the base once:
context.Orders.IncludeFrom(
    o => o.LineItems.Where(li => li.IsActive).Each(),
    li => li.Product,
    li => li.Discounts.Each(),
    li => li.Supplier.To().Address)
```

### Reference Navigations

Group paths from a reference navigation:

```csharp
context.Orders.IncludeFrom(
    o => o.Customer.To(),
    c => c.Address,
    c => c.PaymentMethods.Each())
```

### Multiple Groups

Chain multiple `IncludeFrom` calls:

```csharp
context.Orders
    .IncludeFrom(
        o => o.Customer.To(),
        c => c.Address,
        c => c.PaymentMethods.Each())
    .IncludeFrom(
        o => o.LineItems.Where(li => li.IsActive).Each(),
        li => li.Product!.Category,
        li => li.Discounts.Each())
    .IncludeFrom(
        o => o.Payments.Each(),
        p => p.PaymentMethod)
```

### Nested Filters in Sub-paths

Sub-paths support full nesting including filters:

```csharp
context.Orders.IncludeFrom(
    o => o.LineItems.Where(li => li.IsActive).Each(),
    li => li.Product!.Images.Where(i => i.IsPrimary).Each(),
    li => li.Product!.Tags.Where(t => t.Tag == "featured").Each())
```

### In Specifications

Use `IncludeFrom` in specs for clean, grouped includes:

```csharp
public class OrderDetailsSpec : IncludeSpec<Order>
{
    public OrderDetailsSpec()
    {
        IncludeFrom(
            o => o.Customer.To(),
            c => c.Address,
            c => c.PaymentMethods.Each());

        IncludeFrom(
            o => o.LineItems.Each(),
            li => li.Product!.Category,
            li => li.Discounts.Each());
    }
}
```

### Conditional

Use `IncludeFromIf` for conditional grouped includes:

```csharp
context.Orders
    .IncludeFromIf(includeProducts,
        o => o.LineItems.Each(),
        li => li.Product,
        li => li.Discounts.Each())
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
| `IncludeFrom(basePath, subPaths...)` | Group multiple paths from a common base |
| `IncludeFromIf(condition, basePath, subPaths...)` | Conditional grouped includes |
| `Each()` | Navigate through a collection in a path |
| `Where(predicate).Each()` | Filter a collection before navigating through it |
| `OrderBy(keySelector).Each()` | Order a collection before navigating through it |
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
| Filtered collection | `.Include(o => o.Items.Where(i => i.Active)).ThenInclude(i => i.Product)` | `.IncludePaths(o => o.Items.Where(i => i.Active).Each().Product)` |
| Ordered collection | `.Include(o => o.Items.OrderBy(i => i.Date)).ThenInclude(i => i.Product)` | `.IncludePaths(o => o.Items.OrderBy(i => i.Date).Each().Product)` |
| Deep nesting | 4+ lines of Include/ThenInclude | `.IncludePaths(o => o.Items.Each().Product.To().Category.To().Parent)` |
| Multiple branches | Repeat Include chain for each branch | List all paths in one `IncludePaths()` call |
| Grouped paths | Repeat filter for each sub-path | `.IncludeFrom(base, subPath1, subPath2, ...)` |

---

## Performance

EFCore.FluentIncludes generates **identical SQL** to standard EF Core includes. There is a small overhead for expression parsing:

| Scenario | Standard EF | FluentIncludes | Overhead |
|----------|-------------|----------------|----------|
| Single navigation | 2.3 μs | 3.1 μs | +31% |
| Two-level navigation | 3.7 μs | 4.9 μs | +34% |
| Deep navigation (4 levels) | 6.2 μs | 9.0 μs | +44% |
| Multiple paths | 7.6 μs | 10.9 μs | +44% |
| Complex scenario | 13.0 μs | 18.8 μs | +44% |

**Context:** A typical database query takes 1,000-50,000 μs. The overhead is <0.1% of total query time.

### .NET 10 Source Generation

On **.NET 10**, EFCore.FluentIncludes uses C# interceptors to generate direct `Include`/`ThenInclude` calls at compile time, eliminating runtime expression parsing entirely.

**How it works:**
- The source generator analyzes your `IncludePaths` calls during compilation
- For inline lambdas, it generates interceptors that call EF Core directly
- For expressions stored in variables, it falls back to runtime parsing with caching

**What you get:**
- Zero runtime reflection or expression parsing for inline lambdas
- Generated code is visible in `obj/Generated/` when `EmitCompilerGeneratedFiles` is enabled
- Automatic fallback ensures all scenarios work correctly

**Framework behavior:**

| Framework | Behavior |
|-----------|----------|
| .NET 10+ | Source-generated interceptors (eliminates parsing overhead) |
| .NET 8/9 | Runtime expression parsing with caching (existing behavior) |

No configuration required - the source generator activates automatically on .NET 10+.

Benchmarks run automatically in CI with a 50% overhead threshold to catch regressions.

- Full compatibility with `AsSplitQuery()` and `AsSingleQuery()`

---

## Compile-Time Analysis

EFCore.FluentIncludes includes a Roslyn analyzer that validates your include expressions at compile time, catching errors before they become runtime exceptions.

### What It Catches

The analyzer runs automatically when you install the package and provides immediate feedback in your IDE:

| ID | Severity | Description |
|----|----------|-------------|
| `FI0001` | Error | Property does not exist on type |
| `FI0002` | Error | Missing `Each()` on collection navigation |
| `FI0003` | Error | `Each()` used on non-collection property |
| `FI0004` | Error | `Where()` applied to non-collection |
| `FI0005` | Error | `OrderBy()` applied to non-collection |
| `FI0006` | Warning | `To()` used on non-nullable property (unnecessary) |
| `FI0007` | Warning | Nullable navigation without `To()` or `!` |
| `FI0008` | Error | Invalid property in filter predicate |
| `FI0009` | Error | Type mismatch in navigation chain |

### Examples

```csharp
// FI0001 - Typo in property name
.IncludePaths(o => o.Custmer.To().Address)  // Error: Property 'Custmer' does not exist

// FI0002 - Forgot Each() on collection
.IncludePaths(o => o.LineItems.Product)  // Error: Collection requires .Each()

// FI0007 - Nullable navigation without To()
.IncludePaths(o => o.Customer.Address)  // Warning: Use .To() or ! for nullable navigation
```

### Auto-Fixes

The analyzer includes code fixes for common issues. In your IDE, hover over the warning and use the lightbulb menu:

- **FI0006** (unnecessary `To()`) → Remove `.To()`
- **FI0007** (missing `To()`) → Add `.To()` after nullable property

### Configuration

Suppress warnings in `.editorconfig` if needed:

```ini
# Suppress specific rules
[*.cs]
dotnet_diagnostic.FI0006.severity = none  # Allow To() on non-nullable
dotnet_diagnostic.FI0007.severity = none  # Don't require To() on nullable
```

Or suppress inline:

```csharp
#pragma warning disable FI0007
.IncludePaths(o => o.Customer.Address)
#pragma warning restore FI0007
```

---

## Sample Project

A complete sample ASP.NET Core Minimal API project is available in [`samples/EFCore.FluentIncludes.Sample`](samples/EFCore.FluentIncludes.Sample). It demonstrates all library features with an e-commerce domain (orders, customers, products, categories).

```bash
# Run the sample
dotnet run --project samples/EFCore.FluentIncludes.Sample
```

---

## License

Apache 2.0 - See [LICENSE](LICENSE) for details.
