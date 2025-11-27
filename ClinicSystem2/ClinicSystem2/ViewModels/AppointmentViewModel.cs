using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClinicSystem2.ViewModels;

public class AppointmentViewModel
{
    public int AppointmentID { get; set; }
    public int PatientID { get; set; }
    public int DoctorID { get; set; }

    [Required(ErrorMessage = "Appointment date is required")]
    public DateTime AppointmentDate { get; set; }

    public string Status { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public string PatientName { get; set; }
    public string DoctorName { get; set; }
    public string Specialty { get; set; }
    public string PatientPhone { get; set; }
}

public class AppointmentCreateEditViewModel
{
    public int AppointmentID { get; set; }

    [Required(ErrorMessage = "Patient is required")]
    public int PatientID { get; set; }

    [Required(ErrorMessage = "Doctor is required")]
    public int DoctorID { get; set; }

    [Required(ErrorMessage = "Appointment date is required")]
    public DateTime AppointmentDate { get; set; }

    public string Status { get; set; }
    public string Notes { get; set; }
    [ValidateNever]
    public List<SelectListItem> Patients { get; set; }
    [ValidateNever]

    public List<SelectListItem> Doctors { get; set; }
}

public class AppointmentCompleteViewModel
{
    public int AppointmentID { get; set; }

    [Required(ErrorMessage = "Diagnosis is required")]
    public string Diagnosis { get; set; }

    public string Prescription { get; set; }
    public string Notes { get; set; }

    public string PatientName { get; set; }
    public string DoctorName { get; set; }
    public DateTime AppointmentDate { get; set; }
}

public class AppointmentSearchResult
{
    public int AppointmentId { get; set; }
    public string PatientName { get; set; }
    public string PatientPhone { get; set; }
    public string DoctorName { get; set; }
    public string Specialty { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string AppointmentStatus { get; set; }
    public string Notes { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentStatus { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? CreatedDate { get; set; }
}