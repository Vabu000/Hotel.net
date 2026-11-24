using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelCatalog.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomServiceAdditionalPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "additionalprice",
                table: "roomservices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 1, 3 },
                column: "additionalprice",
                value: 15m);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 2, 2 },
                column: "additionalprice",
                value: 20m);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 2, 3 },
                column: "additionalprice",
                value: 10m);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 3, 2 },
                column: "additionalprice",
                value: 15m);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 3, 4 },
                column: "additionalprice",
                value: 50m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_roomservices_additionalprice_nonnegative",
                table: "roomservices",
                sql: "additionalprice >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_roomservices_additionalprice_nonnegative",
                table: "roomservices");

            migrationBuilder.DropColumn(
                name: "additionalprice",
                table: "roomservices");
        }
    }
}
