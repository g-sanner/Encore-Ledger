using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EncoreLedger.Migrations
{
    /// <inheritdoc />
    public partial class ReportModelAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Report",
                columns: table => new
                {
                    IDReport = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeriodStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    PDFContent = table.Column<byte[]>(type: "BLOB", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Report", x => x.IDReport);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Report");
        }
    }
}
