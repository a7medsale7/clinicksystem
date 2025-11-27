using System.ComponentModel.DataAnnotations;

namespace ClinicSystem2.ViewModels;

public class PatientViewModel
{
    public int PatientID { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; }

    public string Phone { get; set; }

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    public string Gender { get; set; }
    public string Address { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? Age { get; set; }
}

public class PatientCreateEditViewModel
{
    public int PatientID { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; }

    public string Phone { get; set; }

    [Required(ErrorMessage = "Birth date is required")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    public string Gender { get; set; }

    public string Address { get; set; }
}
