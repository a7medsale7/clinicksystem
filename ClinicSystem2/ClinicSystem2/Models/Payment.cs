using System;
using System.Collections.Generic;

namespace ClinicSystem2.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int AppointmentId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal? Amount { get; set; }

    public string? Method { get; set; }

    public string? Status { get; set; }

    public string? TransactionReference { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
}
