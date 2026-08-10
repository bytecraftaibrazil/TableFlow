using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTableMaximumCapacityConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Tables_Capacity_Maximum",
                table: "Tables",
                sql: "[Capacity] <= 50");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tables_Capacity_Maximum",
                table: "Tables");
        }
    }
}
