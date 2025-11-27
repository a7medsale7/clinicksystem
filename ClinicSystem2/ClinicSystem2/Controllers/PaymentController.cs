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
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payment
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Select(p => new PaymentViewModel
                {
                    PaymentID = p.PaymentId,
                    AppointmentID = p.AppointmentId,
                    PaymentDate = p.PaymentDate ?? DateTime.Now,
                    Amount = p.Amount ?? 0,
                    Method = p.Method,
                    Status = p.Status,
                    TransactionReference = p.TransactionReference,
                    PatientName = p.Appointment.Patient.FullName,
                    DoctorName = p.Appointment.Doctor.FullName
                })
                .ToListAsync();

            return View(payments);
        }

        // GET: Payment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Select(p => new PaymentViewModel
                {
                    PaymentID = p.PaymentId,
                    AppointmentID = p.AppointmentId,
                    PaymentDate = p.PaymentDate ?? DateTime.Now,
                    Amount = p.Amount ?? 0,
                    Method = p.Method,
                    Status = p.Status,
                    TransactionReference = p.TransactionReference,
                    PatientName = p.Appointment.Patient.FullName,
                    DoctorName = p.Appointment.Doctor.FullName
                })
                .FirstOrDefaultAsync(m => m.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payment/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new PaymentCreateEditViewModel
            {
                Appointments = await _context.Appointments
                    .Where(a => a.Status != "Cancelled" && a.Payment == null)
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Select(a => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = a.AppointmentId.ToString(),
                        Text = $"{a.Patient.FullName} with Dr. {a.Doctor.FullName} on {a.AppointmentDate:yyyy-MM-dd HH:mm}"
                    })
                    .ToListAsync(),
                PaymentDate = DateTime.Now
            };

            return View(viewModel);
        }

        // POST: Payment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentCreateEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var payment = new Payment
                {
                    AppointmentId = viewModel.AppointmentID,
                    PaymentDate = viewModel.PaymentDate,
                    Amount = viewModel.Amount,
                    Method = viewModel.Method,
                    Status = viewModel.Status,
                    TransactionReference = viewModel.TransactionReference
                };

                _context.Add(payment);

                try
                {
                    // الـ Trigger سيقوم بتحديث حالة الموعد تلقائياً بعد الدفع
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Payment created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating payment: {ex.Message}");
                }
            }

            viewModel.Appointments = await _context.Appointments
                .Where(a => a.Status != "Cancelled" && a.Payment == null)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Select(a => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = a.AppointmentId.ToString(),
                    Text = $"{a.Patient.FullName} with Dr. {a.Doctor.FullName} on {a.AppointmentDate:yyyy-MM-dd HH:mm}"
                })
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Payment/Edit/5
        // GET: Payment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            var viewModel = new PaymentCreateEditViewModel
            {
                PaymentID = payment.PaymentId,
                AppointmentID = payment.AppointmentId,
                PaymentDate = payment.PaymentDate ?? DateTime.Now,
                Amount = payment.Amount ?? 0,
                Method = payment.Method,
                Status = payment.Status,
                TransactionReference = payment.TransactionReference,
                Appointments = await _context.Appointments
                    .Where(a => a.Status != "Cancelled")
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Select(a => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = a.AppointmentId.ToString(),
                        Text = $"{a.Patient.FullName} with Dr. {a.Doctor.FullName} on {a.AppointmentDate:yyyy-MM-dd HH:mm}"
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Payment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentCreateEditViewModel viewModel)
        {
            if (id != viewModel.PaymentID)
            {
                return NotFound();
            }

            // التحقق من القيم المسموح بها للـ Status
            var allowedStatuses = new[] { "Paid", "Failed", "Refunded" };
            if (!allowedStatuses.Contains(viewModel.Status))
            {
                ModelState.AddModelError("Status", $"Status must be one of: {string.Join(", ", allowedStatuses)}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var payment = await _context.Payments.FindAsync(id);
                    if (payment == null)
                    {
                        return NotFound();
                    }

                    payment.AppointmentId = viewModel.AppointmentID;
                    payment.PaymentDate = viewModel.PaymentDate;
                    payment.Amount = viewModel.Amount;
                    payment.Method = viewModel.Method;
                    payment.Status = viewModel.Status;
                    payment.TransactionReference = viewModel.TransactionReference;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Payment updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Message.Contains("CHECK constraint"))
                {
                    ModelState.AddModelError("", "Invalid status value. Allowed values are: Paid, Failed, Refunded");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(viewModel.PaymentID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // إعادة تعبئة القوائم المنسدلة
            viewModel.Appointments = await _context.Appointments
                .Where(a => a.Status != "Cancelled")
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Select(a => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = a.AppointmentId.ToString(),
                    Text = $"{a.Patient.FullName} with Dr. {a.Doctor.FullName} on {a.AppointmentDate:yyyy-MM-dd HH:mm}"
                })
                .ToListAsync();

            return View(viewModel);
        }
        // GET: Payment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Select(p => new PaymentViewModel
                {
                    PaymentID = p.PaymentId,
                    AppointmentID = p.AppointmentId,
                    PaymentDate = p.PaymentDate ?? DateTime.Now,
                    Amount = p.Amount ?? 0,
                    Method = p.Method,
                    Status = p.Status,
                    TransactionReference = p.TransactionReference,
                    PatientName = p.Appointment.Patient.FullName,
                    DoctorName = p.Appointment.Doctor.FullName
                })
                .FirstOrDefaultAsync(m => m.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payment deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }
    }
}