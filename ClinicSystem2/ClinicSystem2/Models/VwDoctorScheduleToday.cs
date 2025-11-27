using System;
using System.Collections.Generic;

namespace ClinicSystem2.Models;

public partial class VwDoctorScheduleToday
{
    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = null!;

    public string? Specialty { get; set; }

    public int AppointmentId { get; set; }

    public string PatientName { get; set; } = null!;

    public DateTime AppointmentDate { get; set; }

    public string? Status { get; set; }
}
