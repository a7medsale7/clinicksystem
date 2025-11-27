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
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Patient
        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .Select(p => new PatientViewModel
                {
                    PatientID = p.PatientId,
                    FullName = p.FullName,
                    Email = p.Email,
                    Phone = p.Phone,
                    BirthDate = p.BirthDate.HasValue ?
                        new DateTime(p.BirthDate.Value.Year, p.BirthDate.Value.Month, p.BirthDate.Value.Day) :
                        (DateTime?)null,
                    Gender = p.Gender,
                    Address = p.Address,
                    CreatedDate = p.CreatedDate ?? DateTime.Now,
                    Age = p.BirthDate.HasValue ?
                        DateTime.Now.Year - p.BirthDate.Value.Year -
                        (DateTime.Now.DayOfYear < p.BirthDate.Value.DayOfYear ? 1 : 0) :
                        (int?)null
                })
                .ToListAsync();

            return View(patients);
        }

        // GET: Patient/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // استخدام الـ View للحصول على التاريخ الطبي
            var medicalHistory = await _context.VwPatientMedicalHistories
                .Where(v => v.PatientId == id)
                .ToListAsync();

            var patient = await _context.Patients
                .Select(p => new PatientViewModel
                {
                    PatientID = p.PatientId,
                    FullName = p.FullName,
                    Email = p.Email,
                    Phone = p.Phone,
                    BirthDate = p.BirthDate.HasValue ?
                        new DateTime(p.BirthDate.Value.Year, p.BirthDate.Value.Month, p.BirthDate.Value.Day) :
                        (DateTime?)null,
                    Gender = p.Gender,
                    Address = p.Address,
                    CreatedDate = p.CreatedDate ?? DateTime.Now,
                    Age = p.BirthDate.HasValue ?
                        DateTime.Now.Year - p.BirthDate.Value.Year -
                        (DateTime.Now.DayOfYear < p.BirthDate.Value.DayOfYear ? 1 : 0) :
                        (int?)null
                })
                .FirstOrDefaultAsync(m => m.PatientID == id);

            if (patient == null)
            {
                return NotFound();
            }

            ViewBag.MedicalHistory = medicalHistory;
            return View(patient);
        }

        // GET: Patient/Create
        public IActionResult Create()
        {
            var viewModel = new PatientCreateEditViewModel();
            return View(viewModel);
        }

        // POST: Patient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientCreateEditViewModel viewModel)
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
                    ModelState.AddModelError("", $"Error creating patient: {ex.Message}");
                }
            }
            return View(viewModel);
        }

        // GET: Patient/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var viewModel = new PatientCreateEditViewModel
            {
                PatientID = patient.PatientId,
                FullName = patient.FullName,
                Email = patient.Email,
                Phone = patient.Phone,
                BirthDate = patient.BirthDate.HasValue ?
                    new DateTime(patient.BirthDate.Value.Year, patient.BirthDate.Value.Month, patient.BirthDate.Value.Day) :
                    DateTime.Now,
                Gender = patient.Gender,
                Address = patient.Address
            };

            return View(viewModel);
        }

        // POST: Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientCreateEditViewModel viewModel)
        {
            if (id != viewModel.PatientID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var patient = await _context.Patients.FindAsync(id);
                    if (patient == null)
                    {
                        return NotFound();
                    }

                    patient.FullName = viewModel.FullName;
                    patient.Email = viewModel.Email;
                    patient.Phone = viewModel.Phone;
                    patient.BirthDate = DateOnly.FromDateTime(viewModel.BirthDate);
                    patient.Gender = viewModel.Gender;
                    patient.Address = viewModel.Address;

                    // الـ Trigger هيطبق التحقق من العمر تلقائياً
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Patient updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(viewModel.PatientID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating patient: {ex.Message}");
                }
            }
            return View(viewModel);
        }

        // GET: Patient/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Select(p => new PatientViewModel
                {
                    PatientID = p.PatientId,
                    FullName = p.FullName,
                    Email = p.Email,
                    Phone = p.Phone,
                    BirthDate = p.BirthDate.HasValue ?
                        new DateTime(p.BirthDate.Value.Year, p.BirthDate.Value.Month, p.BirthDate.Value.Day) :
                        (DateTime?)null,
                    Gender = p.Gender,
                    Address = p.Address,
                    CreatedDate = p.CreatedDate ?? DateTime.Now
                })
                .FirstOrDefaultAsync(m => m.PatientID == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Patient deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // استخدام الـ Function لحساب العمر
        public async Task<IActionResult> CalculateAge(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null || !patient.BirthDate.HasValue)
            {
                return Json(new { age = "N/A" });
            }

            var age = await _context.Database.SqlQueryRaw<int>(
                "SELECT dbo.fn_CalculateAge({0})",
                patient.BirthDate.Value.ToString("yyyy-MM-dd")
            ).FirstOrDefaultAsync();

            return Json(new { age = age });
        }

        // استخدام الـ Function للحصول على الموعد القادم
        public async Task<IActionResult> GetNextAppointment(int patientId)
        {
            var nextAppointment = await _context.Database.SqlQueryRaw<DateTime?>(
                "SELECT dbo.fn_GetPatientNextAppointment({0})",
                patientId
            ).FirstOrDefaultAsync();

            return Json(new { nextAppointment = nextAppointment?.ToString("yyyy-MM-dd HH:mm") ?? "No upcoming appointments" });
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }
    }
}