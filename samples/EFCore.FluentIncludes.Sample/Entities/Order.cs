namespace EFCore.FluentIncludes.Sample.Entities;

public class Order
{
    public int Id { get; set; }
    public required string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<LineItem> LineItems { get; set; } = [];
}
