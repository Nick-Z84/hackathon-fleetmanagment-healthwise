namespace VehicleBookingSystem.Core.Models;

public enum StaffRole { User, Admin }

public class Staff
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool HasDriversLicence { get; set; }
    public DateOnly? DriversLicenceExpiry { get; set; }

    // Auth
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public StaffRole Role { get; set; } = StaffRole.User;

    public string FullName => $"{FirstName} {LastName}";

    public bool IsLicenceCurrentAsOf(DateOnly date) =>
        HasDriversLicence && DriversLicenceExpiry.HasValue && DriversLicenceExpiry.Value >= date;
}
