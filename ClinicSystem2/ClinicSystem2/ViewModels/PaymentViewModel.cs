using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClinicSystem2.ViewModels;

public class PaymentViewModel
{
    public int PaymentID { get; set; }
    public int AppointmentID { get; set; }

    [Required(ErrorMessage = "Payment date is required")]
    public DateTime PaymentDate { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Amount cannot be negative")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public string Method { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; }

    public string TransactionReference { get; set; }

    // Navigation properties
    public string PatientName { get; set; }
    public string DoctorName { get; set; }
}

public class PaymentCreateEditViewModel
{
    public int PaymentID { get; set; }

    [Required(ErrorMessage = "Appointment is required")]
    public int AppointmentID { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Amount cannot be negative")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public string Method { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; }

    public string TransactionReference { get; set; }
    [ValidateNever]
    public List<SelectListItem> Appointments { get; set; }
    public DateTime? PaymentDate { get; internal set; }
}