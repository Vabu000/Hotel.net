using HotelCatalog.Dal.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HotelCatalog.Dal.Migrations;

[DbContext(typeof(HotelCatalogDbContext))]
[Migration("20261113161500_AddCatalogAuditFields")]
public sealed class AddCatalogAuditFields : Migration
{
    private static readonly string[] Tables =
    ["clientdiscounts", "discountcategories", "roomcategorydetails", "roomcategories", "roomservices", "services"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            migrationBuilder.AddColumn<DateTime>("CreatedAt", table, type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
            migrationBuilder.AddColumn<string>("CreatedBy", table, type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<bool>("IsDeleted", table, type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<DateTime>("UpdatedAt", table, type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>("UpdatedBy", table, type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<int>("Version", table, type: "integer", nullable: false, defaultValue: 1);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            migrationBuilder.DropColumn("CreatedAt", table);
            migrationBuilder.DropColumn("CreatedBy", table);
            migrationBuilder.DropColumn("IsDeleted", table);
            migrationBuilder.DropColumn("UpdatedAt", table);
            migrationBuilder.DropColumn("UpdatedBy", table);
            migrationBuilder.DropColumn("Version", table);
        }
    }
}
