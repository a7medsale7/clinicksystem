using ClinicSystem2.Data;
using ClinicSystem2.Models;
using ClinicSystem2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace ClinicSystem2.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Appointment
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.VwAppointmentDetails
                .OrderByDescending(a => a.AppointmentDate)
                .Select(v => new AppointmentViewModel
                {
                    AppointmentID = v.AppointmentId,
                    PatientID = 0,
                    DoctorID = 0,
                    AppointmentDate = v.AppointmentDate,
                    Status = v.AppointmentStatus,
                    Notes = v.Notes,
                    CreatedDate = DateTime.Now,
                    PatientName = v.PatientName,
                    DoctorName = v.DoctorName,
                    Specialty = v.Specialty,
                    PatientPhone = v.PatientPhone
                })
                .ToListAsync();

            return View(appointments);
        }

        // GET: Appointment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.VwAppointmentDetails
                .Where(v => v.AppointmentId == id)
                .Select(v => new AppointmentViewModel
                {
                    AppointmentID = v.AppointmentId,
                    PatientID = 0,
                    DoctorID = 0,
                    AppointmentDate = v.AppointmentDate,
                    Status = v.AppointmentStatus,
                    Notes = v.Notes,
                    CreatedDate = DateTime.Now,
                    PatientName = v.PatientName,
                    DoctorName = v.DoctorName,
                    Specialty = v.Specialty,
                    PatientPhone = v.PatientPhone
                })
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return NotFound();
            }

            // جلب معلومات إضافية
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AppointmentId == id);

            var medicalRecord = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            ViewBag.Payment = payment;
            ViewBag.MedicalRecord = medicalRecord;

            return View(appointment);
        }

        // GET: Appointment/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = await CreateAppointmentViewModel();
            return View(viewModel);
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateEditViewModel viewModel)
        {
            // إعادة تعبئة القوائم المنسدلة أولاً
            viewModel.Patients = await GetPatientsList();
            viewModel.Doctors = await GetDoctorsList();

            if (ModelState.IsValid)
            {
                try
                {
                    // التحقق من وجود المريض والطبيب
                    var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == viewModel.PatientID);
                    var doctorExists = await _context.Doctors.AnyAsync(d => d.DoctorId == viewModel.DoctorID && d.IsActive == true);

                    if (!patientExists)
                    {
                        ModelState.AddModelError("PatientID", "Selected patient does not exist.");
                        return View(viewModel);
                    }

                    if (!doctorExists)
                    {
                        ModelState.AddModelError("DoctorID", "Selected doctor does not exist or is not active.");
                        return View(viewModel);
                    }

                    // التحقق من التوفر يدوياً
                    var isAvailable = await IsTimeSlotAvailable(viewModel.DoctorID, viewModel.AppointmentDate);
                    if (!isAvailable)
                    {
                        ModelState.AddModelError("", "Doctor already has an appointment at this time. Please choose a different time.");
                        return View(viewModel);
                    }

                    // إنشاء الموعد باستخدام الـEntity Framework
                    var appointment = new Appointment
                    {
                        PatientId = viewModel.PatientID,
                        DoctorId = viewModel.DoctorID,
                        AppointmentDate = viewModel.AppointmentDate,
                        Status = viewModel.Status ?? "Pending",
                        Notes = viewModel.Notes,
                        CreatedDate = DateTime.Now
                    };

                    _context.Appointments.Add(appointment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Appointment created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    // الـTrigger سيمنع الازدواجية
                    if (ex.InnerException != null && (ex.InnerException.Message.Contains("double booking") || ex.InnerException.Message.Contains("already has an appointment")))
                    {
                        ModelState.AddModelError("", "Doctor already has an appointment at this time. Please choose a different time.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error creating appointment. Please check the provided information.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating appointment: {ex.Message}");
                }
            }

            return View(viewModel);
        }

        // GET: Appointment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            var viewModel = new AppointmentCreateEditViewModel
            {
                AppointmentID = appointment.AppointmentId,
                PatientID = appointment.PatientId,
                DoctorID = appointment.DoctorId,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                Notes = appointment.Notes,
                Patients = await GetPatientsList(),
                Doctors = await GetDoctorsList()
            };

            return View(viewModel);
        }

        // POST: Appointment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppointmentCreateEditViewModel viewModel)
        {
            // إعادة تعبئة القوائم المنسدلة أولاً
            viewModel.Patients = await GetPatientsList();
            viewModel.Doctors = await GetDoctorsList();

            if (id != viewModel.AppointmentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var appointment = await _context.Appointments.FindAsync(id);
                    if (appointment == null)
                    {
                        return NotFound();
                    }

                    // التحقق من عدم التعديل إذا كان هناك سجل طبي مرتبط
                    var hasMedicalRecord = await _context.MedicalRecords
                        .AnyAsync(m => m.AppointmentId == id);

                    if (hasMedicalRecord && (appointment.PatientId != viewModel.PatientID || appointment.DoctorId != viewModel.DoctorID))
                    {
                        ModelState.AddModelError("", "Cannot change patient or doctor for an appointment that has a medical record.");
                        return View(viewModel);
                    }

                    // التحقق من التوفر إذا تم تغيير الطبيب أو الوقت
                    if (appointment.DoctorId != viewModel.DoctorID || appointment.AppointmentDate != viewModel.AppointmentDate)
                    {
                        var isAvailable = await IsTimeSlotAvailable(viewModel.DoctorID, viewModel.AppointmentDate, id);
                        if (!isAvailable)
                        {
                            ModelState.AddModelError("", "Doctor already has an appointment at this time. Please choose a different time.");
                            return View(viewModel);
                        }
                    }

                    appointment.PatientId = viewModel.PatientID;
                    appointment.DoctorId = viewModel.DoctorID;
                    appointment.AppointmentDate = viewModel.AppointmentDate;
                    appointment.Status = viewModel.Status;
                    appointment.Notes = viewModel.Notes;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Appointment updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(viewModel.AppointmentID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException != null && (ex.InnerException.Message.Contains("double booking") || ex.InnerException.Message.Contains("already has an appointment")))
                    {
                        ModelState.AddModelError("", "Doctor already has an appointment at this time. Please choose a different time.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error updating appointment. Please check the provided information.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating appointment: {ex.Message}");
                }
            }

            return View(viewModel);
        }

        // GET: Appointment/Complete/5
        public async Task<IActionResult> Complete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.VwAppointmentDetails
                .Where(v => v.AppointmentId == id)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return NotFound();
            }

            // التحقق إذا كان الموعد مكتملاً بالفعل
            var existingMedicalRecord = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (existingMedicalRecord != null)
            {
                TempData["ErrorMessage"] = "This appointment already has a medical record and is marked as completed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new AppointmentCompleteViewModel
            {
                AppointmentID = appointment.AppointmentId,
                PatientName = appointment.PatientName,
                DoctorName = appointment.DoctorName,
                AppointmentDate = appointment.AppointmentDate
            };

            return View(viewModel);
        }

        // POST: Appointment/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, AppointmentCompleteViewModel viewModel)
        {
            if (id != viewModel.AppointmentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // استخدام الـEntity Framework بدلاً من الـStored Procedure
                    var appointment = await _context.Appointments
                        .Include(a => a.Patient)
                        .Include(a => a.Doctor)
                        .FirstOrDefaultAsync(a => a.AppointmentId == id);

                    if (appointment == null)
                    {
                        ModelState.AddModelError("", "Appointment not found.");
                        return View(viewModel);
                    }

                    // تحديث حالة الموعد
                    appointment.Status = "Completed";

                    // إنشاء السجل الطبي
                    var medicalRecord = new MedicalRecord
                    {
                        PatientId = appointment.PatientId,
                        DoctorId = appointment.DoctorId,
                        AppointmentId = appointment.AppointmentId,
                        Diagnosis = viewModel.Diagnosis,
                        Prescription = viewModel.Prescription,
                        Notes = viewModel.Notes,
                        RecordDate = DateTime.Now
                    };

                    _context.MedicalRecords.Add(medicalRecord);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Appointment completed and medical record created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error completing appointment: {ex.Message}");
                }
            }

            var appointmentDetails = await _context.VwAppointmentDetails
                .Where(v => v.AppointmentId == id)
                .FirstOrDefaultAsync();

            if (appointmentDetails != null)
            {
                viewModel.PatientName = appointmentDetails.PatientName;
                viewModel.DoctorName = appointmentDetails.DoctorName;
                viewModel.AppointmentDate = appointmentDetails.AppointmentDate;
            }

            return View(viewModel);
        }

        // GET: Appointment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.VwAppointmentDetails
                .Where(v => v.AppointmentId == id)
                .Select(v => new AppointmentViewModel
                {
                    AppointmentID = v.AppointmentId,
                    AppointmentDate = v.AppointmentDate,
                    Status = v.AppointmentStatus,
                    Notes = v.Notes,
                    PatientName = v.PatientName,
                    DoctorName = v.DoctorName,
                    Specialty = v.Specialty,
                    PatientPhone = v.PatientPhone
                })
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return NotFound();
            }

            // التحقق من السجلات المرتبطة
            var hasMedicalRecord = await _context.MedicalRecords
                .AnyAsync(m => m.AppointmentId == id);

            var hasPayment = await _context.Payments
                .AnyAsync(p => p.AppointmentId == id);

            ViewBag.HasMedicalRecord = hasMedicalRecord;
            ViewBag.HasPayment = hasPayment;

            return View(appointment);
        }

        // POST: Appointment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.MedicalRecords)
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment != null)
            {
                try
                {
                    // حذف السجلات المرتبطة أولاً
                    if (appointment.MedicalRecords.Any())
                    {
                        _context.MedicalRecords.RemoveRange(appointment.MedicalRecords);
                    }

                    if (appointment.Payment != null)
                    {
                        _context.Payments.Remove(appointment.Payment);
                    }

                    // ثم حذف الموعد
                    _context.Appointments.Remove(appointment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Appointment deleted successfully!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error deleting appointment: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // استخدام الـ Stored Procedure للبحث
        public async Task<IActionResult> SearchByDoctor(string doctorName, DateTime date)
        {
            if (string.IsNullOrEmpty(doctorName))
            {
                doctorName = "";
            }

            List<AppointmentViewModel> appointments = new List<AppointmentViewModel>();

            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_FindAppointmentForDoctor";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@DoctorName", doctorName));
                command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Date", date.ToString("yyyy-MM-dd")));

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    try
                    {
                        var appointment = new AppointmentViewModel
                        {
                            AppointmentID = GetSafeInt32(reader, "AppointmentID"),
                            PatientID = GetSafeInt32(reader, "PatientID"),
                            DoctorID = GetSafeInt32(reader, "DoctorID"),
                            PatientName = GetSafeString(reader, "PatientName"),
                            DoctorName = GetSafeString(reader, "DoctorName"),
                            Specialty = GetSafeString(reader, "Specialty"),
                            AppointmentDate = GetSafeDateTime(reader, "AppointmentDate") ?? DateTime.Now,
                            Status = GetSafeString(reader, "Status"),
                            Notes = GetSafeString(reader, "Notes"),
                            PatientPhone = GetSafeString(reader, "PatientPhone"),
                            CreatedDate = GetSafeDateTime(reader, "CreatedDate") ?? DateTime.Now
                        };

                        appointments.Add(appointment);
                    }
                    catch (Exception innerEx)
                    {
                        // تجاهل الصف الذي به خطأ والمضي للصف التالي
                        continue;
                    }
                }

                ViewBag.SearchDoctorName = doctorName;
                ViewBag.SearchDate = date;

                if (!appointments.Any())
                {
                    TempData["InfoMessage"] = "No appointments found for the specified criteria.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Found {appointments.Count} appointment(s) using stored procedure.";
                }

                return View("Index", appointments);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Search failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper methods for safe data reading
        private int GetSafeInt32(DbDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
            }
            catch
            {
                return 0;
            }
        }

        private string GetSafeString(DbDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
            }
            catch
            {
                return "";
            }
        }

        private DateTime? GetSafeDateTime(DbDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch
            {
                return DateTime.Now;
            }
        }
        // استخدام الـ View لعرض جدول اليوم
        public async Task<IActionResult> TodaySchedule()
        {
            var todaySchedule = await _context.VwDoctorScheduleTodays
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return View(todaySchedule);
        }

        // AJAX: Check time slot availability
        [HttpPost]
        public async Task<JsonResult> CheckTimeSlotAvailability(int doctorId, DateTime proposedDateTime, int? excludeAppointmentId = null)
        {
            try
            {
                var isAvailable = await IsTimeSlotAvailable(doctorId, proposedDateTime, excludeAppointmentId);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Helper method to check time slot availability
        private async Task<bool> IsTimeSlotAvailable(int doctorId, DateTime proposedDateTime, int? excludeAppointmentId = null)
        {
            var query = _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate == proposedDateTime &&
                           a.Status != "Cancelled");

            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.AppointmentId != excludeAppointmentId.Value);
            }

            return !await query.AnyAsync();
        }

        // Helper methods for dropdown lists
        private async Task<List<SelectListItem>> GetPatientsList()
        {
            return await _context.Patients
                .Select(p => new SelectListItem
                {
                    Value = p.PatientId.ToString(),
                    Text = p.FullName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetDoctorsList()
        {
            return await _context.Doctors
                .Where(d => d.IsActive == true)
                .Select(d => new SelectListItem
                {
                    Value = d.DoctorId.ToString(),
                    Text = $"{d.FullName} - {d.Specialty}"
                })
                .ToListAsync();
        }

        private async Task<AppointmentCreateEditViewModel> CreateAppointmentViewModel()
        {
            return new AppointmentCreateEditViewModel
            {
                Patients = await GetPatientsList(),
                Doctors = await GetDoctorsList(),
                AppointmentDate = DateTime.Now.AddDays(1).Date.AddHours(9),
                Status = "Pending"
            };
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentId == id);
        }
    }
}