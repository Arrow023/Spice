using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spice.Data.Migrations
{
    public partial class AddedNameColToOrderDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "OrderDetails");
        }
    }
}
