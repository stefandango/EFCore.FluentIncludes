using Microsoft.CodeAnalysis;

namespace EFCore.FluentIncludes.Analyzers.Diagnostics;

/// <summary>
/// Diagnostic descriptors for FluentIncludes analyzer rules.
/// </summary>
public static class DiagnosticDescriptors
{
    private const string Category = "EFCore.FluentIncludes";

    /// <summary>
    /// FI0001: Property does not exist on type.
    /// </summary>
    public static readonly DiagnosticDescriptor PropertyNotFound = new(
        id: "FI0001",
        title: "Property not found",
        messageFormat: "Property '{0}' does not exist on type '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified property does not exist on the source type. Check for typos in the property name.");

    /// <summary>
    /// FI0002: Missing Each() on collection navigation.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingEachOnCollection = new(
        id: "FI0002",
        title: "Missing Each() on collection",
        messageFormat: "Collection property '{0}' requires '.Each()' to navigate into its elements",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When navigating through a collection property to access properties on its elements, you must use .Each() after the collection property.");

    /// <summary>
    /// FI0003: Each() used on non-collection property.
    /// </summary>
    public static readonly DiagnosticDescriptor EachOnNonCollection = new(
        id: "FI0003",
        title: "Each() on non-collection",
        messageFormat: "'.Each()' can only be used on collection properties, but '{0}' is not a collection",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The .Each() method is only valid for collection navigation properties (IEnumerable<T>, ICollection<T>, etc.).");

    /// <summary>
    /// FI0004: Where() applied to non-collection.
    /// </summary>
    public static readonly DiagnosticDescriptor WhereOnNonCollection = new(
        id: "FI0004",
        title: "Where() on non-collection",
        messageFormat: "'.Where()' can only be used on collection properties, but '{0}' is not a collection",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Filtered includes using .Where() are only valid for collection navigation properties.");

    /// <summary>
    /// FI0005: OrderBy() applied to non-collection.
    /// </summary>
    public static readonly DiagnosticDescriptor OrderByOnNonCollection = new(
        id: "FI0005",
        title: "OrderBy() on non-collection",
        messageFormat: "'.{0}()' can only be used on collection properties",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Ordering methods (OrderBy, OrderByDescending, ThenBy, ThenByDescending) are only valid for collection navigation properties.");

    /// <summary>
    /// FI0006: To() used on non-nullable property (unnecessary).
    /// </summary>
    public static readonly DiagnosticDescriptor UnnecessaryTo = new(
        id: "FI0006",
        title: "Unnecessary To()",
        messageFormat: "'.To()' is unnecessary on non-nullable property '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The .To() method is used for nullable navigation properties. Using it on a non-nullable property is unnecessary.");

    /// <summary>
    /// FI0007: Nullable navigation without To() or null-forgiving operator.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingToOnNullable = new(
        id: "FI0007",
        title: "Missing To() on nullable navigation",
        messageFormat: "Nullable property '{0}' should use '.To()' or '!' to indicate intentional navigation",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When navigating through a nullable property, use .To() or the null-forgiving operator (!) to make the intent explicit.");

    /// <summary>
    /// FI0008: Filter predicate references invalid property.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFilterProperty = new(
        id: "FI0008",
        title: "Invalid property in filter",
        messageFormat: "Property '{0}' does not exist on type '{1}' in filter expression",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The filter predicate references a property that does not exist on the collection element type.");

    /// <summary>
    /// FI0009: Type mismatch in navigation chain.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeMismatch = new(
        id: "FI0009",
        title: "Type mismatch in navigation",
        messageFormat: "Type mismatch: expected '{0}' but got '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The navigation chain has a type mismatch that would cause a runtime error.");
}
