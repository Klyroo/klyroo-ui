namespace Klyroo.UI.Internal;

/// <summary>
/// What a date field does with the type it is bound to, chosen once per type with no reflection: it reads the day of a value and builds a value from a day.
/// Supports <see cref="DateOnly"/>, <see cref="DateTime"/> and <see cref="DateTimeOffset"/> and their nullable forms. A <see cref="DateTime"/> built from a day is
/// that day, and the time of day of the value it replaces is kept: a <see cref="DateTime"/> keeps its time and kind, a <see cref="DateTimeOffset"/> its time and offset. A value that had no
/// time to keep (null before, or the default value of a non-nullable type) starts at midnight: a <see cref="DateTime"/> of unspecified kind, a <see cref="DateTimeOffset"/> in the offset of the
/// server's local time zone on that day.
/// </summary>
internal abstract class DateCodec<T>
{
    private static DateCodec<T>? _instance;

    /// <summary>The codec of <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidOperationException">The type is not a supported date type.</exception>
    public static DateCodec<T> Instance => _instance ??= Create();

    /// <summary>The day of a value, in the value's own calendar day (the offset of a <see cref="DateTimeOffset"/> is not applied). No value is null.</summary>
    public abstract DateOnly? ToDate(T? value);

    /// <summary>
    /// The value for a day: null for no day when the type is nullable, else the default value of the type. <paramref name="previous"/> is the value the day replaces: its time
    /// of day (and offset) are kept; without one (null, or the default value) the time is midnight.
    /// </summary>
    public abstract T? FromDate(DateOnly? date, T? previous);

    private static DateCodec<T> Create()
    {
        if (typeof(T) == typeof(DateOnly)) return (DateCodec<T>)(object)new DateCodecs.Plain<DateOnly>(d => d, (d, _) => d);
        if (typeof(T) == typeof(DateOnly?)) return (DateCodec<T>)(object)new DateCodecs.Lifted<DateOnly>(d => d, (d, _) => d);
        if (typeof(T) == typeof(DateTime)) return (DateCodec<T>)(object)new DateCodecs.Plain<DateTime>(DateOnly.FromDateTime, DateCodecs.Merge);
        if (typeof(T) == typeof(DateTime?)) return (DateCodec<T>)(object)new DateCodecs.Lifted<DateTime>(DateOnly.FromDateTime, DateCodecs.Merge);
        if (typeof(T) == typeof(DateTimeOffset)) return (DateCodec<T>)(object)new DateCodecs.Plain<DateTimeOffset>(o => DateOnly.FromDateTime(o.DateTime), DateCodecs.Merge);
        if (typeof(T) == typeof(DateTimeOffset?)) return (DateCodec<T>)(object)new DateCodecs.Lifted<DateTimeOffset>(o => DateOnly.FromDateTime(o.DateTime), DateCodecs.Merge);
        throw new InvalidOperationException($"{typeof(T).Name} is not a date type Klyroo.UI can bind: use DateOnly, DateTime or DateTimeOffset, or the nullable form of one of them.");
    }
}

/// <summary>The codecs of the supported date types, apart from <see cref="DateCodec{T}"/> so that each is one type, not one per bound type.</summary>
internal static class DateCodecs
{
    // The day, at the time of day (and kind) of the value it replaces; midnight when there is none to keep (the default value).
    internal static DateTime Merge(DateOnly date, DateTime previous) =>
        previous == default ? date.ToDateTime(TimeOnly.MinValue) : DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromTimeSpan(previous.TimeOfDay)), previous.Kind);

    // The day, at the time of day and the offset of the value it replaces; without one, midnight in the offset of the local time zone on that day.
    internal static DateTimeOffset Merge(DateOnly date, DateTimeOffset previous) =>
        previous == default
            ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue))
            : new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(previous.TimeOfDay)), previous.Offset);

    internal sealed class Plain<TDate>(Func<TDate, DateOnly> to, Func<DateOnly, TDate, TDate> from) : DateCodec<TDate> where TDate : struct
    {
        public override DateOnly? ToDate(TDate value) => to(value);

        public override TDate FromDate(DateOnly? date, TDate previous) => date is { } day ? from(day, previous) : default;
    }

    internal sealed class Lifted<TDate>(Func<TDate, DateOnly> to, Func<DateOnly, TDate, TDate> from) : DateCodec<TDate?> where TDate : struct
    {
        public override DateOnly? ToDate(TDate? value) => value is { } date ? to(date) : null;

        public override TDate? FromDate(DateOnly? date, TDate? previous) => date is { } day ? from(day, previous ?? default) : null;
    }
}
