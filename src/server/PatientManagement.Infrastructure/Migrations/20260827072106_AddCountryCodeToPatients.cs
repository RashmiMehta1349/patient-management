using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryCodeToPatients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Patients",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "+91");

            // Existing PhoneNumber values may contain formatting characters (dashes, spaces, a
            // leading "+") and/or exceed the new 10-digit column width, e.g. from before the
            // CountryCode column existed. Normalize to digits-only and keep the last 10 before
            // narrowing the column, so ALTER COLUMN below doesn't fail with a truncation error.
            migrationBuilder.Sql(@"
                UPDATE [Patients]
                SET [PhoneNumber] = RIGHT(
                    REPLACE(REPLACE(REPLACE(REPLACE([PhoneNumber], '-', ''), ' ', ''), '(', ''), ')', ''),
                    10)
                WHERE [PhoneNumber] IS NOT NULL;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Patients",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Patients");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);
        }
    }
}
