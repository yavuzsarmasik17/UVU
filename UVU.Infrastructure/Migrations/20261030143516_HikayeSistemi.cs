using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UVU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HikayeSistemi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hikayeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MedyaYolu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MedyaTipi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EfektKodu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hikayeler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HikayeGoruntulemeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HikayeId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GoruntulemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HikayeGoruntulemeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HikayeGoruntulemeleri_Hikayeler_HikayeId",
                        column: x => x.HikayeId,
                        principalTable: "Hikayeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HikayeKatmanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HikayeId = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    X = table.Column<double>(type: "float", nullable: false),
                    Y = table.Column<double>(type: "float", nullable: false),
                    Olcek = table.Column<double>(type: "float", nullable: false),
                    DonmeAcisi = table.Column<double>(type: "float", nullable: false),
                    Renk = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    YaziTipi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sira = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HikayeKatmanlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HikayeKatmanlari_Hikayeler_HikayeId",
                        column: x => x.HikayeId,
                        principalTable: "Hikayeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HikayeMuzikleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HikayeId = table.Column<int>(type: "int", nullable: false),
                    MuzikId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SarkiAdi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Sanatci = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BaslangicSaniyesi = table.Column<double>(type: "float", nullable: false),
                    SureSaniyesi = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HikayeMuzikleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HikayeMuzikleri_Hikayeler_HikayeId",
                        column: x => x.HikayeId,
                        principalTable: "Hikayeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HikayeGoruntulemeleri_HikayeId_KullaniciId",
                table: "HikayeGoruntulemeleri",
                columns: new[] { "HikayeId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HikayeKatmanlari_HikayeId",
                table: "HikayeKatmanlari",
                column: "HikayeId");

            migrationBuilder.CreateIndex(
                name: "IX_Hikayeler_KullaniciId_BitisTarihi",
                table: "Hikayeler",
                columns: new[] { "KullaniciId", "BitisTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HikayeMuzikleri_HikayeId",
                table: "HikayeMuzikleri",
                column: "HikayeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HikayeGoruntulemeleri");

            migrationBuilder.DropTable(
                name: "HikayeKatmanlari");

            migrationBuilder.DropTable(
                name: "HikayeMuzikleri");

            migrationBuilder.DropTable(
                name: "Hikayeler");
        }
    }
}
