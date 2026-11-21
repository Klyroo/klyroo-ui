using System.Reflection;
using System.Text.RegularExpressions;

namespace Blux.UI.Tools.Tests;

/// <summary>The message tables: same codes as Blux.UI, no key missing, same placeholders, resolved from the locale code, overridable.</summary>
public partial class ToolsLocalizationTests
{
    private static readonly PropertyInfo[] MessageProperties = typeof(BxToolsMessages).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(string)).ToArray();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    private static string[] Placeholders(string text) => Placeholder().Matches(text).Select(m => m.Value).Order().ToArray();

    public static TheoryData<string> LocaleCodes()
    {
        var data = new TheoryData<string>();
        foreach (var locale in BxLocales.All) data.Add(locale.Code);
        return data;
    }

    [Fact]
    public void There_is_one_table_per_locale_of_Blux_UI_with_the_same_codes()
    {
        Assert.Equal(BxLocales.All.Select(l => l.Code).Order(StringComparer.Ordinal), BxToolsLocales.Tables.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(12, BxToolsLocales.Tables.Count);
    }

    [Fact]
    public void Messages_exist()
    {
        Assert.NotEmpty(MessageProperties);
    }

    [Theory]
    [MemberData(nameof(LocaleCodes))]
    public void Every_message_of_every_table_is_filled_and_keeps_the_placeholders_of_English(string code)
    {
        var table = BxToolsLocales.Tables[code];
        foreach (var property in MessageProperties)
        {
            var english = (string)property.GetValue(BxToolsLocales.En)!;
            var text = (string?)property.GetValue(table);
            Assert.False(string.IsNullOrWhiteSpace(text), $"{code}: {property.Name} is empty");
            Assert.True(Placeholders(english).SequenceEqual(Placeholders(text!)), $"{code}: {property.Name} has the placeholders {string.Join(",", Placeholders(text!))}, English has {string.Join(",", Placeholders(english))}");
        }
    }

    [Theory]
    [MemberData(nameof(LocaleCodes))]
    public void A_translated_table_translates_nearly_every_message(string code)
    {
        if (code == "en") return;
        var same = MessageProperties
            .Where(p => (string)p.GetValue(BxToolsLocales.En)! == (string)p.GetValue(BxToolsLocales.Tables[code])!)
            .Select(p => p.Name).ToList();

        // Few messages so far, and none is a word shared between languages: raise the allowance (core allows 20 of ~200) when one is added.
        Assert.True(same.Count == 0, $"{code} keeps {same.Count} English messages: {string.Join(", ", same)}");
    }

    [Fact]
    public void English_and_French_are_the_reviewed_tables()
    {
        Assert.Equal(["en", "fr"], BxToolsLocales.ReviewedCodes);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("fr-CA", "fr")]
    [InlineData("pt-PT", "pt")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-Hans", "zh-Hans")]
    [InlineData("tlh", "en")]
    public void A_locale_code_resolves_to_a_table_like_a_culture_does_in_Blux_UI(string code, string table)
    {
        var options = new BluxToolsOptions();

        Assert.Same(BxToolsLocales.Tables[table], options.ResolveMessages(code));
    }

    [Fact]
    public void Changes_apply_to_the_table_of_a_locale_and_to_the_locales_that_resolve_to_it()
    {
        var options = new BluxToolsOptions()
            .Messages("fr", m => m with { NoResults = "Rien" })
            .Messages("fr-CA", m => m with { NoResultsDescription = "Essaie autre chose." });

        Assert.Equal("Rien", options.ResolveMessages("fr").NoResults);
        Assert.Equal("Rien", options.ResolveMessages("fr-CA").NoResults);
        Assert.Equal("Essaie autre chose.", options.ResolveMessages("fr-CA").NoResultsDescription);
        Assert.Equal(BxToolsLocales.Fr.NoResultsDescription, options.ResolveMessages("fr").NoResultsDescription);
        Assert.Equal("No results", options.ResolveMessages("en").NoResults);
    }

    [Fact]
    public void A_custom_locale_starts_from_English_and_takes_its_changes()
    {
        var options = new BluxToolsOptions().Messages("tlh", m => m with { NoResults = "Qapla'" });

        Assert.Equal("Qapla'", options.ResolveMessages("tlh").NoResults);
        Assert.Equal(BxToolsLocales.En.NoResultsDescription, options.ResolveMessages("tlh").NoResultsDescription);
    }

    [Fact]
    public void Messages_become_read_only_when_a_text_is_resolved()
    {
        var options = new BluxToolsOptions();
        options.ResolveMessages("en");

        Assert.Throws<InvalidOperationException>(() => options.Messages("en", m => m));
    }
}
