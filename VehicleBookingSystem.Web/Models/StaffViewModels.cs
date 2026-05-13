using System.ComponentModel.DataAnnotations;
using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Web.Models;

public class StaffViewModel
{
    public int Id { get; set; }
    [Required, Display(Name = "First Name")] public string FirstName { get; set; } = string.Empty;
    [Required, Display(Name = "Last Name")]  public string LastName  { get; set; } = string.Empty;
    [Required]                               public string Username  { get; set; } = string.Empty;
    [Display(Name = "Role")]                 public StaffRole Role   { get; set; } = StaffRole.User;
    [Display(Name = "Holds Driver's Licence")] public bool HasDriversLicence { get; set; }
    [Display(Name = "Licence Expiry")] public DateOnly? DriversLicenceExpiry { get; set; }

    // Password — only required on Create; leave blank on Edit to keep existing
    [DataType(DataType.Password), Display(Name = "Password")]
    public string? Password { get; set; }
}

