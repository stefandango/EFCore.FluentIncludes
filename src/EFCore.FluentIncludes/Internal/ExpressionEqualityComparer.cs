using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.FluentIncludes.Internal;

/// <summary>
/// Compares expressions by their structure for use as dictionary keys.
/// Two expressions are considered equal if they have the same structure and reference the same members/methods.
/// </summary>
internal sealed class ExpressionEqualityComparer : IEqualityComparer<LambdaExpression>
{
    public static ExpressionEqualityComparer Instance { get; } = new();

    private ExpressionEqualityComparer() { }

    public bool Equals(LambdaExpression? x, LambdaExpression? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // Compare parameter counts and types
        if (x.Parameters.Count != y.Parameters.Count) return false;
        for (int i = 0; i < x.Parameters.Count; i++)
        {
            if (x.Parameters[i].Type != y.Parameters[i].Type) return false;
        }

        // Compare return types
        if (x.ReturnType != y.ReturnType) return false;

        // Build parameter mapping (parameters at same position should be considered equal)
        var parameterMap = new Dictionary<ParameterExpression, ParameterExpression>();
        for (int i = 0; i < x.Parameters.Count; i++)
        {
            parameterMap[x.Parameters[i]] = y.Parameters[i];
        }

        return ExpressionsEqual(x.Body, y.Body, parameterMap);
    }

    public int GetHashCode(LambdaExpression obj)
    {
        if (obj is null) return 0;

        var hash = new HashCode();
        hash.Add(obj.NodeType);
        hash.Add(obj.ReturnType);
        hash.Add(obj.Parameters.Count);

        foreach (var param in obj.Parameters)
        {
            hash.Add(param.Type);
        }

        AddExpressionHashCode(obj.Body, ref hash);
        return hash.ToHashCode();
    }

    private static bool ExpressionsEqual(Expression? x, Expression? y, Dictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.NodeType != y.NodeType) return false;
        if (x.Type != y.Type) return false;

        return x switch
        {
            MemberExpression mx when y is MemberExpression my =>
                mx.Member == my.Member && ExpressionsEqual(mx.Expression, my.Expression, parameterMap),

            MethodCallExpression mcx when y is MethodCallExpression mcy =>
                MethodCallsEqual(mcx, mcy, parameterMap),

            ParameterExpression px when y is ParameterExpression py =>
                parameterMap.TryGetValue(px, out var mappedY) ? mappedY == py : px.Type == py.Type,

            UnaryExpression ux when y is UnaryExpression uy =>
                ux.Method == uy.Method && ExpressionsEqual(ux.Operand, uy.Operand, parameterMap),

            BinaryExpression bx when y is BinaryExpression by =>
                bx.Method == by.Method &&
                ExpressionsEqual(bx.Left, by.Left, parameterMap) &&
                ExpressionsEqual(bx.Right, by.Right, parameterMap),

            ConstantExpression cx when y is ConstantExpression cy =>
                Equals(cx.Value, cy.Value),

            LambdaExpression lx when y is LambdaExpression ly =>
                LambdasEqual(lx, ly, parameterMap),

            NewExpression nx when y is NewExpression ny =>
                nx.Constructor == ny.Constructor &&
                ArgumentsEqual(nx.Arguments, ny.Arguments, parameterMap),

            MemberInitExpression mix when y is MemberInitExpression miy =>
                MemberInitsEqual(mix, miy, parameterMap),

            ConditionalExpression condx when y is ConditionalExpression condy =>
                ExpressionsEqual(condx.Test, condy.Test, parameterMap) &&
                ExpressionsEqual(condx.IfTrue, condy.IfTrue, parameterMap) &&
                ExpressionsEqual(condx.IfFalse, condy.IfFalse, parameterMap),

            InvocationExpression ix when y is InvocationExpression iy =>
                ExpressionsEqual(ix.Expression, iy.Expression, parameterMap) &&
                ArgumentsEqual(ix.Arguments, iy.Arguments, parameterMap),

            _ => false
        };
    }

    private static bool MethodCallsEqual(
        MethodCallExpression x,
        MethodCallExpression y,
        Dictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (x.Method != y.Method) return false;
        if (!ExpressionsEqual(x.Object, y.Object, parameterMap)) return false;
        return ArgumentsEqual(x.Arguments, y.Arguments, parameterMap);
    }

    private static bool ArgumentsEqual(
        System.Collections.ObjectModel.ReadOnlyCollection<Expression> x,
        System.Collections.ObjectModel.ReadOnlyCollection<Expression> y,
        Dictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (x.Count != y.Count) return false;
        for (int i = 0; i < x.Count; i++)
        {
            if (!ExpressionsEqual(x[i], y[i], parameterMap)) return false;
        }
        return true;
    }

    private static bool LambdasEqual(
        LambdaExpression x,
        LambdaExpression y,
        Dictionary<ParameterExpression, ParameterExpression> outerParameterMap)
    {
        if (x.Parameters.Count != y.Parameters.Count) return false;
        if (x.ReturnType != y.ReturnType) return false;

        // Create a new parameter map that includes both outer parameters and this lambda's parameters
        var innerParameterMap = new Dictionary<ParameterExpression, ParameterExpression>(outerParameterMap);
        for (int i = 0; i < x.Parameters.Count; i++)
        {
            if (x.Parameters[i].Type != y.Parameters[i].Type) return false;
            innerParameterMap[x.Parameters[i]] = y.Parameters[i];
        }

        return ExpressionsEqual(x.Body, y.Body, innerParameterMap);
    }

    private static bool MemberInitsEqual(
        MemberInitExpression x,
        MemberInitExpression y,
        Dictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (x.NewExpression.Constructor != y.NewExpression.Constructor) return false;
        if (!ArgumentsEqual(x.NewExpression.Arguments, y.NewExpression.Arguments, parameterMap)) return false;
        if (x.Bindings.Count != y.Bindings.Count) return false;

        for (int i = 0; i < x.Bindings.Count; i++)
        {
            if (!MemberBindingsEqual(x.Bindings[i], y.Bindings[i], parameterMap)) return false;
        }
        return true;
    }

    private static bool MemberBindingsEqual(
        MemberBinding x,
        MemberBinding y,
        Dictionary<ParameterExpression, ParameterExpression> parameterMap)
    {
        if (x.BindingType != y.BindingType) return false;
        if (x.Member != y.Member) return false;

        return (x, y) switch
        {
            (MemberAssignment ax, MemberAssignment ay) =>
                ExpressionsEqual(ax.Expression, ay.Expression, parameterMap),
            (MemberMemberBinding mmx, MemberMemberBinding mmy) =>
                mmx.Bindings.Count == mmy.Bindings.Count &&
                mmx.Bindings.Zip(mmy.Bindings).All(pair => MemberBindingsEqual(pair.First, pair.Second, parameterMap)),
            (MemberListBinding mlx, MemberListBinding mly) =>
                mlx.Initializers.Count == mly.Initializers.Count &&
                mlx.Initializers.Zip(mly.Initializers).All(pair =>
                    pair.First.AddMethod == pair.Second.AddMethod &&
                    ArgumentsEqual(pair.First.Arguments, pair.Second.Arguments, parameterMap)),
            _ => false
        };
    }

    private static void AddExpressionHashCode(Expression? expr, ref HashCode hash)
    {
        if (expr is null) return;

        hash.Add(expr.NodeType);
        hash.Add(expr.Type);

        switch (expr)
        {
            case MemberExpression mx:
                hash.Add(mx.Member);
                AddExpressionHashCode(mx.Expression, ref hash);
                break;

            case MethodCallExpression mcx:
                hash.Add(mcx.Method);
                AddExpressionHashCode(mcx.Object, ref hash);
                foreach (var arg in mcx.Arguments)
                {
                    AddExpressionHashCode(arg, ref hash);
                }
                break;

            case ParameterExpression:
                // Only use type (already added above) - name can differ
                break;

            case UnaryExpression ux:
                hash.Add(ux.Method);
                AddExpressionHashCode(ux.Operand, ref hash);
                break;

            case BinaryExpression bx:
                hash.Add(bx.Method);
                AddExpressionHashCode(bx.Left, ref hash);
                AddExpressionHashCode(bx.Right, ref hash);
                break;

            case ConstantExpression cx:
                hash.Add(cx.Value);
                break;

            case LambdaExpression lx:
                foreach (var param in lx.Parameters)
                {
                    hash.Add(param.Type);
                }
                AddExpressionHashCode(lx.Body, ref hash);
                break;

            case NewExpression nx:
                hash.Add(nx.Constructor);
                foreach (var arg in nx.Arguments)
                {
                    AddExpressionHashCode(arg, ref hash);
                }
                break;

            case ConditionalExpression condx:
                AddExpressionHashCode(condx.Test, ref hash);
                AddExpressionHashCode(condx.IfTrue, ref hash);
                AddExpressionHashCode(condx.IfFalse, ref hash);
                break;
        }
    }
}
