using System;
using System.Collections.Generic;

namespace ClinicSystem2.Models;

public partial class AppointmentLog
{
    public int LogId { get; set; }

    public int? AppointmentId { get; set; }

    public string? LogMessage { get; set; }

    public DateTime? LogDate { get; set; }
}
