namespace Klyroo.UI.Internal;

/// <summary>
/// What a date field does with the type it is bound to, chosen once per type with no reflection: it reads the day of a value and builds a value from a day.
/// Supports <see cref="DateOnly"/>, <see cref="DateTime"/> and <see cref="DateTimeOffset"/> and their nullable forms. A <see cref="DateTime"/> built from a day is
/// that day at midnight (kind unspecified); a <see cref="DateTimeOffset"/> is that day at midnight in the local offset, as the built-in <c>InputDate</c> reads it.
/// </summary>
internal abstract class DateCodec<T>
{
    private static DateCodec<T>? _instance;

    /// <summary>The codec of <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidOperationException">The type is not a supported date type.</exception>
    public static DateCodec<T> Instance => _instance ??= Create();

    /// <summary>The day of a value, in the value's own calendar day (the offset of a <see cref="DateTimeOffset"/> is not applied). No value is null.</summary>
    public abstract DateOnly? ToDate(T? value);

    /// <summary>The value for a day: null for no day when the type is nullable, else the default value of the type.</summary>
    public abstract T? FromDate(DateOnly? date);

    private static DateCodec<T> Create()
    {
        if (typeof(T) == typeof(DateOnly)) return (DateCodec<T>)(object)new DateCodecs.Plain<DateOnly>(d => d, d => d);
        if (typeof(T) == typeof(DateOnly?)) return (DateCodec<T>)(object)new DateCodecs.Lifted<DateOnly>(d => d, d => d);
        if (typeof(T) == typeof(DateTime)) return (DateCodec<T>)(object)new DateCodecs.Plain<DateTime>(DateOnly.FromDateTime, DateCodecs.AtMidnight);
        if (typeof(T) == typeof(DateTime?)) return (DateCodec<T>)(object)new DateCodecs.Lifted<DateTime>(DateOnly.FromDateTime, DateCodecs.AtMidnight);
        if (typeof(T) == typeof(DateTimeOffset)) return (DateCodec<T>)(object)new DateCodecs.Plain<DateTimeOffset>(o => DateOnly.FromDateTime(o.DateTime), DateCodecs.AtMidnightOffset);
        if (typeof(T) == typeof(DateTimeOffset?)) return (DateCodec<T>)(object)new DateCodecs.Lifted<DateTimeOffset>(o => DateOnly.FromDateTime(o.DateTime), DateCodecs.AtMidnightOffset);
        throw new InvalidOperationException($"{typeof(T).Name} is not a date type Klyroo.UI can bind: use DateOnly, DateTime or DateTimeOffset, or the nullable form of one of them.");
    }
}

/// <summary>The codecs of the supported date types, apart from <see cref="DateCodec{T}"/> so that each is one type, not one per bound type.</summary>
internal static class DateCodecs
{
    internal static DateTime AtMidnight(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);

    internal static DateTimeOffset AtMidnightOffset(DateOnly date) => new(AtMidnight(date));

    internal sealed class Plain<TDate>(Func<TDate, DateOnly> to, Func<DateOnly, TDate> from) : DateCodec<TDate> where TDate : struct
    {
        public override DateOnly? ToDate(TDate value) => to(value);

        public override TDate FromDate(DateOnly? date) => date is { } day ? from(day) : default;
    }

    internal sealed class Lifted<TDate>(Func<TDate, DateOnly> to, Func<DateOnly, TDate> from) : DateCodec<TDate?> where TDate : struct
    {
        public override DateOnly? ToDate(TDate? value) => value is { } date ? to(date) : null;

        public override TDate? FromDate(DateOnly? date) => date is { } day ? from(day) : null;
    }
}
