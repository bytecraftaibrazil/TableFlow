using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Tables_Capacity_Positive",
                table: "Tables",
                sql: "[Capacity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tables_Number_Positive",
                table: "Tables",
                sql: "[Number] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_PartySize_Positive",
                table: "Reservations",
                sql: "[PartySize] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_Status_Valid",
                table: "Reservations",
                sql: "[Status] IN ('Pending', 'Confirmed', 'Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tables_Capacity_Positive",
                table: "Tables");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tables_Number_Positive",
                table: "Tables");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_PartySize_Positive",
                table: "Reservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_Status_Valid",
                table: "Reservations");
        }
    }
}
