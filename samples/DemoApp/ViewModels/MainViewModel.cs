using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoApp.Models;
using Julien.Avalonia.DataGrid.Models;
using System.Collections.ObjectModel;

namespace DemoApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Employee> _employees;

    [ObservableProperty]
    private Employee? _selectedEmployee;

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

    public MainViewModel()
    {
        _employees = SampleDataGenerator.GenerateEmployees(1000);
        UpdateStatusText();
    }

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
                Employees = SampleDataGenerator.GenerateEmployees(DataRowCount);
                IsLoading = false;
                UpdateStatusText();
            });
        });
    }

    [RelayCommand]
    private void AddEmployee()
    {
        var newEmployee = new Employee
        {
            Id = Employees.Count + 1,
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

        Employees.Add(newEmployee);
        SelectedEmployee = newEmployee;
        UpdateStatusText();
    }

    [RelayCommand]
    private void DeleteEmployee()
    {
        if (SelectedEmployee != null)
        {
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
        StatusText = $"{Employees.Count:N0} employees";
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
