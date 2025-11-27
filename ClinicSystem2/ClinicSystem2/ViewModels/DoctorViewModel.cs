using System.ComponentModel.DataAnnotations;

namespace ClinicSystem2.ViewModels;

public class DoctorViewModel
{
    public int DoctorID { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; }

    public string Specialty { get; set; }
    public string Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Consultation fee cannot be negative")]
    public decimal ConsultationFee { get; set; }

    public bool IsActive { get; set; }
    public int? AppointmentCount { get; set; }
    public decimal? TotalRevenue { get; set; }
}

public class DoctorCreateEditViewModel
{
    public int DoctorID { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Specialty is required")]
    public string Specialty { get; set; }

    public string Description { get; set; }

    [Required(ErrorMessage = "Consultation fee is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Consultation fee cannot be negative")]
    public decimal ConsultationFee { get; set; }

    public bool IsActive { get; set; }
}
