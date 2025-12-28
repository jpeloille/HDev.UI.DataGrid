using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DemoApp.Models;

public partial class Employee : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _department = string.Empty;

    [ObservableProperty]
    private string _position = string.Empty;

    [ObservableProperty]
    private DateTime _hireDate;

    [ObservableProperty]
    private decimal _salary;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private int _performanceRating;

    [ObservableProperty]
    private string _country = string.Empty;

    public string FullName => $"{FirstName} {LastName}";
}

public static class SampleDataGenerator
{
    private static readonly Random _random = new(42);

    private static readonly string[] FirstNames =
    {
        "Jean", "Marie", "Pierre", "Sophie", "Nicolas", "Isabelle", "François", "Catherine",
        "Michel", "Nathalie", "Philippe", "Sandrine", "Julien", "Valérie", "Laurent", "Céline"
    };

    private static readonly string[] LastNames =
    {
        "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand",
        "Leroy", "Moreau", "Simon", "Laurent", "Lefebvre", "Michel", "Garcia", "David"
    };

    private static readonly string[] Departments =
    {
        "Engineering", "Marketing", "Sales", "HR", "Finance", "Operations", "Legal", "IT"
    };

    private static readonly string[] Positions =
    {
        "Junior", "Senior", "Lead", "Manager", "Director", "VP", "Analyst", "Specialist"
    };

    private static readonly string[] Countries =
    {
        "France", "New Caledonia", "Australia", "New Zealand", "Japan", "Singapore"
    };

    public static ObservableCollection<Employee> GenerateEmployees(int count)
    {
        var employees = new ObservableCollection<Employee>();

        for (int i = 1; i <= count; i++)
        {
            var firstName = FirstNames[_random.Next(FirstNames.Length)];
            var lastName = LastNames[_random.Next(LastNames.Length)];
            var department = Departments[_random.Next(Departments.Length)];

            employees.Add(new Employee
            {
                Id = i,
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@company.com",
                Department = department,
                Position = Positions[_random.Next(Positions.Length)],
                HireDate = DateTime.Now.AddDays(-_random.Next(365 * 10)),
                Salary = Math.Round((decimal)(_random.NextDouble() * 100000 + 30000), 2),
                IsActive = _random.NextDouble() > 0.1,
                PerformanceRating = _random.Next(1, 6),
                Country = Countries[_random.Next(Countries.Length)]
            });
        }

        return employees;
    }
}
