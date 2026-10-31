using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GonderiEtkilesimleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GonderiBegenileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderiId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GonderiBegenileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GonderiBegenileri_Gonderiler_GonderiId",
                        column: x => x.GonderiId,
                        principalTable: "Gonderiler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GonderiKaydetmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderiId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GonderiKaydetmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GonderiKaydetmeleri_Gonderiler_GonderiId",
                        column: x => x.GonderiId,
                        principalTable: "Gonderiler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GonderiYorumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderiId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GonderiYorumlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GonderiYorumlari_Gonderiler_GonderiId",
                        column: x => x.GonderiId,
                        principalTable: "Gonderiler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GonderiBegenileri_GonderiId_KullaniciId",
                table: "GonderiBegenileri",
                columns: new[] { "GonderiId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GonderiKaydetmeleri_GonderiId_KullaniciId",
                table: "GonderiKaydetmeleri",
                columns: new[] { "GonderiId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GonderiYorumlari_GonderiId_OlusturmaTarihi",
                table: "GonderiYorumlari",
                columns: new[] { "GonderiId", "OlusturmaTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GonderiBegenileri");

            migrationBuilder.DropTable(
                name: "GonderiKaydetmeleri");

            migrationBuilder.DropTable(
                name: "GonderiYorumlari");
        }
    }
}
