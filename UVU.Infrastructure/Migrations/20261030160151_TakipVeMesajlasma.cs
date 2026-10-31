using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TakipVeMesajlasma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sohbetler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sohbetler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Takipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TakipEdenId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TakipEdilenId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Takipler", x => x.Id);
                    table.CheckConstraint("CK_Takip_Farkli", "[TakipEdenId] <> [TakipEdilenId]");
                });

            migrationBuilder.CreateTable(
                name: "Mesajlar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SohbetId = table.Column<int>(type: "int", nullable: false),
                    GonderenId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MedyaYolu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GonderiId = table.Column<int>(type: "int", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SilinmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Sohbetler_SohbetId",
                        column: x => x.SohbetId,
                        principalTable: "Sohbetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SohbetKatilimcilari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SohbetId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SonOkumaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SohbetKatilimcilari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SohbetKatilimcilari_Sohbetler_SohbetId",
                        column: x => x.SohbetId,
                        principalTable: "Sohbetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_SohbetId_Id",
                table: "Mesajlar",
                columns: new[] { "SohbetId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SohbetKatilimcilari_SohbetId_KullaniciId",
                table: "SohbetKatilimcilari",
                columns: new[] { "SohbetId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Takipler_TakipEdenId_TakipEdilenId",
                table: "Takipler",
                columns: new[] { "TakipEdenId", "TakipEdilenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Takipler_TakipEdilenId",
                table: "Takipler",
                column: "TakipEdilenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mesajlar");

            migrationBuilder.DropTable(
                name: "SohbetKatilimcilari");

            migrationBuilder.DropTable(
                name: "Takipler");

            migrationBuilder.DropTable(
                name: "Sohbetler");
        }
    }
}
