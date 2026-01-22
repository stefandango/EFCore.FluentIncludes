namespace EFCore.FluentIncludes.Tests.TestEntities;

public class PaymentMethod
{
    public int Id { get; set; }
    public required string Type { get; set; } // Credit, Debit, PayPal, etc.
    public required string LastFourDigits { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
