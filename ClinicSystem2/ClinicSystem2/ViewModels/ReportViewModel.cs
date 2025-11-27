namespace ClinicSystem2.ViewModels;

public class FinancialReportViewModel
{
    public int DoctorID { get; set; }
    public string DoctorName { get; set; }
    public string Specialty { get; set; }
    public int TotalAppointments { get; set; }
    public int PaidAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageFee { get; set; }
}

public class MonthlyRevenueViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePayment { get; set; }
    public string PaymentMethod { get; set; }
}
