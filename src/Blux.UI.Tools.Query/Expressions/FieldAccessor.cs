using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Blux.UI.Tools.Query;

/// <summary>
/// A field of the row type as an expression (<c>e =&gt; e.Department.Name</c>). It can be bound to any parameter, either as written (for a database
/// provider, which treats a null navigation as SQL null) or null-safe (for in-memory evaluation, where a null <c>Department</c> must not throw).
/// The result is always "lifted": a non-nullable value type becomes <see cref="Nullable{T}"/> so that null compares uniformly.
/// </summary>
internal sealed class FieldAccessor
{
    private FieldAccessor(LambdaExpression lambda, Expression body, MemberInfo[]? chain, bool guardRoot)
    {
        GuardRoot = guardRoot;
        Lambda = lambda;
        Body = body;
        Chain = chain;
        Type = body.Type;
        UnderlyingType = Nullable.GetUnderlyingType(Type) ?? Type;
        LiftedType = Lift(Type);
        Path = chain is null ? null : string.Join('.', chain.Select(m => m.Name));
    }

    /// <summary>Whether the root of the chain (the lambda parameter) is null-checked too: true for the elements of a collection, which can be null.</summary>
    public bool GuardRoot { get; }

    public LambdaExpression Lambda { get; }

    /// <summary>The body without a boxing conversion.</summary>
    public Expression Body { get; }

    /// <summary>The property or field chain from the row to the value, or <see langword="null"/> when the body is something else (a call, an operator).</summary>
    public MemberInfo[]? Chain { get; }

    public Type Type { get; }

    /// <summary>The type without <see cref="Nullable{T}"/>.</summary>
    public Type UnderlyingType { get; }

    public Type LiftedType { get; }

    /// <summary>The members joined with dots (<c>Department.Name</c>), or <see langword="null"/>.</summary>
    public string? Path { get; }

    /// <summary>The default stable key: the path with each segment in camelCase (<c>department.name</c>), or <see langword="null"/> when the body is not a member chain.</summary>
    public string? DefaultKey => Path is null ? null : string.Join('.', Path.Split('.').Select(CamelCase));

    public static FieldAccessor Create(LambdaExpression lambda, bool guardRoot = false)
    {
        ArgumentNullException.ThrowIfNull(lambda);
        var body = lambda.Body;
        // A boxing or lifting conversion around a member access (e => (object)e.Age) is dropped: the value is lifted again on binding.
        while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u && u.Operand is MemberExpression)
            body = u.Operand;

        MemberInfo[]? chain = null;
        var members = new List<MemberInfo>();
        var current = body;
        while (current is MemberExpression member)
        {
            members.Add(member.Member);
            current = member.Expression;
        }
        if (members.Count > 0 && current == lambda.Parameters[0])
        {
            members.Reverse();
            chain = [.. members];
        }
        return new FieldAccessor(lambda, body, chain, guardRoot);
    }

    /// <summary>The expression of the field for <paramref name="parameter"/>, lifted; null-safe along the chain when <paramref name="nullSafe"/> and the body is a member chain.</summary>
    public Expression Bind(Expression parameter, bool nullSafe)
    {
        if (nullSafe && Chain is not null)
        {
            var guarded = Guard(parameter, 0);
            return guarded;
        }
        var raw = new ParameterReplacer(Lambda.Parameters[0], parameter).Visit(Body);
        return LiftedType == raw.Type ? raw : Expression.Convert(raw, LiftedType);
    }

    private Expression Guard(Expression current, int index)
    {
        var chain = Chain!;
        if (index == chain.Length)
            return LiftedType == current.Type ? current : Expression.Convert(current, LiftedType);
        // The parameter itself (index 0) is never null; every later link is checked before its members are read.
        if ((index > 0 || GuardRoot) && (!current.Type.IsValueType || Nullable.GetUnderlyingType(current.Type) is not null))
        {
            var next = Expression.MakeMemberAccess(current, chain[index]);
            return Expression.Condition(
                Expression.Equal(current, Expression.Constant(null, current.Type)),
                Expression.Default(LiftedType),
                Guard(next, index + 1));
        }
        return Guard(Expression.MakeMemberAccess(current, chain[index]), index + 1);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Nullable<T> over a value type that appears in a user expression; the type is already rooted by that expression.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Expression trees already require dynamic code; the library documents it.")]
    internal static Type Lift(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) is null ? typeof(Nullable<>).MakeGenericType(type) : type;

    internal static string CamelCase(string name) =>
        name.Length == 0 || char.IsLower(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    internal sealed class ParameterReplacer(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        [return: NotNullIfNotNull(nameof(node))]
        public override Expression? Visit(Expression? node) => base.Visit(node);

        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
