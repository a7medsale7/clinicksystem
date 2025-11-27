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
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctor
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Select(d => new DoctorViewModel
                {
                    DoctorID = d.DoctorId,
                    FullName = d.FullName,
                    Specialty = d.Specialty,
                    Description = d.Description,
                    ConsultationFee = d.ConsultationFee ?? 0,
                    IsActive = d.IsActive ?? true,
                    AppointmentCount = d.Appointments.Count(a => a.Status != "Cancelled"),
                    TotalRevenue = d.Appointments
                        .Where(a => a.Payment != null && a.Payment.Status == "Paid")
                        .Sum(a => a.Payment.Amount ?? 0)
                })
                .ToListAsync();

            return View(doctors);
        }

        // GET: Doctor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // استخدام الـ View للحصول على جدول اليوم
            var todaySchedule = await _context.VwDoctorScheduleTodays
                .Where(v => v.DoctorId == id)
                .ToListAsync();

            var doctor = await _context.Doctors
                .Select(d => new DoctorViewModel
                {
                    DoctorID = d.DoctorId,
                    FullName = d.FullName,
                    Specialty = d.Specialty,
                    Description = d.Description,
                    ConsultationFee = d.ConsultationFee ?? 0,
                    IsActive = d.IsActive ?? true,
                    AppointmentCount = d.Appointments.Count(a => a.Status != "Cancelled"),
                    TotalRevenue = d.Appointments
                        .Where(a => a.Payment != null && a.Payment.Status == "Paid")
                        .Sum(a => a.Payment.Amount ?? 0)
                })
                .FirstOrDefaultAsync(m => m.DoctorID == id);

            if (doctor == null)
            {
                return NotFound();
            }

            ViewBag.TodaySchedule = todaySchedule;
            return View(doctor);
        }

        // GET: Doctor/Create
        public IActionResult Create()
        {
            var viewModel = new DoctorCreateEditViewModel();
            return View(viewModel);
        }

        // POST: Doctor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorCreateEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var doctor = new Doctor
                {
                    FullName = viewModel.FullName,
                    Specialty = viewModel.Specialty,
                    Description = viewModel.Description,
                    ConsultationFee = viewModel.ConsultationFee,
                    IsActive = viewModel.IsActive
                };

                _context.Add(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Doctor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            var viewModel = new DoctorCreateEditViewModel
            {
                DoctorID = doctor.DoctorId,
                FullName = doctor.FullName,
                Specialty = doctor.Specialty,
                Description = doctor.Description,
                ConsultationFee = doctor.ConsultationFee ?? 0,
                IsActive = doctor.IsActive ?? true
            };

            return View(viewModel);
        }

        // POST: Doctor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorCreateEditViewModel viewModel)
        {
            if (id != viewModel.DoctorID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var doctor = await _context.Doctors.FindAsync(id);
                    if (doctor == null)
                    {
                        return NotFound();
                    }

                    doctor.FullName = viewModel.FullName;
                    doctor.Specialty = viewModel.Specialty;
                    doctor.Description = viewModel.Description;
                    doctor.ConsultationFee = viewModel.ConsultationFee;
                    doctor.IsActive = viewModel.IsActive;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Doctor updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(viewModel.DoctorID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(viewModel);
        }

        // GET: Doctor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Select(d => new DoctorViewModel
                {
                    DoctorID = d.DoctorId,
                    FullName = d.FullName,
                    Specialty = d.Specialty,
                    Description = d.Description,
                    ConsultationFee = d.ConsultationFee ?? 0,
                    IsActive = d.IsActive ?? true
                })
                .FirstOrDefaultAsync(m => m.DoctorID == id);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // استخدام الـ Function لحساب عدد المواعيد
        public async Task<IActionResult> GetAppointmentCount(int doctorId, DateTime startDate, DateTime endDate)
        {
            var count = await _context.Database.SqlQueryRaw<int>(
                "SELECT dbo.fn_GetDoctorAppointmentCount({0}, {1}, {2})",
                doctorId,
                startDate.ToString("yyyy-MM-dd"),
                endDate.ToString("yyyy-MM-dd")
            ).FirstOrDefaultAsync();

            return Json(new { appointmentCount = count });
        }

        // استخدام الـ Function للتحقق من التوفر
        public async Task<IActionResult> CheckTimeSlotAvailability(int doctorId, DateTime proposedDateTime)
        {
            var isAvailable = await _context.Database.SqlQueryRaw<bool>(
                "SELECT dbo.fn_IsTimeSlotAvailable({0}, {1})",
                doctorId,
                proposedDateTime
            ).FirstOrDefaultAsync();

            return Json(new { isAvailable = isAvailable });
        }

        // استخدام الـ Function لحساب الإيرادات
        public async Task<IActionResult> GetDoctorRevenue(int doctorId, DateTime startDate, DateTime endDate)
        {
            var revenue = await _context.Database.SqlQueryRaw<decimal>(
                "SELECT dbo.fn_GetDoctorRevenue({0}, {1}, {2})",
                doctorId,
                startDate.ToString("yyyy-MM-dd"),
                endDate.ToString("yyyy-MM-dd")
            ).FirstOrDefaultAsync();

            return Json(new { totalRevenue = revenue });
        }

        // استخدام الـ View للعرض المتاح
        public async Task<IActionResult> AvailableSlots()
        {
            var availableSlots = await _context.VwDoctorAvailableSlots.ToListAsync();
            return View(availableSlots);
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.DoctorId == id);
        }
    }
}