using EFCore.FluentIncludes.Tests.TestEntities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCore.FluentIncludes.Tests.Fixtures;

public class DatabaseFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _options;

    public DatabaseFixture()
    {
        // Keep connection open to preserve in-memory database
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
        SeedData(context);
    }

    public TestDbContext CreateContext()
    {
        return new TestDbContext(_options);
    }

    private static void SeedData(TestDbContext context)
    {
        // Addresses
        var address1 = new Address { Id = 1, Street = "123 Main St", City = "New York", Country = "USA", PostalCode = "10001" };
        var address2 = new Address { Id = 2, Street = "456 Oak Ave", City = "Los Angeles", Country = "USA", PostalCode = "90001" };
        var address3 = new Address { Id = 3, Street = "789 Pine Rd", City = "Chicago", Country = "USA", PostalCode = "60601" };
        var address4 = new Address { Id = 4, Street = "321 Elm St", City = "Seattle", Country = "USA", PostalCode = "98101" };
        var supplierAddress = new Address { Id = 5, Street = "999 Industrial Blvd", City = "Detroit", Country = "USA", PostalCode = "48201" };

        context.Addresses.AddRange(address1, address2, address3, address4, supplierAddress);

        // Categories (with hierarchy: Electronics > Phones > Smartphones)
        var electronics = new Category { Id = 1, Name = "Electronics" };
        var phones = new Category { Id = 2, Name = "Phones", ParentCategoryId = 1 };
        var smartphones = new Category { Id = 3, Name = "Smartphones", ParentCategoryId = 2 };
        var accessories = new Category { Id = 4, Name = "Accessories", ParentCategoryId = 1 };

        context.Categories.AddRange(electronics, phones, smartphones, accessories);

        // Suppliers
        var supplier1 = new Supplier { Id = 1, Name = "TechSupply Co", ContactEmail = "contact@techsupply.com", AddressId = 5 };
        var supplier2 = new Supplier { Id = 2, Name = "GadgetWorld", ContactEmail = "sales@gadgetworld.com" };

        context.Suppliers.AddRange(supplier1, supplier2);

        // Products
        var product1 = new Product { Id = 1, Name = "iPhone 15", Sku = "APPL-IP15", Price = 999.99m, CategoryId = 3, SupplierId = 1 };
        var product2 = new Product { Id = 2, Name = "Samsung Galaxy S24", Sku = "SAMS-GS24", Price = 899.99m, CategoryId = 3, SupplierId = 1 };
        var product3 = new Product { Id = 3, Name = "Phone Case", Sku = "ACC-CASE1", Price = 29.99m, CategoryId = 4, SupplierId = 2 };
        // Product with no supplier (for null mid-path tests)
        var product4 = new Product { Id = 4, Name = "Generic Item", Sku = "GEN-001", Price = 9.99m, CategoryId = 4, SupplierId = null };

        context.Products.AddRange(product1, product2, product3, product4);

        // Product Images
        context.ProductImages.AddRange(
            new ProductImage { Id = 1, Url = "https://example.com/iphone15-1.jpg", IsPrimary = true, ProductId = 1 },
            new ProductImage { Id = 2, Url = "https://example.com/iphone15-2.jpg", IsPrimary = false, ProductId = 1 },
            new ProductImage { Id = 3, Url = "https://example.com/galaxy-1.jpg", IsPrimary = true, ProductId = 2 },
            new ProductImage { Id = 4, Url = "https://example.com/case-1.jpg", IsPrimary = true, ProductId = 3 }
        );

        // Product Tags
        context.ProductTags.AddRange(
            new ProductTag { Id = 1, Tag = "premium", ProductId = 1 },
            new ProductTag { Id = 2, Tag = "bestseller", ProductId = 1 },
            new ProductTag { Id = 3, Tag = "premium", ProductId = 2 },
            new ProductTag { Id = 4, Tag = "budget", ProductId = 3 }
        );

        // Customers
        var customer1 = new Customer { Id = 1, Name = "John Doe", Email = "john@example.com", AddressId = 1 };
        var customer2 = new Customer { Id = 2, Name = "Jane Smith", Email = "jane@example.com", AddressId = 2 };
        // Customer with no address (for null navigation tests)
        var customer3 = new Customer { Id = 3, Name = "No Address Customer", Email = "noaddr@example.com", AddressId = null };

        context.Customers.AddRange(customer1, customer2, customer3);

        // Payment Methods
        var paymentMethod1 = new PaymentMethod { Id = 1, Type = "Credit", LastFourDigits = "1234", CustomerId = 1 };
        var paymentMethod2 = new PaymentMethod { Id = 2, Type = "PayPal", LastFourDigits = "5678", CustomerId = 1 };
        var paymentMethod3 = new PaymentMethod { Id = 3, Type = "Debit", LastFourDigits = "9012", CustomerId = 2 };

        context.PaymentMethods.AddRange(paymentMethod1, paymentMethod2, paymentMethod3);

        // Orders
        var order1 = new Order
        {
            Id = 1,
            OrderNumber = "ORD-001",
            OrderDate = new DateTime(2024, 1, 15),
            TotalAmount = 1029.98m,
            CustomerId = 1,
            ShippingAddressId = 1,
            BillingAddressId = 1
        };

        var order2 = new Order
        {
            Id = 2,
            OrderNumber = "ORD-002",
            OrderDate = new DateTime(2024, 1, 20),
            TotalAmount = 929.98m,
            CustomerId = 1,
            ShippingAddressId = 3,
            BillingAddressId = 1
        };

        var order3 = new Order
        {
            Id = 3,
            OrderNumber = "ORD-003",
            OrderDate = new DateTime(2024, 2, 1),
            TotalAmount = 899.99m,
            CustomerId = 2,
            ShippingAddressId = 2,
            BillingAddressId = 2
        };

        // Order for customer with no address (for null navigation tests)
        var order4 = new Order
        {
            Id = 4,
            OrderNumber = "ORD-004",
            OrderDate = new DateTime(2024, 2, 15),
            TotalAmount = 19.98m,
            CustomerId = 3,
            ShippingAddressId = 4,
            BillingAddressId = 4
        };

        context.Orders.AddRange(order1, order2, order3, order4);

        // Line Items
        context.LineItems.AddRange(
            new LineItem { Id = 1, Quantity = 1, UnitPrice = 999.99m, OrderId = 1, ProductId = 1 },
            new LineItem { Id = 2, Quantity = 1, UnitPrice = 29.99m, OrderId = 1, ProductId = 3 },
            new LineItem { Id = 3, Quantity = 1, UnitPrice = 899.99m, OrderId = 2, ProductId = 2 },
            new LineItem { Id = 4, Quantity = 1, UnitPrice = 29.99m, OrderId = 2, ProductId = 3 },
            new LineItem { Id = 5, Quantity = 1, UnitPrice = 899.99m, OrderId = 3, ProductId = 2 },
            // Line items for null navigation tests
            new LineItem { Id = 6, Quantity = 1, UnitPrice = 9.99m, OrderId = 4, ProductId = 4 }, // Product with no supplier
            new LineItem { Id = 7, Quantity = 1, UnitPrice = 9.99m, OrderId = 4, ProductId = 3 }  // Product with supplier (no address)
        );

        // Line Item Discounts
        context.LineItemDiscounts.AddRange(
            new LineItemDiscount { Id = 1, Code = "WELCOME10", Amount = 100.00m, LineItemId = 1 },
            new LineItemDiscount { Id = 2, Code = "BUNDLE5", Amount = 5.00m, LineItemId = 2 }
        );

        // Payments
        context.Payments.AddRange(
            new Payment { Id = 1, Amount = 1029.98m, PaymentDate = new DateTime(2024, 1, 15), OrderId = 1, PaymentMethodId = 1 },
            new Payment { Id = 2, Amount = 929.98m, PaymentDate = new DateTime(2024, 1, 20), OrderId = 2, PaymentMethodId = 2 },
            new Payment { Id = 3, Amount = 899.99m, PaymentDate = new DateTime(2024, 2, 1), OrderId = 3, PaymentMethodId = 3 }
        );

        // Order Notes
        context.OrderNotes.AddRange(
            new OrderNote { Id = 1, Content = "Please leave at door", CreatedAt = new DateTime(2024, 1, 15), OrderId = 1, AuthorId = 1 },
            new OrderNote { Id = 2, Content = "Gift wrapping requested", CreatedAt = new DateTime(2024, 1, 20), OrderId = 2, AuthorId = 1 },
            new OrderNote { Id = 3, Content = "Express delivery", CreatedAt = new DateTime(2024, 2, 1), OrderId = 3, AuthorId = 2 }
        );

        context.SaveChanges();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
