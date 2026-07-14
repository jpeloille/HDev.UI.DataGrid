using System.ComponentModel;
using Julien.Avalonia.DataGrid.Models;
using Xunit;

namespace Julien.Avalonia.DataGrid.Tests;

public class GridDataSourceTests
{
    private sealed class Person
    {
        public string Name { get; set; } = "";
        public string Dept { get; set; } = "";
        public int Age { get; set; }
    }

    private static GridDataSource CreateSource(params Person[] people)
    {
        var source = new GridDataSource { Source = people };
        return source;
    }

    private static Person[] Sample() => new[]
    {
        new Person { Name = "Zoé", Dept = "IT", Age = 30 },
        new Person { Name = "Anna", Dept = "RH", Age = 45 },
        new Person { Name = "Marc", Dept = "IT", Age = 25 },
        new Person { Name = "Luc", Dept = "RH", Age = 25 },
    };

    private static string[] Names(GridDataSource source)
        => source.View.Cast<Person>().Select(p => p.Name).ToArray();

    // ── Tri ────────────────────────────────────────────────────────

    [Fact]
    public void AddSort_Ascending()
    {
        var source = CreateSource(Sample());
        source.AddSort(nameof(Person.Name), ListSortDirection.Ascending);
        Assert.Equal(new[] { "Anna", "Luc", "Marc", "Zoé" }, Names(source));
    }

    [Fact]
    public void ToggleSort_Cycles_Asc_Desc_Removed()
    {
        var source = CreateSource(Sample());

        source.ToggleSort(nameof(Person.Name));
        Assert.Equal(ListSortDirection.Ascending, source.SortDescriptors.Single().Direction);

        source.ToggleSort(nameof(Person.Name));
        Assert.Equal(ListSortDirection.Descending, source.SortDescriptors.Single().Direction);

        source.ToggleSort(nameof(Person.Name));
        Assert.Empty(source.SortDescriptors);
    }

    [Fact]
    public void ToggleSort_WithoutAppend_ReplacesExistingSort()
    {
        var source = CreateSource(Sample());
        source.ToggleSort(nameof(Person.Age));
        source.ToggleSort(nameof(Person.Name)); // sans append : remplace

        Assert.Single(source.SortDescriptors);
        Assert.Equal(nameof(Person.Name), source.SortDescriptors[0].FieldName);
    }

    [Fact]
    public void ToggleSort_Append_MultiColumn_ThenBy()
    {
        var source = CreateSource(Sample());
        source.ToggleSort(nameof(Person.Age));               // 1er critère
        source.ToggleSort(nameof(Person.Name), append: true); // 2e critère (Shift+clic)

        Assert.Equal(2, source.SortDescriptors.Count);
        // Age 25 : Luc avant Marc (Name asc en 2e critère)
        Assert.Equal(new[] { "Luc", "Marc", "Zoé", "Anna" }, Names(source));
    }

    // ── Filtres ────────────────────────────────────────────────────

    [Fact]
    public void AddFilter_RestrictsView_AndFilteredCount()
    {
        var source = CreateSource(Sample());
        source.AddFilter(new GridFilter
        {
            FieldName = nameof(Person.Dept),
            Operator = FilterOperator.Equals,
            Value = "IT"
        });

        Assert.Equal(2, source.FilteredCount);
        Assert.Equal(4, source.TotalCount);
        Assert.All(source.View.Cast<Person>(), p => Assert.Equal("IT", p.Dept));
    }

    [Fact]
    public void MultipleFilters_AreCombinedWithAnd()
    {
        var source = CreateSource(Sample());
        source.AddFilter(new GridFilter
        {
            FieldName = nameof(Person.Dept), Operator = FilterOperator.Equals, Value = "IT"
        });
        source.AddFilter(new GridFilter
        {
            FieldName = nameof(Person.Age), Operator = FilterOperator.GreaterThan, Value = 26
        });

        Assert.Equal(new[] { "Zoé" }, Names(source));
    }

    [Fact]
    public void RemoveFilter_RestoresRows()
    {
        var source = CreateSource(Sample());
        source.AddFilter(new GridFilter
        {
            FieldName = nameof(Person.Dept), Operator = FilterOperator.Equals, Value = "IT"
        });
        source.RemoveFilter(nameof(Person.Dept));

        Assert.Equal(4, source.FilteredCount);
    }

    [Fact]
    public void AddFilter_SameField_ReplacesPreviousFilter()
    {
        var source = CreateSource(Sample());
        source.AddFilter(new GridFilter
        {
            FieldName = nameof(Person.Dept), Operator = FilterOperator.Equals, Value = "IT"
        });
        source.AddFilter(new GridFilter
        {
            FieldName = nameof(Person.Dept), Operator = FilterOperator.Equals, Value = "RH"
        });

        // La filter row réémet un filtre par colonne : il remplace le précédent
        Assert.All(source.View.Cast<Person>(), p => Assert.Equal("RH", p.Dept));
    }

    // ── Groupes ────────────────────────────────────────────────────

    [Fact]
    public void AddGroup_BuildsGroups_AndVisualRows()
    {
        var source = CreateSource(Sample());
        source.AddGroup(nameof(Person.Dept));

        Assert.True(source.IsGrouped);
        Assert.Equal(2, source.Groups.Count);
        // VisualRows = en-têtes de groupe + items (groupes développés par défaut)
        Assert.Equal(6, source.VisualRows.Count);
    }

    [Fact]
    public void CollapseGroup_RemovesItemsFromVisualRows()
    {
        var source = CreateSource(Sample());
        source.AddGroup(nameof(Person.Dept));

        source.Groups[0].IsExpanded = false;
        source.RebuildVisualRows();

        // 1 en-tête replié + 1 en-tête développé + ses 2 items
        Assert.Equal(4, source.VisualRows.Count);
    }

    [Fact]
    public void RemoveGroup_ClearsGrouping()
    {
        var source = CreateSource(Sample());
        source.AddGroup(nameof(Person.Dept));
        source.RemoveGroup(nameof(Person.Dept));

        Assert.False(source.IsGrouped);
        Assert.Equal(4, source.VisualRows.Count);
    }

    // ── Accès indexé ───────────────────────────────────────────────

    [Fact]
    public void GetItemAt_And_IndexOf_AreConsistent()
    {
        var source = CreateSource(Sample());
        source.AddSort(nameof(Person.Name), ListSortDirection.Ascending);

        var first = source.GetItemAt(0);
        Assert.NotNull(first);
        Assert.Equal("Anna", ((Person)first!).Name);
        Assert.Equal(0, source.IndexOf(first));
    }

    [Fact]
    public void GetItemAt_OutOfRange_ReturnsNull()
    {
        var source = CreateSource(Sample());
        Assert.Null(source.GetItemAt(-1));
        Assert.Null(source.GetItemAt(99));
    }
}
