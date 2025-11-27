using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClinicSystem2.ViewModels;

public class MedicalRecordViewModel
{
    public int RecordID { get; set; }
    public int PatientID { get; set; }
    public int DoctorID { get; set; }
    public int? AppointmentID { get; set; }
    public string Diagnosis { get; set; }
    public string Prescription { get; set; }
    public string Notes { get; set; }
    public DateTime RecordDate { get; set; }

    // Navigation properties
    public string PatientName { get; set; }
    public string DoctorName { get; set; }
    public string Specialty { get; set; }
    public DateTime? AppointmentDate { get; set; }
}

public class MedicalRecordCreateEditViewModel
{
    public int RecordID { get; set; }

    [Required(ErrorMessage = "Patient is required")]
    public int PatientID { get; set; }

    [Required(ErrorMessage = "Doctor is required")]
    public int DoctorID { get; set; }

    public int? AppointmentID { get; set; }

    [Required(ErrorMessage = "Diagnosis is required")]
    public string Diagnosis { get; set; }

    public string Prescription { get; set; }
    public string Notes { get; set; }
    [ValidateNever]
    public List<SelectListItem> Patients { get; set; }
    [ValidateNever]

    public List<SelectListItem> Doctors { get; set; }
    [ValidateNever]

    public List<SelectListItem> Appointments { get; set; }
    public DateTime? RecordDate { get; internal set; }
}