using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GonderiVideoDestegi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoYolu",
                table: "Gonderiler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoYolu",
                table: "Gonderiler");
        }
    }
}
