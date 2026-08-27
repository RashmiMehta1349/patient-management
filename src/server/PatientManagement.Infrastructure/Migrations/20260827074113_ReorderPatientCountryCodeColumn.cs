using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReorderPatientCountryCodeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server has no in-place column reorder (see ReorderIdColumns). CountryCode was
            // appended at the end by the preceding AddColumn migration; this moves it to just
            // before PhoneNumber via rebuild-and-copy, preserving existing Patients rows and the
            // Appointments/Visits foreign keys that reference this table.
            migrationBuilder.Sql(@"
ALTER TABLE [Appointments] DROP CONSTRAINT [FK_Appointments_Patients_PatientId];
ALTER TABLE [Visits] DROP CONSTRAINT [FK_Visits_Patients_PatientId];

CREATE TABLE [Patients_Reordered] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [FullName] nvarchar(200) NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Gender] nvarchar(20) NOT NULL,
    [CountryCode] nvarchar(5) NOT NULL,
    [PhoneNumber] nvarchar(10) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Patients_Reordered] PRIMARY KEY ([Id])
);

SET IDENTITY_INSERT [Patients_Reordered] ON;
INSERT INTO [Patients_Reordered] ([Id], [FullName], [DateOfBirth], [Gender], [CountryCode], [PhoneNumber], [CreatedAt], [UpdatedAt])
SELECT [Id], [FullName], [DateOfBirth], [Gender], [CountryCode], [PhoneNumber], [CreatedAt], [UpdatedAt]
FROM [Patients];
SET IDENTITY_INSERT [Patients_Reordered] OFF;

DROP TABLE [Patients];
EXEC sp_rename 'Patients_Reordered', 'Patients';
EXEC sp_rename 'PK_Patients_Reordered', 'PK_Patients';

CREATE INDEX [IX_Patients_FullName] ON [Patients] ([FullName]);
CREATE INDEX [IX_Patients_PhoneNumber] ON [Patients] ([PhoneNumber]);

ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION;
ALTER TABLE [Visits] ADD CONSTRAINT [FK_Visits_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reordering is not meaningfully reversible; recreate with CountryCode last again,
            // matching the column order this migration's Up() started from.
            migrationBuilder.Sql(@"
ALTER TABLE [Appointments] DROP CONSTRAINT [FK_Appointments_Patients_PatientId];
ALTER TABLE [Visits] DROP CONSTRAINT [FK_Visits_Patients_PatientId];

CREATE TABLE [Patients_Reordered] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [FullName] nvarchar(200) NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Gender] nvarchar(20) NOT NULL,
    [PhoneNumber] nvarchar(10) NOT NULL,
    [CountryCode] nvarchar(5) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Patients_Reordered] PRIMARY KEY ([Id])
);

SET IDENTITY_INSERT [Patients_Reordered] ON;
INSERT INTO [Patients_Reordered] ([Id], [FullName], [DateOfBirth], [Gender], [PhoneNumber], [CountryCode], [CreatedAt], [UpdatedAt])
SELECT [Id], [FullName], [DateOfBirth], [Gender], [PhoneNumber], [CountryCode], [CreatedAt], [UpdatedAt]
FROM [Patients];
SET IDENTITY_INSERT [Patients_Reordered] OFF;

DROP TABLE [Patients];
EXEC sp_rename 'Patients_Reordered', 'Patients';
EXEC sp_rename 'PK_Patients_Reordered', 'PK_Patients';

CREATE INDEX [IX_Patients_FullName] ON [Patients] ([FullName]);
CREATE INDEX [IX_Patients_PhoneNumber] ON [Patients] ([PhoneNumber]);

ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION;
ALTER TABLE [Visits] ADD CONSTRAINT [FK_Visits_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION;
");
        }
    }
}
