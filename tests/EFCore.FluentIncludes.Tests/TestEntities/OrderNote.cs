namespace EFCore.FluentIncludes.Tests.TestEntities;

public class OrderNote
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int? AuthorId { get; set; }
    public Customer? Author { get; set; }
}
