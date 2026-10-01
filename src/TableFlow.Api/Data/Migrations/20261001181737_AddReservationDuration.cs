using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Reservations",
                type: "int",
                nullable: true);

            // Preenche as reservas antigas com 90 minutos.
            migrationBuilder.Sql(
                """
                UPDATE [Reservations]
                SET [DurationMinutes] = 90
                WHERE [DurationMinutes] IS NULL;
                """
            );

            // Torna a coluna obrigatória após preencher os dados.
            migrationBuilder.AlterColumn<int>(
                name: "DurationMinutes",
                table: "Reservations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_DurationMinutes_Positive",
                table: "Reservations",
                sql: "[DurationMinutes] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_DurationMinutes_Positive",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Reservations");
        }
    }
}
