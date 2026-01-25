using EFCore.FluentIncludes.Sample.Entities;

namespace EFCore.FluentIncludes.Sample.Data;

public static class SampleDataSeeder
{
    public static void Seed(SampleDbContext context)
    {
        if (context.Categories.Any())
            return;

        // Categories (with hierarchy)
        var electronics = new Category { Name = "Electronics" };
        var computers = new Category { Name = "Computers", ParentCategory = electronics };
        var phones = new Category { Name = "Phones", ParentCategory = electronics };
        var clothing = new Category { Name = "Clothing" };
        var menswear = new Category { Name = "Menswear", ParentCategory = clothing };

        context.Categories.AddRange(electronics, computers, phones, clothing, menswear);

        // Products
        var laptop = new Product { Name = "Laptop Pro", Price = 1299.99m, Category = computers };
        var desktop = new Product { Name = "Desktop Workstation", Price = 1899.99m, Category = computers };
        var smartphone = new Product { Name = "Smartphone X", Price = 999.99m, Category = phones };
        var tablet = new Product { Name = "Tablet Plus", Price = 599.99m, Category = phones };
        var shirt = new Product { Name = "Classic Shirt", Price = 49.99m, Category = menswear };
        var jacket = new Product { Name = "Winter Jacket", Price = 199.99m, Category = menswear };

        context.Products.AddRange(laptop, desktop, smartphone, tablet, shirt, jacket);

        // Addresses
        var address1 = new Address { Street = "123 Main St", City = "New York", Country = "USA" };
        var address2 = new Address { Street = "456 Oak Ave", City = "Los Angeles", Country = "USA" };
        var address3 = new Address { Street = "789 Pine Rd", City = "London", Country = "UK" };

        context.Addresses.AddRange(address1, address2, address3);

        // Customers
        var customer1 = new Customer { Name = "John Doe", Email = "john@example.com", Address = address1 };
        var customer2 = new Customer { Name = "Jane Smith", Email = "jane@example.com", Address = address2 };
        var customer3 = new Customer { Name = "Bob Wilson", Email = "bob@example.com", Address = address3 };

        context.Customers.AddRange(customer1, customer2, customer3);

        // Orders with LineItems
        var order1 = new Order
        {
            OrderNumber = "ORD-001",
            OrderDate = DateTime.UtcNow.AddDays(-5),
            Customer = customer1,
            LineItems =
            [
                new LineItem { Product = laptop, Quantity = 1, UnitPrice = 1299.99m },
                new LineItem { Product = smartphone, Quantity = 2, UnitPrice = 999.99m }
            ]
        };
        order1.TotalAmount = order1.LineItems.Sum(li => li.Quantity * li.UnitPrice);

        var order2 = new Order
        {
            OrderNumber = "ORD-002",
            OrderDate = DateTime.UtcNow.AddDays(-3),
            Customer = customer1,
            LineItems =
            [
                new LineItem { Product = shirt, Quantity = 3, UnitPrice = 49.99m },
                new LineItem { Product = jacket, Quantity = 1, UnitPrice = 199.99m }
            ]
        };
        order2.TotalAmount = order2.LineItems.Sum(li => li.Quantity * li.UnitPrice);

        var order3 = new Order
        {
            OrderNumber = "ORD-003",
            OrderDate = DateTime.UtcNow.AddDays(-1),
            Customer = customer2,
            LineItems =
            [
                new LineItem { Product = desktop, Quantity = 1, UnitPrice = 1899.99m },
                new LineItem { Product = tablet, Quantity = 1, UnitPrice = 599.99m }
            ]
        };
        order3.TotalAmount = order3.LineItems.Sum(li => li.Quantity * li.UnitPrice);

        var order4 = new Order
        {
            OrderNumber = "ORD-004",
            OrderDate = DateTime.UtcNow,
            Customer = customer3,
            LineItems =
            [
                new LineItem { Product = smartphone, Quantity = 1, UnitPrice = 999.99m }
            ]
        };
        order4.TotalAmount = order4.LineItems.Sum(li => li.Quantity * li.UnitPrice);

        context.Orders.AddRange(order1, order2, order3, order4);

        context.SaveChanges();
    }
}
