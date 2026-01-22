namespace EFCore.FluentIncludes.Tests.TestEntities;

public class Order
{
    public int Id { get; set; }
    public required string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }

    // Single navigations
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? ShippingAddressId { get; set; }
    public Address? ShippingAddress { get; set; }

    public int? BillingAddressId { get; set; }
    public Address? BillingAddress { get; set; }

    // Collection navigations
    public ICollection<LineItem> LineItems { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<OrderNote> Notes { get; set; } = [];
}
