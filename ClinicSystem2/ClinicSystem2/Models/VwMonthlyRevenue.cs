using System;
using System.Collections.Generic;

namespace ClinicSystem2.Models;

public partial class VwMonthlyRevenue
{
    public int? Year { get; set; }

    public int? Month { get; set; }

    public int? TotalTransactions { get; set; }

    public decimal? TotalRevenue { get; set; }

    public decimal? AveragePayment { get; set; }

    public string? PaymentMethod { get; set; }
}
