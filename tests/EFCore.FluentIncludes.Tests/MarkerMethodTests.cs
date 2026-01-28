namespace EFCore.FluentIncludes.Tests;

/// <summary>
/// Tests for marker extension methods (Each, To) that should never be called directly.
/// These methods exist only for expression tree analysis and throw if invoked at runtime.
/// </summary>
public class MarkerMethodTests
{
    #region Each() Method Tests

    [Fact]
    public void Each_OnIEnumerable_ThrowsInvalidOperationException()
    {
        IEnumerable<string> collection = ["a", "b", "c"];

        var action = () => collection.Each();

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Each()");
        ex.Message.ShouldContain("marker");
    }

    [Fact]
    public void Each_OnICollection_ThrowsInvalidOperationException()
    {
        ICollection<string> collection = new List<string> { "a", "b", "c" };

        var action = () => collection.Each();

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Each()");
        ex.Message.ShouldContain("marker");
    }

    [Fact]
    public void Each_OnIList_ThrowsInvalidOperationException()
    {
        IList<string> list = ["a", "b", "c"];

        var action = () => list.Each();

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Each()");
        ex.Message.ShouldContain("marker");
    }

    [Fact]
    public void Each_OnList_ThrowsInvalidOperationException()
    {
        List<string> list = ["a", "b", "c"];

        var action = () => list.Each();

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("Each()");
        ex.Message.ShouldContain("marker");
    }

    #endregion

    #region To() Method Tests

    [Fact]
    public void To_OnNullableReference_ThrowsInvalidOperationException()
    {
        string? value = "test";

        var action = () => value.To();

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("To()");
        ex.Message.ShouldContain("marker");
    }

    [Fact]
    public void To_OnNullValue_ThrowsInvalidOperationException()
    {
        string? value = null;

        var action = () => value.To();

        var ex = action.ShouldThrow<InvalidOperationException>();
        ex.Message.ShouldContain("To()");
        ex.Message.ShouldContain("marker");
    }

    #endregion
}
