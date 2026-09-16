using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status_ReservationDate",
                table: "Reservations",
                columns: new[] { "Status", "ReservationDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_Status_ReservationDate",
                table: "Reservations");
        }
    }
}
