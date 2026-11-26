using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelCatalog.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomCategoryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roomcategorydetails",
                columns: table => new
                {
                    categoryid = table.Column<int>(type: "integer", nullable: false),
                    maxoccupancy = table.Column<int>(type: "integer", nullable: false),
                    areasquaremeters = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roomcategorydetails", x => x.categoryid);
                    table.ForeignKey(
                        name: "FK_roomcategorydetails_roomcategories_categoryid",
                        column: x => x.categoryid,
                        principalTable: "roomcategories",
                        principalColumn: "categoryid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roomcategorydetails",
                columns: new[] { "categoryid", "areasquaremeters", "maxoccupancy" },
                values: new object[,]
                {
                    { 1, 20.00m, 2 },
                    { 2, 32.50m, 3 },
                    { 3, 55.00m, 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "roomcategorydetails");
        }
    }
}
