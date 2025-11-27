using System;
using System.Collections.Generic;

namespace ClinicSystem2.Models;

public partial class VwDoctorAvailableSlot
{
    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = null!;

    public string? Specialty { get; set; }

    public DateTime? SlotStartTime { get; set; }

    public DateTime? SlotEndTime { get; set; }
}
