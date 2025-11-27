using System.Diagnostics;
using ClinicSystem2.Data;
using ClinicSystem2.Models;
using ClinicSystem2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ClinicSystem2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = await GetDashboardData();
            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Dashboard Data
        public async Task<IActionResult> Dashboard()
        {
            var dashboardData = await GetDashboardData();
            return View(dashboardData);
        }

        // Today's Appointments using View
        public async Task<IActionResult> TodayAppointments()
        {
            var todayAppointments = await _context.VwDoctorScheduleTodays
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return View(todayAppointments);
        }

        // Available Slots using View
        public async Task<IActionResult> AvailableSlots()
        {
            var availableSlots = await _context.VwDoctorAvailableSlots.ToListAsync();
            return View(availableSlots);
        }

        // Quick Patient Registration using Stored Procedure
        public IActionResult QuickRegister()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickRegister(PatientCreateEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // استخدام الـ Stored Procedure لتسجيل المريض
                    var result = await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_RegisterPatient @FullName, @Email, @Phone, @BirthDate, @Gender, @Address",
                        new SqlParameter("@FullName", viewModel.FullName),
                        new SqlParameter("@Email", viewModel.Email ?? (object)DBNull.Value),
                        new SqlParameter("@Phone", viewModel.Phone ?? (object)DBNull.Value),
                        new SqlParameter("@BirthDate", viewModel.BirthDate),
                        new SqlParameter("@Gender", viewModel.Gender),
                        new SqlParameter("@Address", viewModel.Address ?? (object)DBNull.Value)
                    );

                    TempData["SuccessMessage"] = "Patient registered successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error registering patient: {ex.Message}");
                }
            }
            return View(viewModel);
        }

        // Quick Appointment using Stored Procedure
        public async Task<IActionResult> QuickAppointment()
        {
            var viewModel = new AppointmentCreateEditViewModel
            {
                Patients = await _context.Patients
                    .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = p.PatientId.ToString(),
                        Text = p.FullName
                    })
                    .ToListAsync(),
                Doctors = await _context.Doctors
                    .Where(d => d.IsActive == true)
                    .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = d.DoctorId.ToString(),
                        Text = $"{d.FullName} - {d.Specialty}"
                    })
                    .ToListAsync(),
                AppointmentDate = DateTime.Now.AddDays(1).Date.AddHours(9)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAppointment(AppointmentCreateEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // استخدام الـ Stored Procedure مع الـ Trigger لمنع الازدواجية
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_AddAppointment @PatientID, @DoctorID, @AppointmentDate, @Notes",
                        new SqlParameter("@PatientID", viewModel.PatientID),
                        new SqlParameter("@DoctorID", viewModel.DoctorID),
                        new SqlParameter("@AppointmentDate", viewModel.AppointmentDate),
                        new SqlParameter("@Notes", viewModel.Notes ?? (object)DBNull.Value)
                    );

                    TempData["SuccessMessage"] = "Appointment created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating appointment: {ex.Message}");
                }
            }

            // إعادة تعبئة القوائم المنسدلة في حالة الخطأ
            viewModel.Patients = await _context.Patients
                .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = p.PatientId.ToString(),
                    Text = p.FullName
                })
                .ToListAsync();

            viewModel.Doctors = await _context.Doctors
                .Where(d => d.IsActive == true)
                .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = d.DoctorId.ToString(),
                    Text = $"{d.FullName} - {d.Specialty}"
                })
                .ToListAsync();

            return View(viewModel);
        }

        // Financial Overview using View
        public async Task<IActionResult> FinancialOverview()
        {
            var monthlyRevenue = await _context.VwMonthlyRevenues
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .Take(12)
                .ToListAsync();

            return View(monthlyRevenue);
        }

        // Search Appointments using Stored Procedure
        public IActionResult SearchAppointments()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchAppointments(string doctorName, DateTime date)
        {
            var appointments = await _context.Database.SqlQueryRaw<VwAppointmentDetail>(
                "EXEC sp_FindAppointmentForDoctor @DoctorName, @Date",
                new SqlParameter("@DoctorName", doctorName),
                new SqlParameter("@Date", date.ToString("yyyy-MM-dd"))
            ).ToListAsync();

            return View("SearchResults", appointments);
        }

        // System Statistics using Functions
        public async Task<IActionResult> SystemStats()
        {
            var stats = new SystemStatsViewModel();

            // إحصائيات المرضى
            stats.TotalPatients = await _context.Patients.CountAsync();

            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            stats.NewPatientsThisMonth = await _context.Patients
                .Where(p => p.CreatedDate >= firstDayOfMonth)
                .CountAsync();

            // إحصائيات الأطباء
            stats.TotalDoctors = await _context.Doctors.CountAsync(d => d.IsActive == true);
            stats.ActiveDoctors = stats.TotalDoctors;

            // إحصائيات المواعيد
            stats.TotalAppointments = await _context.Appointments.CountAsync();

            var today = DateTime.Today;
            stats.TodayAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Date == today && a.Status != "Cancelled")
                .CountAsync();

            stats.PendingAppointments = await _context.Appointments
                .CountAsync(a => a.Status == "Pending");

            // إحصائيات المدفوعات - التصحيح هنا
            stats.TotalRevenue = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => p.Amount) ?? 0;

            stats.MonthlyRevenue = await _context.Payments
                .Where(p => p.Status == "Paid" && p.PaymentDate >= firstDayOfMonth)
                .SumAsync(p => p.Amount) ?? 0;

            return View(stats);
        }

        // AJAX Methods for Dashboard
        public async Task<JsonResult> GetTodayAppointmentsCount()
        {
            var count = await _context.VwDoctorScheduleTodays.CountAsync();
            return Json(new { count });
        }

        public async Task<JsonResult> GetAvailableDoctors()
        {
            var doctors = await _context.VwDoctorAvailableSlots
                .Select(d => new { d.DoctorName, d.Specialty })
                .ToListAsync();
            return Json(doctors);
        }

        public async Task<JsonResult> GetMonthlyRevenueData()
        {
            var revenueData = await _context.VwMonthlyRevenues
                .Where(m => m.Year == DateTime.Now.Year)
                .OrderBy(m => m.Month)
                .Select(m => new
                {
                    Month = m.Month,
                    Revenue = m.TotalRevenue
                })
                .ToListAsync();

            return Json(revenueData);
        }

        public async Task<JsonResult> GetDoctorPerformance()
        {
            var performance = await _context.Doctors
                .Where(d => d.IsActive == true)
                .Select(d => new
                {
                    DoctorName = d.FullName,
                    AppointmentCount = d.Appointments.Count(a => a.Status != "Cancelled"),
                    Revenue = d.Appointments
                        .Where(a => a.Payment != null && a.Payment.Status == "Paid")
                        .Sum(a => a.Payment.Amount ?? 0)
                })
                .OrderByDescending(d => d.Revenue)
                .Take(5)
                .ToListAsync();

            return Json(performance);
        }

        // Check Slot Availability using Function
        [HttpPost]
        public async Task<JsonResult> CheckSlotAvailability(int doctorId, DateTime dateTime)
        {
            var isAvailable = await _context.Database.SqlQueryRaw<bool>(
                "SELECT dbo.fn_IsTimeSlotAvailable({0}, {1})",
                doctorId,
                dateTime
            ).FirstOrDefaultAsync();

            return Json(new { isAvailable });
        }

        // Calculate Age using Function
        [HttpPost]
        public async Task<JsonResult> CalculateAge(DateTime birthDate)
        {
            var age = await _context.Database.SqlQueryRaw<int>(
                "SELECT dbo.fn_CalculateAge({0})",
                birthDate.ToString("yyyy-MM-dd")
            ).FirstOrDefaultAsync();

            return Json(new { age });
        }

        // Get Next Appointment using Function
        [HttpPost]
        public async Task<JsonResult> GetNextAppointment(int patientId)
        {
            var nextAppointment = await _context.Database.SqlQueryRaw<DateTime?>(
                "SELECT dbo.fn_GetPatientNextAppointment({0})",
                patientId
            ).FirstOrDefaultAsync();

            return Json(new
            {
                nextAppointment = nextAppointment?.ToString("yyyy-MM-dd HH:mm") ?? "No upcoming appointments"
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<DashboardViewModel> GetDashboardData()
        {
            var dashboard = new DashboardViewModel();

            // الإحصائيات الأساسية
            dashboard.TotalPatients = await _context.Patients.CountAsync();
            dashboard.TotalDoctors = await _context.Doctors.CountAsync(d => d.IsActive == true);
            dashboard.TodayAppointments = await _context.VwDoctorScheduleTodays.CountAsync();

            // التصحيح: استخدام متغيرات بدلاً من DateTime.Now مباشرة في الـ LINQ
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var today = DateTime.Today;

            // الإيرادات - التصحيح هنا
            dashboard.TotalRevenue = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => p.Amount) ?? 0;

            dashboard.MonthlyRevenue = await _context.Payments
                .Where(p => p.Status == "Paid" && p.PaymentDate >= firstDayOfMonth)
                .SumAsync(p => p.Amount) ?? 0;

            // المواعيد القادمة
            dashboard.UpcomingAppointments = await _context.VwAppointmentDetails
                .Where(a => a.AppointmentDate >= DateTime.Now && a.AppointmentStatus != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .Select(v => new AppointmentViewModel
                {
                    AppointmentID = v.AppointmentId,
                    AppointmentDate = v.AppointmentDate,
                    Status = v.AppointmentStatus,
                    PatientName = v.PatientName,
                    DoctorName = v.DoctorName,
                    Specialty = v.Specialty
                })
                .ToListAsync();

            // الأطباء المتاحين
            dashboard.AvailableDoctors = await _context.VwDoctorAvailableSlots
                .Take(5)
                .ToListAsync();

            // الإيرادات الشهرية
            dashboard.MonthlyRevenueData = await _context.VwMonthlyRevenues
                .Where(m => m.Year == DateTime.Now.Year)
                .OrderBy(m => m.Month)
                .Select(m => new MonthlyRevenueViewModel
                {
                    Month = m.Month ?? 0,
                    TotalRevenue = m.TotalRevenue ?? 0,
                    TotalTransactions = m.TotalTransactions ?? 0
                })
                .ToListAsync();

            // سجلات النظام
            dashboard.RecentLogs = await _context.AppointmentLogs
                .OrderByDescending(l => l.LogDate)
                .Take(10)
                .ToListAsync();

            return dashboard;
        }
    }

    // View Models for Dashboard
    public class DashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TodayAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<AppointmentViewModel> UpcomingAppointments { get; set; } = new();
        public List<VwDoctorAvailableSlot> AvailableDoctors { get; set; } = new();
        public List<MonthlyRevenueViewModel> MonthlyRevenueData { get; set; } = new();
        public List<AppointmentLog> RecentLogs { get; set; } = new();
    }

    public class SystemStatsViewModel
    {
        public int TotalPatients { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public int TotalDoctors { get; set; }
        public int ActiveDoctors { get; set; }
        public int TotalAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }
}