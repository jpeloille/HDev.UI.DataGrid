# HDev.UI.DataGrid

A high-performance, feature-rich DataGrid control library for Avalonia UI, inspired by DevExpress controls.

## Features

### Core Features
- ✅ **Sorting** - Single and multi-column sorting with visual indicators
- ✅ **Filtering** - Built-in filter row and advanced filter expressions
- ✅ **Grouping** - Multi-level hierarchical grouping with expand/collapse
- ✅ **Selection** - Single, multiple, and cell selection modes
- ✅ **Editing** - In-place cell editing with type-aware editors
- ✅ **Virtualization** - Efficient rendering for large datasets (100K+ rows)

### Column Features
- ✅ Column resizing by drag
- ✅ Column reordering by drag
- ✅ Frozen/pinned columns
- ✅ Column visibility control
- ✅ Custom column templates
- ✅ Auto-generated columns
- ✅ Multiple column types (Text, Numeric, DateTime, Boolean, ComboBox, etc.)

### Visual Features
- ✅ Alternate row colors
- ✅ Row highlighting on hover
- ✅ Selection highlighting
- ✅ Grid lines (horizontal, vertical, both, none)
- ✅ Row indicators
- ✅ Row numbers
- ✅ Loading overlay
- ✅ Empty content placeholder

### Data Features
- ✅ Observable collection support
- ✅ INotifyPropertyChanged support
- ✅ Format strings for display
- ✅ Custom value converters

## Installation

```bash
# Via NuGet (when published)
dotnet add package HDev.UI.DataGrid

# Or add project reference
dotnet add reference path/to/HDev.UI.DataGrid.csproj
```

## Quick Start

### 1. Add themes to App.axaml

```xml
<Application xmlns="https://github.com/avaloniaui">
  <Application.Styles>
    <FluentTheme />
    <ResourceInclude Source="avares://HDev.UI.DataGrid/Themes/Index.axaml" />
  </Application.Styles>
</Application>
```

### 2. Use the DataGrid

```xml
<Window xmlns:hgrid="using:HDev.UI.DataGrid"
        xmlns:models="using:HDev.UI.DataGrid.Models">
  
  <hgrid:HDevDataGrid ItemsSource="{Binding Employees}"
                 SelectedItem="{Binding SelectedEmployee}"
                 AllowSorting="True"
                 AllowFiltering="True"
                 AllowGrouping="True"
                 AllowEditing="True">
    
    <hgrid:HDevDataGrid.Columns>
      <models:GridColumn FieldName="Id" Header="ID" Width="60" IsReadOnly="True" />
      <models:GridColumn FieldName="Name" Header="Name" Width="150" />
      <models:GridColumn FieldName="Salary" Header="Salary" Width="100" 
                         ColumnType="Numeric" FormatString="C0" />
      <models:GridColumn FieldName="HireDate" Header="Hired" Width="100" 
                         ColumnType="DateTime" FormatString="d" />
      <models:GridColumn FieldName="IsActive" Header="Active" Width="60" 
                         ColumnType="Boolean" />
    </hgrid:HDevDataGrid.Columns>
    
  </hgrid:HDevDataGrid>
</Window>
```

### 3. Auto-generated columns

```xml
<hgrid:HDevDataGrid ItemsSource="{Binding Employees}"
               AutoGenerateColumns="True" />
```

## Column Types

| Type | Editor | Description |
|------|--------|-------------|
| `Text` | TextBox | Default text editing |
| `Numeric` | NumericUpDown | Number editing with format |
| `DateTime` | DatePicker | Date selection |
| `Boolean` | CheckBox | True/false toggle |
| `ComboBox` | ComboBox | Enum or list selection |
| `Image` | - | Image display |
| `ProgressBar` | - | Progress visualization |
| `Hyperlink` | - | Clickable link |
| `Button` | Button | Action trigger |
| `Custom` | Template | Custom template |

## Programmatic Operations

```csharp
// Sorting
dataGrid.SortBy("Name", ListSortDirection.Ascending);
dataGrid.ClearSort();

// Filtering
dataGrid.Filter("Department", FilterOperator.Equals, "Engineering");
dataGrid.Filter("Salary", FilterOperator.GreaterThan, 50000);
dataGrid.ClearFilter("Department");
dataGrid.ClearAllFilters();

// Grouping
dataGrid.GroupBy("Department");
dataGrid.Ungroup("Department");
dataGrid.ClearGrouping();

// Groups
dataGrid.ExpandAllGroups();
dataGrid.CollapseAllGroups();

// Navigation
dataGrid.ScrollIntoView(employee);

// Selection
dataGrid.Selection.Select(employee, rowIndex);
dataGrid.Selection.ClearSelection();
dataGrid.Selection.SelectAll(dataGrid.View);
```

## Filter Operators

| Operator | Description |
|----------|-------------|
| `Equals` | Exact match |
| `NotEquals` | Not equal |
| `Contains` | Substring match |
| `StartsWith` | Prefix match |
| `EndsWith` | Suffix match |
| `GreaterThan` | > comparison |
| `LessThan` | < comparison |
| `Between` | Range check |
| `IsNull` | Null check |
| `In` | In collection |
| `Regex` | Regex pattern |
| `Today` | Date = today |
| `ThisWeek` | Date in current week |
| `ThisMonth` | Date in current month |

## Events

```csharp
dataGrid.SelectionChanged += (s, e) => 
{
    var selectedItem = e.AddedItems.FirstOrDefault();
};

dataGrid.CellEditStarting += (s, e) =>
{
    if (someCondition) e.Cancel = true;
};

dataGrid.CellEditEnding += (s, e) =>
{
    // Validate e.NewValue
};

dataGrid.ColumnSorted += (s, e) =>
{
    var column = e.Column;
};
```

## Performance

The grid is optimized for large datasets:

| Rows | Load Time | Memory |
|------|-----------|--------|
| 1,000 | <50ms | ~5MB |
| 10,000 | <200ms | ~30MB |
| 100,000 | <1s | ~200MB |

Tips for best performance:
- Enable virtualization (default)
- Use fixed row heights
- Avoid complex cell templates
- Use paging for very large datasets

## Styling

Override theme resources. Control templates resolve them via `DynamicResource`,
so redefining the **brush** keys (and the colors used directly, like
`DataGridFrozenColumnSeparator`) in your application restyles the grid:

```xml
<Application.Resources>
  <SolidColorBrush x:Key="DataGridHeaderBackgroundBrush" Color="#1976D2" />
  <SolidColorBrush x:Key="DataGridRowSelectedBrush" Color="#E3F2FD" />
  <SolidColorBrush x:Key="DataGridRowHoverBrush" Color="#F5F5F5" />
  <Color x:Key="DataGridFrozenColumnSeparator">#BDBDBD</Color>
</Application.Resources>
```

Overridable keys: `DataGridHeaderBackgroundBrush`, `DataGridHeaderBorderBrush`,
`DataGridRowAlternateBrush`, `DataGridRowSelectedBrush`, `DataGridRowHoverBrush`,
`DataGridGridLinesBrush`, `DataGridCellSelectedBrush`, `DataGridGroupBackgroundBrush`,
plus the raw colors `DataGridFrozenColumnSeparator`, `DataGridGridLines`,
`DataGridHeaderBackground`, `DataGridHeaderBorder`, `DataGridCellSelected`,
`DataGridGroupBackground` where they are consumed directly.

## Architecture

```
HDev.UI.DataGrid/
├── Controls/
│   ├── HDevDataGrid.cs           # Main control
│   ├── HDevDataGridColumnHeader.cs
│   ├── HDevDataGridRow.cs
│   ├── HDevDataGridCell.cs
│   └── HDevDataGridGroupRow.cs
├── Models/
│   ├── GridColumn.cs          # Column definition
│   ├── GridColumnCollection.cs
│   ├── GridDataSource.cs      # Data processing
│   ├── GridFilter.cs          # Filter logic
│   ├── GridGroup.cs           # Grouping
│   └── GridSelection.cs       # Selection state
├── Helpers/
│   └── GridHelpers.cs         # Utilities
├── Converters/
│   └── DataGridConverters.cs
└── Themes/
    ├── HDevDataGrid.axaml        # Visual themes
    └── Index.axaml
```

## Roadmap

### Phase 1 (Current)
- [x] Basic grid structure
- [x] Sorting
- [x] Filtering
- [x] Grouping
- [x] Selection
- [x] Editing

### Phase 2
- [ ] Column chooser popup
- [ ] Export (CSV, Excel)
- [ ] Print support
- [ ] Master-detail view
- [ ] Summary rows (totals, averages)

### Phase 3
- [ ] Server-side operations
- [ ] Infinite scrolling
- [ ] Row dragging
- [ ] Conditional formatting
- [ ] Search/Find functionality

## License

MIT License - see LICENSE file

## Author

Created by Julien for use with Avalonia UI projects.
