CREATE DATABASE ClinicSystem;
GO

USE ClinicSystem;
GO

------------------------------------------------------------
--                       CREATE TABLES
------------------------------------------------------------

CREATE TABLE Patient (
    PatientID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE,
    Phone NVARCHAR(20),
    BirthDate DATE,
    Gender NVARCHAR(10) CHECK (Gender IN ('Male', 'Female')),
    Address NVARCHAR(255),
    CreatedDate DATETIME DEFAULT GETDATE()
);

CREATE TABLE Doctor (
    DoctorID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Specialty NVARCHAR(100),
    Description NVARCHAR(255),
    ConsultationFee DECIMAL(10,2) DEFAULT 0,
    IsActive BIT DEFAULT 1
);

CREATE TABLE Appointment (
    AppointmentID INT IDENTITY(1,1) PRIMARY KEY,
    PatientID INT NOT NULL,
    DoctorID INT NOT NULL,
    AppointmentDate DATETIME NOT NULL,
    Status NVARCHAR(20) CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')),
    Notes NVARCHAR(255),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PatientID) REFERENCES Patient(PatientID),
    FOREIGN KEY (DoctorID) REFERENCES Doctor(DoctorID)
);

CREATE TABLE Payment (
    PaymentID INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentID INT NOT NULL UNIQUE,
    PaymentDate DATETIME DEFAULT GETDATE(),
    Amount DECIMAL(10,2) CHECK (Amount >= 0),
    Method NVARCHAR(50),
    Status NVARCHAR(20) CHECK (Status IN ('Paid', 'Failed', 'Refunded')),
    TransactionReference NVARCHAR(100),
    FOREIGN KEY (AppointmentID) REFERENCES Appointment(AppointmentID)
);

CREATE TABLE MedicalRecord (
    RecordID INT IDENTITY(1,1) PRIMARY KEY,
    PatientID INT NOT NULL,
    DoctorID INT NOT NULL,
    AppointmentID INT,
    Diagnosis NVARCHAR(500),
    Prescription NVARCHAR(500),
    Notes NVARCHAR(1000),
    RecordDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PatientID) REFERENCES Patient(PatientID),
    FOREIGN KEY (DoctorID) REFERENCES Doctor(DoctorID),
    FOREIGN KEY (AppointmentID) REFERENCES Appointment(AppointmentID)
);

-- Create AppointmentLog table before triggers that reference it
CREATE TABLE AppointmentLog (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentID INT,
    LogMessage NVARCHAR(255),
    LogDate DATETIME DEFAULT GETDATE()
);

------------------------------------------------------------
--                   INSERT SAMPLE DATA
------------------------------------------------------------

INSERT INTO Patient (FullName, Email, Phone, BirthDate, Gender, Address)
VALUES
(N'Ahmed Ali', 'ahmed.ali@gmail.com', '01012345678', '1985-03-15', 'Male', N'123 Main St, Cairo'),
(N'Sara Mohamed', 'sara.mohamed@yahoo.com', '01098765432', '1990-07-22', 'Female', N'456 Garden City, Giza'),
(N'Omar Hassan', 'omar.hassan@gmail.com', '01122334455', '1988-11-30', 'Male', N'789 Heliopolis, Cairo');

INSERT INTO Doctor (FullName, Specialty, Description, ConsultationFee)
VALUES
(N'Dr. Mohamed Hanafy', N'Cardiology', N'15 years of experience', 300.00),
(N'Dr. Samir Fathy', N'Dentistry', N'Specialist in dental surgery', 200.00),
(N'Dr. Asmaa Adel', N'Pediatrics', N'Children health expert', 250.00);

INSERT INTO Appointment (PatientID, DoctorID, AppointmentDate, Status, Notes)
VALUES
(1, 1, '2025-11-10 10:30:00', 'Confirmed', N'Regular checkup'),
(2, 2, '2025-11-10 12:00:00', 'Pending', N'Tooth pain'),
(3, 3, '2025-11-11 09:00:00', 'Confirmed', N'Child fever problem');

INSERT INTO Payment (AppointmentID, PaymentDate, Amount, Method, Status, TransactionReference)
VALUES
(1, GETDATE(), 300.00, N'Credit Card', N'Paid', 'TXN001'),
(2, GETDATE(), 150.00, N'Cash', N'Failed', 'TXN002'),
(3, GETDATE(), 400.00, N'Online', N'Paid', 'TXN003');

INSERT INTO MedicalRecord (PatientID, DoctorID, AppointmentID, Diagnosis, Prescription, Notes)
VALUES
(1, 1, 1, N'Hypertension - Stage 1', N'Lisinopril 10mg once daily', N'Patient advised to reduce salt intake and exercise regularly'),
(2, 2, 2, N'Dental Caries - Molar', N'Amoxicillin 500mg TDS for 5 days', N'Root canal treatment scheduled for next week');

------------------------------------------------------------
--                      TEST SELECTS
------------------------------------------------------------

SELECT * FROM Patient;
SELECT * FROM Doctor;
SELECT * FROM Appointment;
SELECT * FROM Payment;
SELECT * FROM MedicalRecord;

------------------------------------------------------------
--                         VIEWS
------------------------------------------------------------

-- 1. View: Appointment Complete Details  --100/100
CREATE OR ALTER VIEW vw_AppointmentDetails AS
SELECT 
    A.AppointmentID,
    P.FullName AS PatientName,
    P.Phone AS PatientPhone,
    D.FullName AS DoctorName,
    D.Specialty,
    A.AppointmentDate,
    A.Status AS AppointmentStatus,
    P2.Amount AS PaymentAmount,
    P2.Method AS PaymentMethod,
    P2.Status AS PaymentStatus,
    P2.PaymentDate,
    A.Notes
FROM Appointment A
JOIN Patient P ON A.PatientID = P.PatientID
JOIN Doctor D ON A.DoctorID = D.DoctorID
LEFT JOIN Payment P2 ON A.AppointmentID = P2.AppointmentID;
GO

-- 2. View: Doctor Schedule for Today
CREATE OR ALTER VIEW vw_DoctorScheduleToday AS
SELECT 
    D.DoctorID,
    D.FullName AS DoctorName,
    D.Specialty,
    A.AppointmentID,
    P.FullName AS PatientName,
    A.AppointmentDate,
    A.Status
FROM Doctor D
JOIN Appointment A ON D.DoctorID = A.DoctorID
JOIN Patient P ON A.PatientID = P.PatientID
WHERE CAST(A.AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)
AND A.Status IN ('Pending', 'Confirmed');
GO

-- 3. View: Patient Medical History
CREATE OR ALTER VIEW vw_PatientMedicalHistory AS
SELECT 
    P.PatientID,
    P.FullName AS PatientName,
    MR.RecordID,
    D.FullName AS DoctorName,
    D.Specialty,
    MR.Diagnosis,
    MR.Prescription,
    MR.RecordDate,
    A.AppointmentDate
FROM Patient P
JOIN MedicalRecord MR ON P.PatientID = MR.PatientID
JOIN Doctor D ON MR.DoctorID = D.DoctorID
LEFT JOIN Appointment A ON MR.AppointmentID = A.AppointmentID;
GO

-- 4. View: Monthly Revenue Report
CREATE OR ALTER VIEW vw_MonthlyRevenue AS
SELECT 
    YEAR(PaymentDate) AS Year,
    MONTH(PaymentDate) AS Month,
    COUNT(*) AS TotalTransactions,
    SUM(Amount) AS TotalRevenue,
    AVG(Amount) AS AveragePayment,
    Method AS PaymentMethod
FROM Payment
WHERE Status = 'Paid'
GROUP BY YEAR(PaymentDate), MONTH(PaymentDate), Method;
GO

-- 5. View: Available Time Slots for Doctors
CREATE OR ALTER VIEW vw_DoctorAvailableSlots AS
SELECT 
    D.DoctorID,
    D.FullName AS DoctorName,
    D.Specialty,
    DATEADD(HOUR, 9, CAST(CAST(GETDATE() AS DATE) AS DATETIME)) AS SlotStartTime,
    DATEADD(HOUR, 17, CAST(CAST(GETDATE() AS DATE) AS DATETIME)) AS SlotEndTime
FROM Doctor D
WHERE D.IsActive = 1;
GO

------------------------------------------------------------
--                     FUNCTIONS
------------------------------------------------------------

-- 1. Function: Calculate Patient Age
CREATE OR ALTER FUNCTION fn_CalculateAge(@BirthDate DATE)
RETURNS INT
AS
BEGIN
    RETURN DATEDIFF(YEAR, @BirthDate, GETDATE()) - 
           CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, @BirthDate, GETDATE()), @BirthDate) > GETDATE() 
                THEN 1 ELSE 0 END
END;
GO

-- 2. Function: Get Doctor Appointment Count
CREATE OR ALTER FUNCTION fn_GetDoctorAppointmentCount(@DoctorID INT, @StartDate DATE, @EndDate DATE)
RETURNS INT
AS
BEGIN
    DECLARE @Count INT;
    SELECT @Count = COUNT(*) 
    FROM Appointment 
    WHERE DoctorID = @DoctorID 
    AND CAST(AppointmentDate AS DATE) BETWEEN @StartDate AND @EndDate
    AND Status <> 'Cancelled';
    RETURN @Count;
END;
GO

-- 3. Function: Check If Time Slot Available
CREATE OR ALTER FUNCTION fn_IsTimeSlotAvailable(@DoctorID INT, @ProposedDateTime DATETIME)
RETURNS BIT
AS
BEGIN
    DECLARE @IsAvailable BIT = 1;
    
    IF EXISTS (
        SELECT 1 FROM Appointment 
        WHERE DoctorID = @DoctorID 
        AND AppointmentDate = @ProposedDateTime
        AND Status <> 'Cancelled'
    )
    BEGIN
        SET @IsAvailable = 0;
    END
    
    RETURN @IsAvailable;
END;
GO

-- 4. Function: Get Total Revenue by Doctor
CREATE OR ALTER FUNCTION fn_GetDoctorRevenue(@DoctorID INT, @StartDate DATE, @EndDate DATE)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @TotalRevenue DECIMAL(10,2);
    
    SELECT @TotalRevenue = ISNULL(SUM(P.Amount), 0)
    FROM Payment P
    JOIN Appointment A ON P.AppointmentID = A.AppointmentID
    WHERE A.DoctorID = @DoctorID
    AND P.Status = 'Paid'
    AND CAST(P.PaymentDate AS DATE) BETWEEN @StartDate AND @EndDate;
    
    RETURN @TotalRevenue;
END;
GO

-- 5. Function: Get Patient Next Appointment
CREATE OR ALTER FUNCTION fn_GetPatientNextAppointment(@PatientID INT)
RETURNS DATETIME
AS
BEGIN
    DECLARE @NextAppointment DATETIME;
    
    SELECT TOP 1 @NextAppointment = AppointmentDate
    FROM Appointment
    WHERE PatientID = @PatientID
    AND AppointmentDate > GETDATE()
    AND Status IN ('Pending', 'Confirmed')
    ORDER BY AppointmentDate ASC;
    
    RETURN @NextAppointment;
END;
GO

------------------------------------------------------------
--                 STORED PROCEDURES
------------------------------------------------------------

-- 1. Procedure: Add Appointment Safely with Validation
-- تعديل sp_AddAppointment
CREATE OR ALTER PROCEDURE sp_AddAppointment
    @PatientID INT,
    @DoctorID INT,
    @AppointmentDate DATETIME,
    @Notes NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Patient WHERE PatientID = @PatientID)
    BEGIN
        THROW 51000, 'Patient does not exist.', 1;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Doctor WHERE DoctorID = @DoctorID)
    BEGIN
        THROW 51000, 'Doctor does not exist.', 1;
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM Appointment 
        WHERE DoctorID = @DoctorID 
          AND AppointmentDate = @AppointmentDate
          AND Status <> 'Cancelled'
    )
    BEGIN
        THROW 51000, 'Doctor already has appointment at this time.', 1;
        RETURN;
    END

    INSERT INTO Appointment (PatientID, DoctorID, AppointmentDate, Status, Notes)
    VALUES (@PatientID, @DoctorID, @AppointmentDate, 'Pending', @Notes);

    SELECT 1 AS Success;
END;
GO
-- 2. Procedure: Search Appointments by Doctor & Date
CREATE OR ALTER PROCEDURE sp_FindAppointmentForDoctor
    @DoctorName NVARCHAR(100),
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        A.AppointmentID AS AppointmentID,
        A.PatientID AS PatientID,        -- أضف PatientID
        A.DoctorID AS DoctorID,          -- أضف DoctorID
        P.FullName AS PatientName,
        ISNULL(P.Phone, '') AS PatientPhone,
        D.FullName AS DoctorName,
        ISNULL(D.Specialty, 'General') AS Specialty,
        A.AppointmentDate,
        ISNULL(A.Status, 'Pending') AS Status,
        ISNULL(A.Notes, '') AS Notes,
        ISNULL(A.CreatedDate, GETDATE()) AS CreatedDate
    FROM Appointment A
    JOIN Patient P ON A.PatientID = P.PatientID
    JOIN Doctor D ON A.DoctorID = D.DoctorID
    WHERE D.FullName LIKE '%' + @DoctorName + '%'
      AND CAST(A.AppointmentDate AS DATE) = @Date;
END;
GO

-- 3. Procedure: Complete Appointment with Medical Record
CREATE OR ALTER PROCEDURE sp_CompleteAppointment
    @AppointmentID INT,
    @Diagnosis NVARCHAR(500),
    @Prescription NVARCHAR(500),
    @Notes NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Update appointment status
        UPDATE Appointment 
        SET Status = 'Completed'
        WHERE AppointmentID = @AppointmentID;
        
        -- Create medical record
        INSERT INTO MedicalRecord (PatientID, DoctorID, AppointmentID, Diagnosis, Prescription, Notes)
        SELECT PatientID, DoctorID, @AppointmentID, @Diagnosis, @Prescription, @Notes
        FROM Appointment 
        WHERE AppointmentID = @AppointmentID;
        
        COMMIT TRANSACTION;
        PRINT 'Appointment completed and medical record created successfully.';
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('Error completing appointment: %s', 16, 1, @ErrorMessage);
    END CATCH
END;
GO

-- 4. Procedure: Generate Financial Report
CREATE OR ALTER PROCEDURE sp_GenerateFinancialReport
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        D.DoctorID,
        D.FullName AS DoctorName,
        D.Specialty,
        COUNT(A.AppointmentID) AS TotalAppointments,
        COUNT(P.PaymentID) AS PaidAppointments,
        ISNULL(SUM(P.Amount), 0) AS TotalRevenue,
        ISNULL(AVG(P.Amount), 0) AS AverageFee
    FROM Doctor D
    LEFT JOIN Appointment A ON D.DoctorID = A.DoctorID 
        AND CAST(A.AppointmentDate AS DATE) BETWEEN @StartDate AND @EndDate
    LEFT JOIN Payment P ON A.AppointmentID = P.AppointmentID 
        AND P.Status = 'Paid'
    GROUP BY D.DoctorID, D.FullName, D.Specialty
    ORDER BY TotalRevenue DESC;
END;
GO

-- 5. Procedure: Register New Patient
CREATE OR ALTER PROCEDURE sp_RegisterPatient
    @FullName NVARCHAR(100),
    @Email NVARCHAR(100),
    @Phone NVARCHAR(20),
    @BirthDate DATE,
    @Gender NVARCHAR(10),
    @Address NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM Patient WHERE Email = @Email)
    BEGIN
        RAISERROR('Patient with this email already exists.', 16, 1);
        RETURN;
    END
    
    INSERT INTO Patient (FullName, Email, Phone, BirthDate, Gender, Address)
    VALUES (@FullName, @Email, @Phone, @BirthDate, @Gender, @Address);
    
    SELECT SCOPE_IDENTITY() AS NewPatientID;
    PRINT 'Patient registered successfully.';
END;
GO

------------------------------------------------------------
--                       TRIGGERS
------------------------------------------------------------

-- 1. Trigger: Prevent Doctor Double Booking After Insert
CREATE OR ALTER TRIGGER trg_PreventDoubleBooking
ON Appointment
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM Appointment A
        JOIN inserted I ON A.DoctorID = I.DoctorID
                       AND A.AppointmentDate = I.AppointmentDate
                       AND A.AppointmentID <> I.AppointmentID
                       AND A.Status <> 'Cancelled'
    )
    BEGIN
        RAISERROR('Doctor already has an appointment at this time.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 2. Trigger: Auto-Confirm Appointment After Payment
CREATE OR ALTER TRIGGER trg_UpdateAppointmentStatus_AfterPayment
ON Payment
AFTER INSERT
AS
BEGIN
    UPDATE A
    SET A.Status = 
        CASE I.Status 
            WHEN 'Paid' THEN 'Confirmed'
            ELSE A.Status
        END
    FROM Appointment A
    JOIN inserted I ON A.AppointmentID = I.AppointmentID;
END;
GO

-- 3. Trigger: Log New Appointments
CREATE OR ALTER TRIGGER trg_LogNewAppointment
ON Appointment
AFTER INSERT
AS
BEGIN
    INSERT INTO AppointmentLog (AppointmentID, LogMessage)
    SELECT 
        I.AppointmentID,
        CONCAT('New appointment created for PatientID = ', I.PatientID, ', DoctorID = ', I.DoctorID)
    FROM inserted I;
END;
GO

-- 4. Trigger: Validate Patient Age
CREATE OR ALTER TRIGGER trg_ValidatePatientAge
ON Patient
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (
        SELECT 1 FROM inserted 
        WHERE BirthDate > DATEADD(YEAR, -1, GETDATE()) OR BirthDate < DATEADD(YEAR, -120, GETDATE())
    )
    BEGIN
        RAISERROR('Invalid birth date. Patient age must be between 1 and 120 years.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 5. Trigger: Archive Completed Appointments
CREATE OR ALTER TRIGGER trg_ArchiveCompletedAppointments
ON Appointment
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM inserted WHERE Status = 'Completed')
    AND EXISTS (SELECT 1 FROM deleted WHERE Status <> 'Completed')
    BEGIN
        PRINT 'Appointment marked as completed. Consider archiving to historical table.';
        -- Here you can add logic to move completed appointments to archive table
    END
END;
GO

------------------------------------------------------------
--                   USAGE EXAMPLES
------------------------------------------------------------

-- Test the new functions
SELECT 
    FullName, 
    dbo.fn_CalculateAge(BirthDate) AS Age 
FROM Patient;

SELECT dbo.fn_GetDoctorAppointmentCount(1, '2025-01-01', '2025-12-31') AS AppointmentCount;

SELECT dbo.fn_IsTimeSlotAvailable(1, '2025-11-10 10:30:00') AS IsSlotAvailable;

-- Test the new views
SELECT * FROM vw_DoctorScheduleToday;
SELECT * FROM vw_PatientMedicalHistory;
SELECT * FROM vw_MonthlyRevenue;

-- Test the new stored procedures
EXEC sp_CompleteAppointment 1, 'Hypertension controlled', 'Continue current medication', 'Follow up in 3 months';
EXEC sp_GenerateFinancialReport '2025-01-01', '2025-12-31';
EXEC sp_RegisterPatient 'Nour Ahmed', 'nour.ahmed@email.com', '01055556666', '1992-05-15', 'Female', '123 New Cairo';

PRINT 'ClinicSystem database setup completed successfully!';