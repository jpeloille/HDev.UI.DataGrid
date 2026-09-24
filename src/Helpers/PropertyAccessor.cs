using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace HDev.UI.DataGrid.Helpers;

/// <summary>
/// High-performance property access using compiled expression-tree delegates,
/// cached per (runtime type, member path). Replaces per-access reflection in
/// sorting, filtering, grouping and cell value access.
/// <para>
/// Supports nested paths ("Customer.Address.City"); each segment is resolved by
/// the <em>runtime</em> type of the intermediate value, so heterogeneous and
/// polymorphic collections sort/filter correctly.
/// </para>
/// </summary>
public static class PropertyAccessor
{
    private const BindingFlags Flags =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

    private static readonly ConcurrentDictionary<(Type Type, string Path), Func<object, object?>?> Getters = new();
    private static readonly ConcurrentDictionary<(Type Type, string Member), SetterInfo?> Setters = new();

    private readonly record struct SetterInfo(Action<object, object?> Set, Type PropertyType);

    /// <summary>
    /// Returns a cached getter for <paramref name="path"/> on <paramref name="type"/>,
    /// or <c>null</c> when the (first) member does not exist on the type.
    /// </summary>
    public static Func<object, object?>? GetGetter(Type type, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return Getters.GetOrAdd((type, path), static key => BuildGetter(key.Type, key.Path));
    }

    /// <summary>
    /// Gets the value at <paramref name="path"/> from <paramref name="item"/>.
    /// Returns <c>null</c> if the item is null, a member is missing, or an
    /// intermediate value in the path is null.
    /// </summary>
    public static object? GetValue(object? item, string path)
    {
        if (item == null) return null;
        return GetGetter(item.GetType(), path)?.Invoke(item);
    }

    /// <summary>
    /// Sets the value at <paramref name="path"/> on <paramref name="item"/>,
    /// converting <paramref name="value"/> to the target property type (handling
    /// nullable, enum and culture-aware conversions). Returns false if the path
    /// cannot be resolved, the property is read-only, or conversion fails.
    /// </summary>
    public static bool TrySetValue(object? item, string path, object? value)
    {
        if (item == null || string.IsNullOrEmpty(path)) return false;

        var segments = path.Split('.');
        object target = item;

        // Walk to the parent object of the final segment.
        for (int i = 0; i < segments.Length - 1; i++)
        {
            var getter = GetGetter(target.GetType(), segments[i]);
            var next = getter?.Invoke(target);
            if (next == null) return false;
            target = next;
        }

        var info = GetSetter(target.GetType(), segments[^1]);
        if (info == null) return false;

        if (!TryConvert(value, info.Value.PropertyType, out var converted)) return false;

        info.Value.Set(target, converted);
        return true;
    }

    #region Building

    private static Func<object, object?>? BuildGetter(Type type, string path)
    {
        var segments = path.Split('.');

        if (segments.Length == 1)
            return BuildSingleGetter(type, segments[0]);

        // Nested: resolve each step by the intermediate's runtime type so the
        // chain works for polymorphic graphs. Step getters are themselves cached.
        if (BuildSingleGetter(type, segments[0]) == null) return null;

        return root =>
        {
            object? current = root;
            foreach (var segment in segments)
            {
                if (current == null) return null;
                var getter = GetGetter(current.GetType(), segment);
                if (getter == null) return null;
                current = getter(current);
            }
            return current;
        };
    }

    private static Func<object, object?>? BuildSingleGetter(Type type, string name)
    {
        var property = type.GetProperty(name, Flags);
        if (property != null && property.CanRead)
        {
            var param = Expression.Parameter(typeof(object), "o");
            var body = Expression.Convert(
                Expression.Property(Expression.Convert(param, type), property),
                typeof(object));
            return Expression.Lambda<Func<object, object?>>(body, param).Compile();
        }

        var field = type.GetField(name, Flags);
        if (field != null)
        {
            var param = Expression.Parameter(typeof(object), "o");
            var body = Expression.Convert(
                Expression.Field(Expression.Convert(param, type), field),
                typeof(object));
            return Expression.Lambda<Func<object, object?>>(body, param).Compile();
        }

        return null;
    }

    private static SetterInfo? GetSetter(Type type, string member)
    {
        return Setters.GetOrAdd((type, member), static key =>
        {
            var property = key.Type.GetProperty(key.Member, Flags);
            if (property == null || !property.CanWrite) return null;

            var param = Expression.Parameter(typeof(object), "o");
            var valueParam = Expression.Parameter(typeof(object), "v");
            var body = Expression.Assign(
                Expression.Property(Expression.Convert(param, key.Type), property),
                Expression.Convert(valueParam, property.PropertyType));
            var setter = Expression.Lambda<Action<object, object?>>(body, param, valueParam).Compile();

            return new SetterInfo(setter, property.PropertyType);
        });
    }

    private static bool TryConvert(object? value, Type targetType, out object? result)
    {
        result = null;
        var underlying = Nullable.GetUnderlyingType(targetType);
        var effective = underlying ?? targetType;

        if (value == null)
        {
            // Reference types and Nullable<T> accept null; non-nullable value types do not.
            if (!targetType.IsValueType || underlying != null) return true;
            return false;
        }

        if (effective.IsInstanceOfType(value))
        {
            result = value;
            return true;
        }

        try
        {
            if (effective.IsEnum)
            {
                result = value is string s
                    ? Enum.Parse(effective, s, ignoreCase: true)
                    : Enum.ToObject(effective, value);
                return true;
            }

            result = Convert.ChangeType(value, effective, CultureInfo.CurrentCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
