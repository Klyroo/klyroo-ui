namespace Klyroo.UI.Maui;

/// <summary>A native date or time picker in the Klyroo.UI field frame, for the date and time fields of <see cref="KlyAuthForm"/>.</summary>
internal sealed class PagesPickerField : KlyFieldBase
{
    private readonly View _picker;

    public PagesPickerField(View picker) : base(picker)
    {
        _picker = picker;
        ApplyStyle();
    }

    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        switch (_picker)
        {
            case DatePicker date:
                date.SetThemeColor(DatePicker.TextColorProperty, text);
                date.FontSize = fontSize;
                break;
            case TimePicker time:
                time.SetThemeColor(TimePicker.TextColorProperty, text);
                time.FontSize = fontSize;
                break;
        }
    }
}
