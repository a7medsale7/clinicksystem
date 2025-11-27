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
    public class MedicalRecordController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicalRecordController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MedicalRecord
        public async Task<IActionResult> Index()
        {
            var medicalRecords = await _context.VwPatientMedicalHistories
                .Select(v => new MedicalRecordViewModel
                {
                    RecordID = v.RecordId,
                    PatientID = v.PatientId,
                    DoctorID = 0, // سيتم تعبئته لاحقاً إذا لزم الأمر
                    AppointmentID = null, // سيتم تعبئته لاحقاً إذا لزم الأمر
                    Diagnosis = v.Diagnosis,
                    Prescription = v.Prescription,
                    Notes = null, // غير متوفر في الـ View
                    RecordDate = v.RecordDate ?? DateTime.Now,
                    PatientName = v.PatientName,
                    DoctorName = v.DoctorName,
                    Specialty = v.Specialty,
                    AppointmentDate = v.AppointmentDate
                })
                .ToListAsync();

            return View(medicalRecords);
        }

        // GET: MedicalRecord/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.Appointment)
                .Select(m => new MedicalRecordViewModel
                {
                    RecordID = m.RecordId,
                    PatientID = m.PatientId,
                    DoctorID = m.DoctorId,
                    AppointmentID = m.AppointmentId,
                    Diagnosis = m.Diagnosis,
                    Prescription = m.Prescription,
                    Notes = m.Notes,
                    RecordDate = m.RecordDate ?? DateTime.Now,
                    PatientName = m.Patient.FullName,
                    DoctorName = m.Doctor.FullName,
                    Specialty = m.Doctor.Specialty,
                    AppointmentDate = m.Appointment != null ? m.Appointment.AppointmentDate : (DateTime?)null
                })
                .FirstOrDefaultAsync(m => m.RecordID == id);

            if (medicalRecord == null)
            {
                return NotFound();
            }

            return View(medicalRecord);
        }

        // GET: MedicalRecord/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new MedicalRecordCreateEditViewModel
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
                Appointments = await _context.Appointments
                    .Where(a => a.Status == "Completed" && a.MedicalRecords.Count == 0)
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Select(a => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = a.AppointmentId.ToString(),
                        Text = $"{a.Patient.FullName} with Dr. {a.Doctor.FullName} on {a.AppointmentDate:yyyy-MM-dd HH:mm}"
                    })
                    .ToListAsync(),
                RecordDate = DateTime.Now
            };

            return View(viewModel);
        }

        // POST: MedicalRecord/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalRecordCreateEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var medicalRecord = new MedicalRecord
                {
                    PatientId = viewModel.PatientID,
                    DoctorId = viewModel.DoctorID,
                    AppointmentId = viewModel.AppointmentID == 0 ? null : viewModel.AppointmentID,
                    Diagnosis = viewModel.Diagnosis,
                    Prescription = viewModel.Prescription,
                    Notes = viewModel.Notes,
                    RecordDate = viewModel.RecordDate
                };

                _context.Add(medicalRecord);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medical record created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // إعادة تعبئة القوائم المنسدلة في حالة الخطأ
            await PopulateDropdowns(viewModel);
            return View(viewModel);
        }

        // GET: MedicalRecord/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            if (medicalRecord == null)
            {
                return NotFound();
            }

            var viewModel = new MedicalRecordCreateEditViewModel
            {
                RecordID = medicalRecord.RecordId,
                PatientID = medicalRecord.PatientId,
                DoctorID = medicalRecord.DoctorId,
                AppointmentID = medicalRecord.AppointmentId,
                Diagnosis = medicalRecord.Diagnosis,
                Prescription = medicalRecord.Prescription,
                Notes = medicalRecord.Notes,
                RecordDate = medicalRecord.RecordDate ?? DateTime.Now
            };

            await PopulateDropdowns(viewModel);
            return View(viewModel);
        }

        // POST: MedicalRecord/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MedicalRecordCreateEditViewModel viewModel)
        {
            if (id != viewModel.RecordID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var medicalRecord = await _context.MedicalRecords.FindAsync(id);
                    if (medicalRecord == null)
                    {
                        return NotFound();
                    }

                    medicalRecord.PatientId = viewModel.PatientID;
                    medicalRecord.DoctorId = viewModel.DoctorID;
                    medicalRecord.AppointmentId = viewModel.AppointmentID == 0 ? null : viewModel.AppointmentID;
                    medicalRecord.Diagnosis = viewModel.Diagnosis;
                    medicalRecord.Prescription = viewModel.Prescription;
                    medicalRecord.Notes = viewModel.Notes;
                    medicalRecord.RecordDate = viewModel.RecordDate;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Medical record updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicalRecordExists(viewModel.RecordID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateDropdowns(viewModel);
            return View(viewModel);
        }

        // GET: MedicalRecord/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.Appointment)
                .Select(m => new MedicalRecordViewModel
                {
                    RecordID = m.RecordId,
                    PatientID = m.PatientId,
                    DoctorID = m.DoctorId,
                    AppointmentID = m.AppointmentId,
                    Diagnosis = m.Diagnosis,
                    Prescription = m.Prescription,
                    Notes = m.Notes,
                    RecordDate = m.RecordDate ?? DateTime.Now,
                    PatientName = m.Patient.FullName,
                    DoctorName = m.Doctor.FullName,
                    Specialty = m.Doctor.Specialty,
                    AppointmentDate = m.Appointment != null ? m.Appointment.AppointmentDate : (DateTime?)null
                })
                .FirstOrDefaultAsync(m => m.RecordID == id);

            if (medicalRecord == null)
            {
                return NotFound();
            }

            return View(medicalRecord);
        }

        // POST: MedicalRecord/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            if (medicalRecord != null)
            {
                _context.MedicalRecords.Remove(medicalRecord);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medical record deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // استخدام الـ View لعرض التاريخ الطبي للمريض
        public async Task<IActionResult> PatientHistory(int patientId)
        {
            var medicalHistory = await _context.VwPatientMedicalHistories
                .Where(v => v.PatientId == patientId)
                .Select(v => new MedicalRecordViewModel
                {
                    RecordID = v.RecordId,
                    PatientID = v.PatientId,
                    Diagnosis = v.Diagnosis,
                    Prescription = v.Prescription,
                    RecordDate = v.RecordDate ?? DateTime.Now,
                    PatientName = v.PatientName,
                    DoctorName = v.DoctorName,
                    Specialty = v.Specialty,
                    AppointmentDate = v.AppointmentDate
                })
                .ToListAsync();

            var patient = await _context.Patients
                .Where(p => p.PatientId == patientId)
                .Select(p => p.FullName)
                .FirstOrDefaultAsync();

            ViewBag.PatientName = patient;
            return View(medicalHistory);
        }

        private async Task PopulateDropdowns(MedicalRecordCreateEditViewModel viewModel)
        {
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

            viewModel.Appointments = await _context.Appointments
                .Where(a => a.Status == "Completed")
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Select(a => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = a.AppointmentId.ToString(),
                    Text = $"{a.Patient.FullName} with Dr. {a.Doctor.FullName} on {a.AppointmentDate:yyyy-MM-dd HH:mm}"
                })
                .ToListAsync();
        }

        private bool MedicalRecordExists(int id)
        {
            return _context.MedicalRecords.Any(e => e.RecordId == id);
        }
    }
}