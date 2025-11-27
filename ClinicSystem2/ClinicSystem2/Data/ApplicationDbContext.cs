using System;
using System.Collections.Generic;
using ClinicSystem2.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem2.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentLog> AppointmentLogs { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<VwAppointmentDetail> VwAppointmentDetails { get; set; }

    public virtual DbSet<VwDoctorAvailableSlot> VwDoctorAvailableSlots { get; set; }

    public virtual DbSet<VwDoctorScheduleToday> VwDoctorScheduleTodays { get; set; }

    public virtual DbSet<VwMonthlyRevenue> VwMonthlyRevenues { get; set; }

    public virtual DbSet<VwPatientMedicalHistory> VwPatientMedicalHistories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=ClinicSystem;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCA260EC7330");

            entity.ToTable("Appointment", tb =>
                {
                    tb.HasTrigger("trg_ArchiveCompletedAppointments");
                    tb.HasTrigger("trg_LogNewAppointment");
                    tb.HasTrigger("trg_PreventDoubleBooking");
                });

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__Docto__4316F928");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__Patie__4222D4EF");
        });

        modelBuilder.Entity<AppointmentLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Appointm__5E5499A81B43718C");

            entity.ToTable("AppointmentLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.LogDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LogMessage).HasMaxLength(255);
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK__Doctor__2DC00EDF93CBB096");

            entity.ToTable("Doctor");

            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.ConsultationFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Specialty).HasMaxLength(100);
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__MedicalR__FBDF78C95725EA32");

            entity.ToTable("MedicalRecord");

            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.Diagnosis).HasMaxLength(500);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Prescription).HasMaxLength(500);
            entity.Property(e => e.RecordDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__MedicalRe__Appoi__4F7CD00D");

            entity.HasOne(d => d.Doctor).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MedicalRe__Docto__4E88ABD4");

            entity.HasOne(d => d.Patient).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MedicalRe__Patie__4D94879B");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patient__970EC3464E871CB9");

            entity.ToTable("Patient", tb => tb.HasTrigger("trg_ValidatePatientAge"));

            entity.HasIndex(e => e.Email, "UQ__Patient__A9D105346374ABB8").IsUnique();

            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A58FB1C1217");

            entity.ToTable("Payment", tb => tb.HasTrigger("trg_UpdateAppointmentStatus_AfterPayment"));

            entity.HasIndex(e => e.AppointmentId, "UQ__Payment__8ECDFCA3924680AD").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.Method).HasMaxLength(50);
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.TransactionReference).HasMaxLength(100);

            entity.HasOne(d => d.Appointment).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__Appoint__49C3F6B7");
        });

        modelBuilder.Entity<VwAppointmentDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AppointmentDetails");

            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.AppointmentStatus).HasMaxLength(20);
            entity.Property(e => e.DoctorName).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.PatientName).HasMaxLength(100);
            entity.Property(e => e.PatientPhone).HasMaxLength(20);
            entity.Property(e => e.PaymentAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);
            entity.Property(e => e.Specialty).HasMaxLength(100);
        });

        modelBuilder.Entity<VwDoctorAvailableSlot>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DoctorAvailableSlots");

            entity.Property(e => e.DoctorId)
                .ValueGeneratedOnAdd()
                .HasColumnName("DoctorID");
            entity.Property(e => e.DoctorName).HasMaxLength(100);
            entity.Property(e => e.SlotEndTime).HasColumnType("datetime");
            entity.Property(e => e.SlotStartTime).HasColumnType("datetime");
            entity.Property(e => e.Specialty).HasMaxLength(100);
        });

        modelBuilder.Entity<VwDoctorScheduleToday>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DoctorScheduleToday");

            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.DoctorName).HasMaxLength(100);
            entity.Property(e => e.PatientName).HasMaxLength(100);
            entity.Property(e => e.Specialty).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<VwMonthlyRevenue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MonthlyRevenue");

            entity.Property(e => e.AveragePayment).HasColumnType("decimal(38, 6)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwPatientMedicalHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PatientMedicalHistory");

            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.Diagnosis).HasMaxLength(500);
            entity.Property(e => e.DoctorName).HasMaxLength(100);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.PatientName).HasMaxLength(100);
            entity.Property(e => e.Prescription).HasMaxLength(500);
            entity.Property(e => e.RecordDate).HasColumnType("datetime");
            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.Specialty).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
