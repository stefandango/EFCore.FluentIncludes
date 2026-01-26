using System.Collections.Immutable;
using EFCore.FluentIncludes.Analyzers.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EFCore.FluentIncludes.Generator.Tests;

/// <summary>
/// Helper class for testing the FluentIncludes source generator.
/// </summary>
public static class GeneratorTestHelper
{
    /// <summary>
    /// Runs the generator on the given source code and returns the generated output.
    /// </summary>
    public static GeneratorDriverRunResult RunGenerator(string source, bool isNet10 = true)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IQueryable<>).Assembly.Location),
        };

        // Add System.Runtime reference
        var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        // Add System.Collections reference
        var collectionsAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Collections");
        if (collectionsAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(collectionsAssembly.Location));
        }

        // Add System.Linq.Expressions reference
        var expressionsAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Linq.Expressions");
        if (expressionsAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(expressionsAssembly.Location));
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new FluentIncludesGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return driver.GetRunResult();
    }

    /// <summary>
    /// Gets the generated source text from the run result.
    /// </summary>
    public static string? GetGeneratedSource(GeneratorDriverRunResult result)
    {
        var generatedTree = result.Results
            .SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(s => s.HintName == "FluentIncludesInterceptors.g.cs");

        return generatedTree.SourceText?.ToString();
    }

    /// <summary>
    /// Common test code preamble with FluentIncludes library signatures.
    /// </summary>
    public const string TestCodePreamble = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Linq.Expressions;

        namespace EFCore.FluentIncludes
        {
            public static class QueryableExtensions
            {
                public static IQueryable<TEntity> IncludePaths<TEntity>(
                    this IQueryable<TEntity> source,
                    params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
                    => source;

                public static IQueryable<TEntity> IncludePath<TEntity, TProperty>(
                    this IQueryable<TEntity> source,
                    Expression<Func<TEntity, TProperty>> path) where TEntity : class
                    => source;

                public static IQueryable<TEntity> IncludePathsIf<TEntity>(
                    this IQueryable<TEntity> source,
                    bool condition,
                    params Expression<Func<TEntity, object?>>[] paths) where TEntity : class
                    => source;

                public static IQueryable<TEntity> IncludeFrom<TEntity, TNav>(
                    this IQueryable<TEntity> source,
                    Expression<Func<TEntity, TNav>> basePath,
                    params Expression<Func<TNav, object?>>[] subPaths) where TEntity : class
                    => source;

                public static IQueryable<TEntity> IncludeFromIf<TEntity, TNav>(
                    this IQueryable<TEntity> source,
                    bool condition,
                    Expression<Func<TEntity, TNav>> basePath,
                    params Expression<Func<TNav, object?>>[] subPaths) where TEntity : class
                    => source;
            }

            public static class CollectionExtensions
            {
                public static T Each<T>(this IEnumerable<T> source) => throw new InvalidOperationException();
            }

            public static class NavigationExtensions
            {
                public static T To<T>(this T? value) where T : class => throw new InvalidOperationException();
            }
        }

        namespace TestNamespace
        {
            using EFCore.FluentIncludes;

            public class Order
            {
                public int Id { get; set; }
                public Customer? Customer { get; set; }
                public ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();
            }

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public Address? Address { get; set; }
            }

            public class Address
            {
                public int Id { get; set; }
                public string Street { get; set; } = "";
            }

            public class LineItem
            {
                public int Id { get; set; }
                public bool IsActive { get; set; }
                public Product? Product { get; set; }
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public Category? Category { get; set; }
            }

            public class Category
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
            }
        """;
}
