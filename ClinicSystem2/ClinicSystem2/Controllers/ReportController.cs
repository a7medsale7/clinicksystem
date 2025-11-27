using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicSystem2.Data;
using ClinicSystem2.Models;
using ClinicSystem2.ViewModels;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace ClinicSystem2.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Report/Financial
        public async Task<IActionResult> Financial(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var financialReports = await _context.Database.SqlQueryRaw<FinancialReportViewModel>(
                "EXEC sp_GenerateFinancialReport @StartDate, @EndDate",
                new SqlParameter("@StartDate", startDate.Value.ToString("yyyy-MM-dd")),
                new SqlParameter("@EndDate", endDate.Value.ToString("yyyy-MM-dd"))
            ).ToListAsync();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(financialReports);
        }

        // GET: Report/MonthlyRevenue
        public async Task<IActionResult> MonthlyRevenue()
        {
            var monthlyRevenue = await _context.VwMonthlyRevenues
                .Select(v => new MonthlyRevenueViewModel
                {
                    Year = v.Year ?? DateTime.Now.Year,
                    Month = v.Month ?? DateTime.Now.Month,
                    TotalTransactions = v.TotalTransactions ?? 0,
                    TotalRevenue = v.TotalRevenue ?? 0,
                    AveragePayment = v.AveragePayment ?? 0,
                    PaymentMethod = v.PaymentMethod
                })
                .OrderByDescending(v => v.Year)
                .ThenByDescending(v => v.Month)
                .ToListAsync();

            return View(monthlyRevenue);
        }

        // GET: Report/AppointmentLogs
        public async Task<IActionResult> AppointmentLogs()
        {
            var appointmentLogs = await _context.AppointmentLogs
                .OrderByDescending(l => l.LogDate)
                .ToListAsync();

            return View(appointmentLogs);
        }

        // GET: Report/DoctorPerformance
        public async Task<IActionResult> DoctorPerformance()
        {
            var doctorPerformance = await _context.Doctors
                .Where(d => d.IsActive == true)
                .Select(d => new
                {
                    Doctor = d,
                    TotalAppointments = d.Appointments.Count(a => a.Status != "Cancelled"),
                    CompletedAppointments = d.Appointments.Count(a => a.Status == "Completed"),
                    TotalRevenue = d.Appointments
                        .Where(a => a.Payment != null && a.Payment.Status == "Paid")
                        .Sum(a => a.Payment.Amount ?? 0),
                    AverageRating = 4.5 // يمكن إضافة نظام تقييم في المستقبل
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            var result = doctorPerformance.Select(x => new FinancialReportViewModel
            {
                DoctorID = x.Doctor.DoctorId,
                DoctorName = x.Doctor.FullName,
                Specialty = x.Doctor.Specialty,
                TotalAppointments = x.TotalAppointments,
                PaidAppointments = x.CompletedAppointments,
                TotalRevenue = x.TotalRevenue,
                AverageFee = x.TotalAppointments > 0 ? x.TotalRevenue / x.TotalAppointments : 0
            }).ToList();

            return View(result);
        }

        // GET: Report/PatientStatistics
        public async Task<IActionResult> PatientStatistics()
        {
            var patientStats = await _context.Patients
                .Select(p => new
                {
                    Patient = p,
                    TotalAppointments = p.Appointments.Count(a => a.Status != "Cancelled"),
                    TotalPaid = p.Appointments
                        .Where(a => a.Payment != null && a.Payment.Status == "Paid")
                        .Sum(a => a.Payment.Amount ?? 0),
                    Age = p.BirthDate.HasValue ?
                        DateTime.Now.Year - p.BirthDate.Value.Year -
                        (DateTime.Now.DayOfYear < p.BirthDate.Value.DayOfYear ? 1 : 0) :
                        (int?)null
                })
                .OrderByDescending(x => x.TotalAppointments)
                .ToListAsync();

            var result = patientStats.Select(x => new
            {
                x.Patient.FullName,
                x.Patient.Gender,
                Age = x.Age,
                x.TotalAppointments,
                x.TotalPaid,
                LastAppointment = x.Patient.Appointments
                    .Where(a => a.Status == "Completed")
                    .Max(a => a.AppointmentDate)
            }).ToList();

            return View(result);
        }
    }
}