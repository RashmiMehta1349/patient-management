using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertIdsToBigint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FKs (children first) so PKs/columns can be dropped and recreated.
            migrationBuilder.DropForeignKey(name: "FK_Medications_Visits_VisitId", table: "Medications");
            migrationBuilder.DropForeignKey(name: "FK_Visits_Appointments_AppointmentId", table: "Visits");
            migrationBuilder.DropForeignKey(name: "FK_Visits_Patients_PatientId", table: "Visits");
            migrationBuilder.DropForeignKey(name: "FK_Appointments_Patients_PatientId", table: "Appointments");
            migrationBuilder.DropForeignKey(name: "FK_PasswordResetTokens_Users_UserId", table: "PasswordResetTokens");

            // Existing uniqueidentifier data cannot be converted to bigint identities; clear rows
            // (children first) so the new NOT NULL bigint columns can be added.
            migrationBuilder.Sql("DELETE FROM [Medications];");
            migrationBuilder.Sql("DELETE FROM [Visits];");
            migrationBuilder.Sql("DELETE FROM [Appointments];");
            migrationBuilder.Sql("DELETE FROM [PasswordResetTokens];");
            migrationBuilder.Sql("DELETE FROM [Patients];");
            migrationBuilder.Sql("DELETE FROM [Users];");

            // Drop PKs (children first) so the identity Id columns can be dropped/recreated.
            migrationBuilder.DropPrimaryKey(name: "PK_Medications", table: "Medications");
            migrationBuilder.DropPrimaryKey(name: "PK_Visits", table: "Visits");
            migrationBuilder.DropPrimaryKey(name: "PK_Appointments", table: "Appointments");
            migrationBuilder.DropPrimaryKey(name: "PK_PasswordResetTokens", table: "PasswordResetTokens");
            migrationBuilder.DropPrimaryKey(name: "PK_Patients", table: "Patients");
            migrationBuilder.DropPrimaryKey(name: "PK_Users", table: "Users");

            // Drop indexes on FK columns so those columns can be dropped/recreated.
            migrationBuilder.DropIndex(name: "IX_PasswordResetTokens_UserId", table: "PasswordResetTokens");
            migrationBuilder.DropIndex(name: "IX_Appointments_PatientId", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Visits_PatientId", table: "Visits");
            migrationBuilder.DropIndex(name: "IX_Visits_PatientId_VisitDate", table: "Visits");
            migrationBuilder.DropIndex(name: "IX_Visits_AppointmentId", table: "Visits");
            migrationBuilder.DropIndex(name: "IX_Medications_VisitId", table: "Medications");

            // Drop and recreate Id/FK columns as bigint (existing uniqueidentifier data cannot be preserved).
            migrationBuilder.DropColumn(name: "Id", table: "Medications");
            migrationBuilder.DropColumn(name: "VisitId", table: "Medications");
            migrationBuilder.AddColumn<long>(name: "Id", table: "Medications", type: "bigint", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");
            migrationBuilder.AddColumn<long>(name: "VisitId", table: "Medications", type: "bigint", nullable: false);

            migrationBuilder.DropColumn(name: "Id", table: "Visits");
            migrationBuilder.DropColumn(name: "PatientId", table: "Visits");
            migrationBuilder.DropColumn(name: "AppointmentId", table: "Visits");
            migrationBuilder.AddColumn<long>(name: "Id", table: "Visits", type: "bigint", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");
            migrationBuilder.AddColumn<long>(name: "PatientId", table: "Visits", type: "bigint", nullable: false);
            migrationBuilder.AddColumn<long>(name: "AppointmentId", table: "Visits", type: "bigint", nullable: true);

            migrationBuilder.DropColumn(name: "Id", table: "Appointments");
            migrationBuilder.DropColumn(name: "PatientId", table: "Appointments");
            migrationBuilder.AddColumn<long>(name: "Id", table: "Appointments", type: "bigint", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");
            migrationBuilder.AddColumn<long>(name: "PatientId", table: "Appointments", type: "bigint", nullable: false);

            migrationBuilder.DropColumn(name: "Id", table: "PasswordResetTokens");
            migrationBuilder.DropColumn(name: "UserId", table: "PasswordResetTokens");
            migrationBuilder.AddColumn<long>(name: "Id", table: "PasswordResetTokens", type: "bigint", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");
            migrationBuilder.AddColumn<long>(name: "UserId", table: "PasswordResetTokens", type: "bigint", nullable: false);

            migrationBuilder.DropColumn(name: "Id", table: "Patients");
            migrationBuilder.AddColumn<long>(name: "Id", table: "Patients", type: "bigint", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "Id", table: "Users");
            migrationBuilder.AddColumn<long>(name: "Id", table: "Users", type: "bigint", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

            // Recreate indexes.
            migrationBuilder.CreateIndex(name: "IX_PasswordResetTokens_UserId", table: "PasswordResetTokens", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_Appointments_PatientId", table: "Appointments", column: "PatientId");
            migrationBuilder.CreateIndex(name: "IX_Visits_PatientId", table: "Visits", column: "PatientId");
            migrationBuilder.CreateIndex(name: "IX_Visits_PatientId_VisitDate", table: "Visits", columns: new[] { "PatientId", "VisitDate" });
            migrationBuilder.CreateIndex(name: "IX_Visits_AppointmentId", table: "Visits", column: "AppointmentId");
            migrationBuilder.CreateIndex(name: "IX_Medications_VisitId", table: "Medications", column: "VisitId");

            // Recreate PKs (parents first).
            migrationBuilder.AddPrimaryKey(name: "PK_Users", table: "Users", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_Patients", table: "Patients", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_PasswordResetTokens", table: "PasswordResetTokens", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_Appointments", table: "Appointments", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_Visits", table: "Visits", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_Medications", table: "Medications", column: "Id");

            // Recreate FKs (parents already exist).
            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetTokens_Users_UserId",
                table: "PasswordResetTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Patients_PatientId",
                table: "Appointments",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Patients_PatientId",
                table: "Visits",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Appointments_AppointmentId",
                table: "Visits",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_Visits_VisitId",
                table: "Medications",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Medications_Visits_VisitId", table: "Medications");
            migrationBuilder.DropForeignKey(name: "FK_Visits_Appointments_AppointmentId", table: "Visits");
            migrationBuilder.DropForeignKey(name: "FK_Visits_Patients_PatientId", table: "Visits");
            migrationBuilder.DropForeignKey(name: "FK_Appointments_Patients_PatientId", table: "Appointments");
            migrationBuilder.DropForeignKey(name: "FK_PasswordResetTokens_Users_UserId", table: "PasswordResetTokens");

            migrationBuilder.DropPrimaryKey(name: "PK_Medications", table: "Medications");
            migrationBuilder.DropPrimaryKey(name: "PK_Visits", table: "Visits");
            migrationBuilder.DropPrimaryKey(name: "PK_Appointments", table: "Appointments");
            migrationBuilder.DropPrimaryKey(name: "PK_PasswordResetTokens", table: "PasswordResetTokens");
            migrationBuilder.DropPrimaryKey(name: "PK_Patients", table: "Patients");
            migrationBuilder.DropPrimaryKey(name: "PK_Users", table: "Users");

            migrationBuilder.DropIndex(name: "IX_PasswordResetTokens_UserId", table: "PasswordResetTokens");
            migrationBuilder.DropIndex(name: "IX_Appointments_PatientId", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Visits_PatientId", table: "Visits");
            migrationBuilder.DropIndex(name: "IX_Visits_PatientId_VisitDate", table: "Visits");
            migrationBuilder.DropIndex(name: "IX_Visits_AppointmentId", table: "Visits");
            migrationBuilder.DropIndex(name: "IX_Medications_VisitId", table: "Medications");

            migrationBuilder.DropColumn(name: "Id", table: "Medications");
            migrationBuilder.DropColumn(name: "VisitId", table: "Medications");
            migrationBuilder.AddColumn<Guid>(name: "Id", table: "Medications", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");
            migrationBuilder.AddColumn<Guid>(name: "VisitId", table: "Medications", type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);

            migrationBuilder.DropColumn(name: "Id", table: "Visits");
            migrationBuilder.DropColumn(name: "PatientId", table: "Visits");
            migrationBuilder.DropColumn(name: "AppointmentId", table: "Visits");
            migrationBuilder.AddColumn<Guid>(name: "Id", table: "Visits", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");
            migrationBuilder.AddColumn<Guid>(name: "PatientId", table: "Visits", type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);
            migrationBuilder.AddColumn<Guid>(name: "AppointmentId", table: "Visits", type: "uniqueidentifier", nullable: true);

            migrationBuilder.DropColumn(name: "Id", table: "Appointments");
            migrationBuilder.DropColumn(name: "PatientId", table: "Appointments");
            migrationBuilder.AddColumn<Guid>(name: "Id", table: "Appointments", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");
            migrationBuilder.AddColumn<Guid>(name: "PatientId", table: "Appointments", type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);

            migrationBuilder.DropColumn(name: "Id", table: "PasswordResetTokens");
            migrationBuilder.DropColumn(name: "UserId", table: "PasswordResetTokens");
            migrationBuilder.AddColumn<Guid>(name: "Id", table: "PasswordResetTokens", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");
            migrationBuilder.AddColumn<Guid>(name: "UserId", table: "PasswordResetTokens", type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);

            migrationBuilder.DropColumn(name: "Id", table: "Patients");
            migrationBuilder.AddColumn<Guid>(name: "Id", table: "Patients", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");

            migrationBuilder.DropColumn(name: "Id", table: "Users");
            migrationBuilder.AddColumn<Guid>(name: "Id", table: "Users", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");

            migrationBuilder.CreateIndex(name: "IX_PasswordResetTokens_UserId", table: "PasswordResetTokens", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_Appointments_PatientId", table: "Appointments", column: "PatientId");
            migrationBuilder.CreateIndex(name: "IX_Visits_PatientId", table: "Visits", column: "PatientId");
            migrationBuilder.CreateIndex(name: "IX_Visits_PatientId_VisitDate", table: "Visits", columns: new[] { "PatientId", "VisitDate" });
            migrationBuilder.CreateIndex(name: "IX_Visits_AppointmentId", table: "Visits", column: "AppointmentId");
            migrationBuilder.CreateIndex(name: "IX_Medications_VisitId", table: "Medications", column: "VisitId");

            migrationBuilder.AddPrimaryKey(name: "PK_Users", table: "Users", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_Patients", table: "Patients", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_PasswordResetTokens", table: "PasswordResetTokens", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_Appointments", table: "Appointments", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_Visits", table: "Visits", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_Medications", table: "Medications", column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetTokens_Users_UserId",
                table: "PasswordResetTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Patients_PatientId",
                table: "Appointments",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Patients_PatientId",
                table: "Visits",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Appointments_AppointmentId",
                table: "Visits",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_Visits_VisitId",
                table: "Medications",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
