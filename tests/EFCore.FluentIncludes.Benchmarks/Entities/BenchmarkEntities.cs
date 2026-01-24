namespace EFCore.FluentIncludes.Benchmarks.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<LineItem> LineItems { get; set; } = [];
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int? AddressId { get; set; }
    public Address? Address { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}

public class Address
{
    public int Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class LineItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public ICollection<ProductTag> Tags { get; set; } = [];
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public ICollection<Category> SubCategories { get; set; } = [];
}

public class ProductTag
{
    public int Id { get; set; }
    public string Tag { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product? Product { get; set; }
}
