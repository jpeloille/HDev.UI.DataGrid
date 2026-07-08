using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoApp.Controls;
using DemoApp.Models;
using Julien.Avalonia.DataGrid.Models;
using System.Collections.ObjectModel;

namespace DemoApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private List<Employee> _allEmployees;

    [ObservableProperty]
    private ObservableCollection<Employee> _employees;

    [ObservableProperty]
    private Employee? _selectedEmployee;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool _showFilterRow;

    [ObservableProperty]
    private bool _showGroupPanel;

    [ObservableProperty]
    private bool _showRowNumbers;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private int _dataRowCount = 1000;

    [ObservableProperty]
    private ObservableCollection<SkinInfo> _availableSkins;

    [ObservableProperty]
    private SkinInfo? _selectedSkin;

    public MainViewModel()
    {
        _allEmployees = SampleDataGenerator.GenerateEmployees(1000).ToList();
        _employees = new ObservableCollection<Employee>(_allEmployees);
        _availableSkins = new ObservableCollection<SkinInfo>
        {
            new SkinInfo
            {
                Name = "Blue",
                ThemeResourcePath = "avares://DemoApp/Themes/Colors/BlueTheme.axaml",
                AccentColor = Color.Parse("#0078D4"),
                SecondaryColor = Color.Parse("#1C86E0"),
                TertiaryColor = Color.Parse("#404040")
            },
            new SkinInfo
            {
                Name = "Cyan",
                ThemeResourcePath = "avares://DemoApp/Themes/Colors/CyanTheme.axaml",
                AccentColor = Color.Parse("#00B7C3"),
                SecondaryColor = Color.Parse("#00D4E1"),
                TertiaryColor = Color.Parse("#404040")
            },
            new SkinInfo
            {
                Name = "Gray",
                ThemeResourcePath = "avares://DemoApp/Themes/Colors/GrayTheme.axaml",
                AccentColor = Color.Parse("#69797E"),
                SecondaryColor = Color.Parse("#8A9A9F"),
                TertiaryColor = Color.Parse("#404040")
            },
            new SkinInfo
            {
                Name = "Gold",
                ThemeResourcePath = "avares://DemoApp/Themes/Colors/GoldTheme.axaml",
                AccentColor = Color.Parse("#C19C00"),
                SecondaryColor = Color.Parse("#D4B000"),
                TertiaryColor = Color.Parse("#404040")
            }
        };
        _selectedSkin = _availableSkins[0];
        UpdateStatusText();
    }

    partial void OnSelectedSkinChanged(SkinInfo? value)
    {
        if (value == null) return;
        ApplyTheme(value);
    }

    private void ApplyTheme(SkinInfo skin)
    {
        var app = Application.Current;
        if (app == null) return;

        var resources = app.Resources;
        var dictionaries = resources.MergedDictionaries;

        // Find existing color theme
        IResourceProvider? existingTheme = null;
        foreach (var dict in dictionaries)
        {
            if (dict is ResourceInclude ri && ri.Source?.ToString().Contains("/Colors/") == true)
            {
                existingTheme = dict;
                break;
            }
        }

        var newTheme = new ResourceInclude(new Uri("avares://DemoApp"))
        {
            Source = new Uri(skin.ThemeResourcePath)
        };

        if (existingTheme != null)
        {
            var index = dictionaries.IndexOf(existingTheme);
            dictionaries.RemoveAt(index);
            dictionaries.Insert(index, newTheme);
        }
        else
        {
            // App.axaml only merges the WinXI bundle; the color theme must sit
            // after it so its brushes win the DynamicResource lookup.
            dictionaries.Add(newTheme);
        }
    }

    partial void OnSearchTextChanged(string? value) => ApplySearch();

    private void ApplySearch()
    {
        var query = SearchText?.Trim();
        Employees = string.IsNullOrEmpty(query)
            ? new ObservableCollection<Employee>(_allEmployees)
            : new ObservableCollection<Employee>(_allEmployees.Where(e => Matches(e, query)));
        UpdateStatusText();
    }

    private static bool Matches(Employee e, string query) =>
        ContainsInsensitive(e.FirstName, query) ||
        ContainsInsensitive(e.LastName, query) ||
        ContainsInsensitive(e.Email, query) ||
        ContainsInsensitive(e.Department, query) ||
        ContainsInsensitive(e.Position, query) ||
        ContainsInsensitive(e.Country, query);

    // Accent-insensitive contains: "valerie" matches "Valérie".
    private static bool ContainsInsensitive(string? source, string query) =>
        source != null &&
        System.Globalization.CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            source, query,
            System.Globalization.CompareOptions.IgnoreCase |
            System.Globalization.CompareOptions.IgnoreNonSpace) >= 0;

    [RelayCommand]
    private void RefreshData()
    {
        IsLoading = true;

        // Simulate loading delay
        Task.Run(async () =>
        {
            await Task.Delay(500);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _allEmployees = SampleDataGenerator.GenerateEmployees(DataRowCount).ToList();
                IsLoading = false;
                ApplySearch();
            });
        });
    }

    [RelayCommand]
    private void AddEmployee()
    {
        var newEmployee = new Employee
        {
            Id = _allEmployees.Count + 1,
            FirstName = "New",
            LastName = "Employee",
            Email = "new.employee@company.com",
            Department = "Engineering",
            Position = "Junior",
            HireDate = DateTime.Today,
            Salary = 50000,
            IsActive = true,
            PerformanceRating = 3,
            Country = "France"
        };

        _allEmployees.Add(newEmployee);
        Employees.Add(newEmployee);
        SelectedEmployee = newEmployee;
        UpdateStatusText();
    }

    [RelayCommand]
    private void DeleteEmployee()
    {
        if (SelectedEmployee != null)
        {
            _allEmployees.Remove(SelectedEmployee);
            Employees.Remove(SelectedEmployee);
            SelectedEmployee = null;
            UpdateStatusText();
        }
    }

    [RelayCommand]
    private void ToggleFilterRow()
    {
        ShowFilterRow = !ShowFilterRow;
    }

    [RelayCommand]
    private void ToggleGroupPanel()
    {
        ShowGroupPanel = !ShowGroupPanel;
    }

    [RelayCommand]
    private void ToggleRowNumbers()
    {
        ShowRowNumbers = !ShowRowNumbers;
    }

    [RelayCommand]
    private void LoadSmallDataset()
    {
        DataRowCount = 100;
        RefreshData();
    }

    [RelayCommand]
    private void LoadMediumDataset()
    {
        DataRowCount = 1000;
        RefreshData();
    }

    [RelayCommand]
    private void LoadLargeDataset()
    {
        DataRowCount = 10000;
        RefreshData();
    }

    [RelayCommand]
    private void LoadVeryLargeDataset()
    {
        DataRowCount = 100000;
        RefreshData();
    }

    private void UpdateStatusText()
    {
        StatusText = string.IsNullOrEmpty(SearchText?.Trim())
            ? $"{Employees.Count:N0} employees"
            : $"{Employees.Count:N0} of {_allEmployees.Count:N0} employees";
        if (SelectedEmployee != null)
        {
            StatusText += $" | Selected: {SelectedEmployee.FullName}";
        }
    }

    partial void OnSelectedEmployeeChanged(Employee? value)
    {
        UpdateStatusText();
    }
}
