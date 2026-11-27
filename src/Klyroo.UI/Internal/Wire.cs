namespace Klyroo.UI.Internal;

/// <summary>The text an enum takes in rendered markup: HTML attribute values and tag names, always the same lowercase or kebab-case spelling.</summary>
internal static class Wire
{
    public static string Of(KlyPlacement placement) => placement switch
    {
        KlyPlacement.Top => "top",
        KlyPlacement.TopStart => "top-start",
        KlyPlacement.TopEnd => "top-end",
        KlyPlacement.Right => "right",
        KlyPlacement.RightStart => "right-start",
        KlyPlacement.RightEnd => "right-end",
        KlyPlacement.Bottom => "bottom",
        KlyPlacement.BottomStart => "bottom-start",
        KlyPlacement.BottomEnd => "bottom-end",
        KlyPlacement.Left => "left",
        KlyPlacement.LeftStart => "left-start",
        KlyPlacement.LeftEnd => "left-end",
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };

    public static string Of(KlySurface surface) => surface switch
    {
        KlySurface.Plain => "plain",
        KlySurface.Muted => "muted",
        KlySurface.Bordered => "bordered",
        KlySurface.Elevated => "elevated",
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, null),
    };

    public static string Of(ButtonType type) => type switch
    {
        ButtonType.Button => "button",
        ButtonType.Submit => "submit",
        ButtonType.Reset => "reset",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static string Of(InputType type) => type switch
    {
        InputType.Text => "text",
        InputType.Email => "email",
        InputType.Password => "password",
        InputType.Search => "search",
        InputType.Tel => "tel",
        InputType.Url => "url",
        InputType.Number => "number",
        InputType.Date => "date",
        InputType.Time => "time",
        InputType.DateTimeLocal => "datetime-local",
        InputType.Month => "month",
        InputType.Week => "week",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static string Of(KlyElement element) => element switch
    {
        KlyElement.Div => "div",
        KlyElement.Section => "section",
        KlyElement.Article => "article",
        KlyElement.Aside => "aside",
        KlyElement.Header => "header",
        KlyElement.Footer => "footer",
        KlyElement.Main => "main",
        KlyElement.Nav => "nav",
        _ => throw new ArgumentOutOfRangeException(nameof(element), element, null),
    };

    public static string Of(KlyDirection direction) => direction switch
    {
        KlyDirection.Ltr => "ltr",
        KlyDirection.Rtl => "rtl",
        KlyDirection.Auto => "auto",
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };
}
