namespace EFCore.FluentIncludes.Tests.TestEntities;

public class LineItemDiscount
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public decimal Amount { get; set; }

    public int LineItemId { get; set; }
    public LineItem? LineItem { get; set; }
}
