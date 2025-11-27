using System;
using System.Collections.Generic;

namespace ClinicSystem2.Models;

public partial class VwPatientMedicalHistory
{
    public int PatientId { get; set; }

    public string PatientName { get; set; } = null!;

    public int RecordId { get; set; }

    public string DoctorName { get; set; } = null!;

    public string? Specialty { get; set; }

    public string? Diagnosis { get; set; }

    public string? Prescription { get; set; }

    public DateTime? RecordDate { get; set; }

    public DateTime? AppointmentDate { get; set; }
}
