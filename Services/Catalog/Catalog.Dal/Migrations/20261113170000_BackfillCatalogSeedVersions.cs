using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelCatalog.Dal.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCatalogSeedVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "discountcategories",
                keyColumn: "discountid",
                keyValue: 1,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "discountcategories",
                keyColumn: "discountid",
                keyValue: 2,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "discountcategories",
                keyColumn: "discountid",
                keyValue: 3,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomcategories",
                keyColumn: "categoryid",
                keyValue: 1,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomcategories",
                keyColumn: "categoryid",
                keyValue: 2,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomcategories",
                keyColumn: "categoryid",
                keyValue: 3,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomcategorydetails",
                keyColumn: "categoryid",
                keyValue: 1,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomcategorydetails",
                keyColumn: "categoryid",
                keyValue: 2,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomcategorydetails",
                keyColumn: "categoryid",
                keyValue: 3,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 1, 1 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 1, 3 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 2, 1 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 2, 2 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 2, 3 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 2, 5 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 3, 1 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 3, 2 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 3, 3 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 3, 4 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "roomservices",
                keyColumns: new[] { "categoryid", "serviceid" },
                keyValues: new object[] { 3, 5 },
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "serviceid",
                keyValue: 1,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "serviceid",
                keyValue: 2,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "serviceid",
                keyValue: 3,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "serviceid",
                keyValue: 4,
                column: "Version",
                value: 1);

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "serviceid",
                keyValue: 5,
                column: "Version",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep seeded versions at 1; the preceding audit migration requires Version > 0.
        }
    }
}
