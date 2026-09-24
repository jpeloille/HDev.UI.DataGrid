using HDev.UI.DataGrid.Helpers;
using HDev.UI.DataGrid.Models;
using Xunit;

namespace HDev.UI.DataGrid.Tests;

public class PropertyAccessorTests
{
    private sealed class Address
    {
        public string City { get; set; } = "";
    }

    private enum Status { Draft, Active }

    private sealed class Entity
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public decimal? Amount { get; set; }
        public Status State { get; set; }
        public Address? Home { get; set; }
    }

    [Fact]
    public void GetValue_SimpleProperty()
        => Assert.Equal("x", PropertyAccessor.GetValue(new Entity { Name = "x" }, nameof(Entity.Name)));

    [Fact]
    public void GetValue_NestedPath()
        => Assert.Equal("Nouméa", PropertyAccessor.GetValue(
            new Entity { Home = new Address { City = "Nouméa" } }, "Home.City"));

    [Fact]
    public void GetValue_NestedPath_WithNullIntermediate_ReturnsNull()
        => Assert.Null(PropertyAccessor.GetValue(new Entity { Home = null }, "Home.City"));

    [Fact]
    public void GetValue_UnknownProperty_ReturnsNull()
        => Assert.Null(PropertyAccessor.GetValue(new Entity(), "Nope"));

    [Fact]
    public void GetGetter_IsReusable_AcrossInstances()
    {
        var getter = PropertyAccessor.GetGetter(typeof(Entity), nameof(Entity.Count));
        Assert.NotNull(getter);
        Assert.Equal(1, getter!(new Entity { Count = 1 }));
        Assert.Equal(2, getter(new Entity { Count = 2 }));
    }

    [Fact]
    public void TrySetValue_ConvertsStringToInt()
    {
        var entity = new Entity();
        Assert.True(PropertyAccessor.TrySetValue(entity, nameof(Entity.Count), "42"));
        Assert.Equal(42, entity.Count);
    }

    [Fact]
    public void TrySetValue_NullableDecimal()
    {
        var entity = new Entity();
        Assert.True(PropertyAccessor.TrySetValue(entity, nameof(Entity.Amount), "12.5"));
        Assert.NotNull(entity.Amount);

        Assert.True(PropertyAccessor.TrySetValue(entity, nameof(Entity.Amount), null));
        Assert.Null(entity.Amount);
    }

    [Fact]
    public void TrySetValue_Enum()
    {
        var entity = new Entity();
        Assert.True(PropertyAccessor.TrySetValue(entity, nameof(Entity.State), nameof(Status.Active)));
        Assert.Equal(Status.Active, entity.State);
    }

    [Fact]
    public void TrySetValue_InvalidConversion_ReturnsFalse()
    {
        var entity = new Entity { Count = 7 };
        Assert.False(PropertyAccessor.TrySetValue(entity, nameof(Entity.Count), "pas-un-nombre"));
        Assert.Equal(7, entity.Count);
    }
}

public class GridColumnFormatTests
{
    [Fact]
    public void FormatValue_Null_ReturnsEmpty()
        => Assert.Equal(string.Empty, GridColumn.FormatValue(null, null));

    [Fact]
    public void FormatValue_Bool_Checkmark()
    {
        Assert.Equal("✓", GridColumn.FormatValue(null, true));
        Assert.Equal("", GridColumn.FormatValue(null, false));
    }

    [Fact]
    public void FormatValue_DateTime_ShortDate()
    {
        var date = new DateTime(2026, 7, 14);
        Assert.Equal(date.ToShortDateString(), GridColumn.FormatValue(null, date));
    }

    [Fact]
    public void FormatValue_WithFormatString()
    {
        var column = new GridColumn { FormatString = "N2" };
        Assert.Equal(1234.5m.ToString("N2"), GridColumn.FormatValue(column, 1234.5m));
    }

    [Fact]
    public void FormatValue_Fallback_ToString()
        => Assert.Equal("42", GridColumn.FormatValue(null, 42));

    [Fact]
    public void GetCellValue_UsesFieldName()
    {
        var column = new GridColumn { FieldName = "Length" };
        Assert.Equal(5, column.GetCellValue("hello"));
    }

    [Fact]
    public void GetDisplayText_CombinesValueAndFormat()
    {
        var column = new GridColumn { FieldName = "Length", FormatString = "D3" };
        Assert.Equal("005", column.GetDisplayText("hello"));
    }
}
