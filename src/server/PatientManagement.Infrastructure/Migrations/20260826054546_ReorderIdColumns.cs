using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReorderIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server has no in-place column reorder, and the EF migrations generator cannot
            // drop+recreate a table under the same name within one migration (rewrite-detection
            // bug), so this is done as plain T-SQL. All tables are empty post ConvertIdsToBigint.
            migrationBuilder.Sql(@"
DROP TABLE [Medications];
DROP TABLE [Visits];
DROP TABLE [Appointments];
DROP TABLE [PasswordResetTokens];
DROP TABLE [Patients];
DROP TABLE [Users];

CREATE TABLE [Users] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [SecurityStamp] nvarchar(64) NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [LastActivityAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Patients] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [FullName] nvarchar(200) NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Gender] nvarchar(20) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id])
);

CREATE TABLE [PasswordResetTokens] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [UserId] bigint NOT NULL,
    [TokenHash] nvarchar(256) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [ConsumedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Appointments] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [PatientId] bigint NOT NULL,
    [AppointmentDate] date NOT NULL,
    [AppointmentTime] time NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Visits] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [PatientId] bigint NOT NULL,
    [AppointmentId] bigint NULL,
    [VisitDate] datetime2 NOT NULL,
    [TemperatureValue] decimal(4,1) NULL,
    [TemperatureNotRecorded] bit NOT NULL,
    [BloodPressureValue] nvarchar(20) NULL,
    [BloodPressureNotRecorded] bit NOT NULL,
    [PulseValue] int NULL,
    [PulseNotRecorded] bit NOT NULL,
    [Complaints] nvarchar(2000) NULL,
    [Diagnosis] nvarchar(2000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Visits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Visits_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Visits_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Medications] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [VisitId] bigint NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Dosage] nvarchar(100) NULL,
    [Frequency] nvarchar(100) NULL,
    [Duration] nvarchar(100) NULL,
    [Instructions] nvarchar(500) NULL,
    [SortOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Medications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Medications_Visits_VisitId] FOREIGN KEY ([VisitId]) REFERENCES [Visits] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE INDEX [IX_Patients_FullName] ON [Patients] ([FullName]);
CREATE INDEX [IX_Patients_PhoneNumber] ON [Patients] ([PhoneNumber]);

CREATE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);

CREATE INDEX [IX_Appointments_AppointmentDate] ON [Appointments] ([AppointmentDate]);
CREATE INDEX [IX_Appointments_PatientId] ON [Appointments] ([PatientId]);

CREATE INDEX [IX_Visits_AppointmentId] ON [Visits] ([AppointmentId]);
CREATE INDEX [IX_Visits_PatientId] ON [Visits] ([PatientId]);
CREATE INDEX [IX_Visits_PatientId_VisitDate] ON [Visits] ([PatientId], [VisitDate]);

CREATE INDEX [IX_Medications_VisitId] ON [Medications] ([VisitId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reordering is not meaningfully reversible; recreate tables in the
            // post-ConvertIdsToBigint column order (Id/FK columns last).
            migrationBuilder.Sql(@"
DROP TABLE [Medications];
DROP TABLE [Visits];
DROP TABLE [Appointments];
DROP TABLE [PasswordResetTokens];
DROP TABLE [Patients];
DROP TABLE [Users];

CREATE TABLE [Users] (
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [SecurityStamp] nvarchar(64) NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [LastActivityAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [Id] bigint NOT NULL IDENTITY(1,1),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Patients] (
    [FullName] nvarchar(200) NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Gender] nvarchar(20) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [Id] bigint NOT NULL IDENTITY(1,1),
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id])
);

CREATE TABLE [PasswordResetTokens] (
    [TokenHash] nvarchar(256) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [ConsumedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Id] bigint NOT NULL IDENTITY(1,1),
    [UserId] bigint NOT NULL,
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Appointments] (
    [AppointmentDate] date NOT NULL,
    [AppointmentTime] time NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [Id] bigint NOT NULL IDENTITY(1,1),
    [PatientId] bigint NOT NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Visits] (
    [VisitDate] datetime2 NOT NULL,
    [TemperatureValue] decimal(4,1) NULL,
    [TemperatureNotRecorded] bit NOT NULL,
    [BloodPressureValue] nvarchar(20) NULL,
    [BloodPressureNotRecorded] bit NOT NULL,
    [PulseValue] int NULL,
    [PulseNotRecorded] bit NOT NULL,
    [Complaints] nvarchar(2000) NULL,
    [Diagnosis] nvarchar(2000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [Id] bigint NOT NULL IDENTITY(1,1),
    [PatientId] bigint NOT NULL,
    [AppointmentId] bigint NULL,
    CONSTRAINT [PK_Visits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Visits_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Visits_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Medications] (
    [Name] nvarchar(200) NOT NULL,
    [Dosage] nvarchar(100) NULL,
    [Frequency] nvarchar(100) NULL,
    [Duration] nvarchar(100) NULL,
    [Instructions] nvarchar(500) NULL,
    [SortOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [Id] bigint NOT NULL IDENTITY(1,1),
    [VisitId] bigint NOT NULL,
    CONSTRAINT [PK_Medications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Medications_Visits_VisitId] FOREIGN KEY ([VisitId]) REFERENCES [Visits] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE INDEX [IX_Patients_FullName] ON [Patients] ([FullName]);
CREATE INDEX [IX_Patients_PhoneNumber] ON [Patients] ([PhoneNumber]);

CREATE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);

CREATE INDEX [IX_Appointments_AppointmentDate] ON [Appointments] ([AppointmentDate]);
CREATE INDEX [IX_Appointments_PatientId] ON [Appointments] ([PatientId]);

CREATE INDEX [IX_Visits_AppointmentId] ON [Visits] ([AppointmentId]);
CREATE INDEX [IX_Visits_PatientId] ON [Visits] ([PatientId]);
CREATE INDEX [IX_Visits_PatientId_VisitDate] ON [Visits] ([PatientId], [VisitDate]);

CREATE INDEX [IX_Medications_VisitId] ON [Medications] ([VisitId]);
");
        }
    }
}
