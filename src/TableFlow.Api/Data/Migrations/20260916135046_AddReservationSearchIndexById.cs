using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationSearchIndexById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_RestaurantId",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RestaurantId_ReservationDate",
                table: "Reservations",
                columns: new[] { "RestaurantId", "ReservationDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_RestaurantId_ReservationDate",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RestaurantId",
                table: "Reservations",
                column: "RestaurantId");
        }
    }
}
