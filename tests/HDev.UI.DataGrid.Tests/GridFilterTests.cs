using HDev.UI.DataGrid.Models;
using Xunit;

namespace HDev.UI.DataGrid.Tests;

public class GridFilterTests
{
    private sealed class Item
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public decimal Price { get; set; }
        public DateTime? Date { get; set; }
        public string? Note { get; set; }
    }

    private static GridFilter Filter(string field, FilterOperator op, object? value = null,
        object? value2 = null, bool caseSensitive = false)
        => new() { FieldName = field, Operator = op, Value = value, Value2 = value2, IsCaseSensitive = caseSensitive };

    // ── Texte ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Alpha", "alpha", true)]   // insensible à la casse par défaut
    [InlineData("Alpha", "ALPHA", true)]
    [InlineData("Alpha", "beta", false)]
    public void Equals_Text_CaseInsensitive(string itemValue, string filterValue, bool expected)
        => Assert.Equal(expected, Filter(nameof(Item.Name), FilterOperator.Equals, filterValue)
            .Matches(new Item { Name = itemValue }));

    [Fact]
    public void Equals_Text_CaseSensitive()
    {
        var filter = Filter(nameof(Item.Name), FilterOperator.Equals, "alpha", caseSensitive: true);
        Assert.False(filter.Matches(new Item { Name = "Alpha" }));
        Assert.True(filter.Matches(new Item { Name = "alpha" }));
    }

    [Theory]
    [InlineData(FilterOperator.Contains, "lph", true)]
    [InlineData(FilterOperator.Contains, "xyz", false)]
    [InlineData(FilterOperator.NotContains, "xyz", true)]
    [InlineData(FilterOperator.StartsWith, "al", true)]
    [InlineData(FilterOperator.StartsWith, "ph", false)]
    [InlineData(FilterOperator.EndsWith, "pha", true)]
    [InlineData(FilterOperator.EndsWith, "alp", false)]
    public void String_Operators(FilterOperator op, string value, bool expected)
        => Assert.Equal(expected, Filter(nameof(Item.Name), op, value)
            .Matches(new Item { Name = "Alpha" }));

    [Fact]
    public void Regex_Operator()
    {
        Assert.True(Filter(nameof(Item.Name), FilterOperator.Regex, "^Al.*a$")
            .Matches(new Item { Name = "Alpha" }));
        Assert.False(Filter(nameof(Item.Name), FilterOperator.Regex, "^X")
            .Matches(new Item { Name = "Alpha" }));
    }

    [Fact]
    public void Regex_Invalid_Pattern_DoesNotThrow()
        => Assert.False(Filter(nameof(Item.Name), FilterOperator.Regex, "([")
            .Matches(new Item { Name = "Alpha" }));

    // ── Conversion de types (fix lot 3) ────────────────────────────

    [Fact]
    public void Equals_ConvertsStringFilterValue_ToIntColumn()
        => Assert.True(Filter(nameof(Item.Count), FilterOperator.Equals, "5")
            .Matches(new Item { Count = 5 }));

    [Fact]
    public void NotEquals_ConvertsStringFilterValue()
    {
        Assert.False(Filter(nameof(Item.Count), FilterOperator.NotEquals, "5")
            .Matches(new Item { Count = 5 }));
        Assert.True(Filter(nameof(Item.Count), FilterOperator.NotEquals, "5")
            .Matches(new Item { Count = 7 }));
    }

    [Fact]
    public void Equals_UnconvertibleString_DoesNotMatch()
        => Assert.False(Filter(nameof(Item.Count), FilterOperator.Equals, "abc")
            .Matches(new Item { Count = 5 }));

    // ── Comparaisons ───────────────────────────────────────────────

    [Theory]
    [InlineData(FilterOperator.GreaterThan, 10, 15, true)]
    [InlineData(FilterOperator.GreaterThan, 10, 10, false)]
    [InlineData(FilterOperator.GreaterThanOrEqual, 10, 10, true)]
    [InlineData(FilterOperator.LessThan, 10, 5, true)]
    [InlineData(FilterOperator.LessThan, 10, 15, false)]
    [InlineData(FilterOperator.LessThanOrEqual, 10, 10, true)]
    public void Comparison_Operators(FilterOperator op, int filterValue, int itemValue, bool expected)
        => Assert.Equal(expected, Filter(nameof(Item.Count), op, filterValue)
            .Matches(new Item { Count = itemValue }));

    [Fact]
    public void Comparison_WithStringFilterValue_ConvertsToItemType()
        => Assert.True(Filter(nameof(Item.Price), FilterOperator.GreaterThan,
            80000.ToString(System.Globalization.CultureInfo.CurrentCulture))
            .Matches(new Item { Price = 95000m }));

    [Fact]
    public void Between_Inclusive()
    {
        var filter = Filter(nameof(Item.Count), FilterOperator.Between, 5, 10);
        Assert.True(filter.Matches(new Item { Count = 5 }));
        Assert.True(filter.Matches(new Item { Count = 7 }));
        Assert.True(filter.Matches(new Item { Count = 10 }));
        Assert.False(filter.Matches(new Item { Count = 11 }));
    }

    // ── Null / opérateurs sans valeur (fix lot 3) ──────────────────

    [Fact]
    public void IsNull_And_IsNotNull()
    {
        Assert.True(Filter(nameof(Item.Note), FilterOperator.IsNull).Matches(new Item { Note = null }));
        Assert.False(Filter(nameof(Item.Note), FilterOperator.IsNull).Matches(new Item { Note = "x" }));
        Assert.True(Filter(nameof(Item.Note), FilterOperator.IsNotNull).Matches(new Item { Note = "x" }));
        Assert.False(Filter(nameof(Item.Note), FilterOperator.IsNotNull).Matches(new Item { Note = null }));
    }

    [Fact]
    public void Today_WithNullValue_IsNotBypassed()
    {
        // Fix lot 3 : les opérateurs de date sans valeur ne doivent pas être
        // court-circuités par le garde « Value == null = pas de filtre »
        Assert.True(Filter(nameof(Item.Date), FilterOperator.Today)
            .Matches(new Item { Date = DateTime.Today }));
        Assert.False(Filter(nameof(Item.Date), FilterOperator.Today)
            .Matches(new Item { Date = DateTime.Today.AddDays(-10) }));
    }

    [Fact]
    public void ThisYear_MatchesCurrentYear()
    {
        Assert.True(Filter(nameof(Item.Date), FilterOperator.ThisYear)
            .Matches(new Item { Date = new DateTime(DateTime.Today.Year, 3, 15) }));
        Assert.False(Filter(nameof(Item.Date), FilterOperator.ThisYear)
            .Matches(new Item { Date = new DateTime(DateTime.Today.Year - 1, 3, 15) }));
    }

    [Fact]
    public void ValuedOperator_WithNullValue_MatchesEverything()
        => Assert.True(Filter(nameof(Item.Name), FilterOperator.Contains, null)
            .Matches(new Item { Name = "anything" }));

    [Fact]
    public void NullItemValue_OnlyMatchesNullChecks()
    {
        var item = new Item { Note = null };
        Assert.False(Filter(nameof(Item.Note), FilterOperator.Contains, "x").Matches(item));
        Assert.False(Filter(nameof(Item.Note), FilterOperator.GreaterThan, "x").Matches(item));
        Assert.True(Filter(nameof(Item.Note), FilterOperator.IsNull).Matches(item));
    }

    // ── Collections ────────────────────────────────────────────────

    [Fact]
    public void In_Collection()
    {
        var filter = Filter(nameof(Item.Name), FilterOperator.In,
            new object[] { "Alpha", "Beta" }.AsEnumerable());
        Assert.True(filter.Matches(new Item { Name = "Alpha" }));
        Assert.False(filter.Matches(new Item { Name = "Gamma" }));
    }

    // ── FilterGroup ────────────────────────────────────────────────

    [Fact]
    public void FilterGroup_And_RequiresAll()
    {
        var group = new FilterGroup { Logic = FilterLogic.And };
        group.Filters.Add(Filter(nameof(Item.Name), FilterOperator.Contains, "Al"));
        group.Filters.Add(Filter(nameof(Item.Count), FilterOperator.GreaterThan, 3));

        Assert.True(group.Matches(new Item { Name = "Alpha", Count = 5 }));
        Assert.False(group.Matches(new Item { Name = "Alpha", Count = 1 }));
    }

    [Fact]
    public void FilterGroup_Or_RequiresAny()
    {
        var group = new FilterGroup { Logic = FilterLogic.Or };
        group.Filters.Add(Filter(nameof(Item.Name), FilterOperator.Equals, "Alpha"));
        group.Filters.Add(Filter(nameof(Item.Name), FilterOperator.Equals, "Beta"));

        Assert.True(group.Matches(new Item { Name = "Beta" }));
        Assert.False(group.Matches(new Item { Name = "Gamma" }));
    }
}
