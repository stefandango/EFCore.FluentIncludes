# EFCore.FluentIncludes

[![codecov](https://codecov.io/gh/stefandango/EFCore.FluentIncludes/graph/badge.svg)](https://codecov.io/gh/stefandango/EFCore.FluentIncludes)

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
    .FirstOrDefaultAsync(o => o.Id == id);
```

With **EFCore.FluentIncludes**, the same query becomes:

```csharp
// Clear and scannable
var order = await context.Orders
    .IncludePaths(
        o => o.Customer.To().Address,
        o => o.Customer.To().PaymentMethods,
        o => o.LineItems.Each().Product.To().Category)
    .FirstOrDefaultAsync(o => o.Id == id);
```

## Installation

```bash
dotnet add package EFCore.FluentIncludes
```

**Requirements:** EF Core 8.0+, .NET 8.0+

```csharp
using EFCore.FluentIncludes;
```

## Quick Reference

| Method | When to Use |
|--------|-------------|
| `.To()` | Navigate through a **reference** (single entity, especially if nullable) |
| `.Each()` | Navigate through a **collection** (one-to-many) |
| `.Where(predicate).Each()` | Filter a collection before including |
| `.OrderBy(key).Each()` | Order a collection before including |

**Simple rule:** Use `To()` for "has one", use `Each()` for "has many".

## Usage

### Basic Paths

```csharp
// Single property
.IncludePaths(o => o.Customer)

// Chain with To() for references
.IncludePaths(o => o.Customer.To().Address)

// Chain with Each() for collections
.IncludePaths(o => o.LineItems.Each().Product)

// Combine both
.IncludePaths(o => o.LineItems.Each().Product.To().Category)

// Multiple paths at once
.IncludePaths(
    o => o.Customer.To().Address,
    o => o.LineItems.Each().Product.To().Category,
    o => o.Payments.Each().PaymentMethod)
```

### Filtering and Ordering Collections

Filter and order collections during eager loading:

```csharp
// Filter: only active items
.IncludePaths(o => o.LineItems.Where(li => li.IsActive).Each().Product)

// Order: by display order
.IncludePaths(o => o.LineItems.OrderBy(li => li.DisplayOrder).Each().Product)

// Combine: filter then order
.IncludePaths(o => o.LineItems
    .Where(li => li.IsActive)
    .OrderBy(li => li.DisplayOrder)
    .Each()
    .Product)

// Multiple sort keys
.IncludePaths(o => o.LineItems
    .OrderBy(li => li.Category)
    .ThenByDescending(li => li.DisplayOrder)
    .Each())
```

### Alternative: The `!` Operator

You can use `!` instead of `To()` for nullable navigation:

```csharp
.IncludePaths(o => o.Customer!.Address)  // Same as o => o.Customer.To().Address
```

Both are safe - the lambda is never executed. `To()` is more readable and consistent with `Each()`.

---

## Advanced Features

### Conditional Includes

Include paths based on runtime conditions:

```csharp
var order = await context.Orders
    .IncludePaths(o => o.Customer)
    .IncludePathsIf(includeProducts,
        o => o.LineItems.Each().Product.To().Category)
    .IncludePathsIf(includePayments,
        o => o.Payments.Each().PaymentMethod)
    .FirstOrDefaultAsync(o => o.Id == id);
```

Use cases: feature flags, user permissions, API query parameters.

### Grouping Paths with `IncludeFrom`

When multiple paths share the same base, avoid repetition:

```csharp
// Instead of repeating the filter:
.IncludePaths(
    o => o.LineItems.Where(li => li.IsActive).Each().Product,
    o => o.LineItems.Where(li => li.IsActive).Each().Discounts)

// Group them:
.IncludeFrom(
    o => o.LineItems.Where(li => li.IsActive).Each(),
    li => li.Product,
    li => li.Discounts.Each())
```

Also works with `IncludeFromIf()` for conditional grouped includes.

### Reusable Specifications

Create reusable include patterns:

```csharp
public class OrderDetailSpec : IncludeSpec<Order>
{
    public OrderDetailSpec()
    {
        Include(o => o.Customer.To().Address);
        Include(o => o.LineItems.Each().Product.To().Category);
    }
}

// Use it
var orders = await context.Orders
    .WithSpec<Order, OrderDetailSpec>()
    .ToListAsync();
```

**Compose specs:**

```csharp
public class OrderFullSpec : IncludeSpec<Order>
{
    public OrderFullSpec()
    {
        IncludeFrom<OrderDetailSpec>();  // Include everything from another spec
        Include(o => o.Payments.Each());
    }
}
```

**Multiple specs:** `.WithSpecs<Order, OrderDetailSpec, OrderAuditSpec>()`

**Conditional:** `.WithSpecIf<Order, OrderDetailSpec>(condition)`

---

## API Reference

| Method | Purpose |
|--------|---------|
| `IncludePaths(paths...)` | Include one or more navigation paths |
| `IncludePathsIf(condition, paths...)` | Include paths only when condition is true |
| `IncludeFrom(basePath, subPaths...)` | Group multiple paths from a common base |
| `IncludeFromIf(condition, basePath, subPaths...)` | Conditional grouped includes |
| `Each()` | Navigate through a collection |
| `Where(predicate).Each()` | Filter a collection |
| `OrderBy(key).Each()` | Order a collection |
| `To()` | Navigate through a reference property |
| `WithSpec<TEntity, TSpec>()` | Apply a reusable specification |
| `WithSpecs<TEntity, TSpec1, TSpec2>()` | Apply multiple specifications |
| `WithSpecIf<TEntity, TSpec>(condition)` | Apply spec only when condition is true |

### Before & After

| Scenario | Standard EF Core | EFCore.FluentIncludes |
|----------|------------------|-------------------|
| Nested property | `.Include(o => o.Customer).ThenInclude(c => c.Address)` | `.IncludePaths(o => o.Customer.To().Address)` |
| Through collection | `.Include(o => o.Items).ThenInclude(i => i.Product)` | `.IncludePaths(o => o.Items.Each().Product)` |
| Filtered collection | `.Include(o => o.Items.Where(i => i.Active))...` | `.IncludePaths(o => o.Items.Where(i => i.Active).Each()...)` |
| Deep nesting | 4+ lines of Include/ThenInclude | Single path expression |

---

## Performance

EFCore.FluentIncludes generates **identical SQL** to standard EF Core includes. Expression parsing adds microseconds of overhead - negligible compared to database query time.

On **.NET 10+**, source generation eliminates parsing overhead entirely using C# interceptors.

Works with `AsSplitQuery()` and `AsSingleQuery()`.

## Compile-Time Analysis

The included Roslyn analyzer catches common errors at compile time:

- `FI0001` - Property does not exist
- `FI0002` - Missing `Each()` on collection
- `FI0003` - `Each()` on non-collection
- `FI0007` - Nullable navigation without `To()` or `!`

Auto-fixes are available for common issues.

## Sample Project

See [`samples/EFCore.FluentIncludes.Sample`](samples/EFCore.FluentIncludes.Sample) for a complete ASP.NET Core example.

```bash
dotnet run --project samples/EFCore.FluentIncludes.Sample
```

## License

Apache 2.0 - See [LICENSE](LICENSE) for details.
