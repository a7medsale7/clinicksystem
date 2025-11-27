using System;
using System.Collections.Generic;

namespace ClinicSystem2.Models;

public partial class VwAppointmentDetail
{
    public int AppointmentId { get; set; }

    public string PatientName { get; set; } = null!;

    public string? PatientPhone { get; set; }

    public string DoctorName { get; set; } = null!;

    public string? Specialty { get; set; }

    public DateTime AppointmentDate { get; set; }

    public string? AppointmentStatus { get; set; }

    public decimal? PaymentAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? Notes { get; set; }
}
