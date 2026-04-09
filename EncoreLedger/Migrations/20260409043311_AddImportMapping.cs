using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EncoreLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddImportMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportMapping",
                columns: table => new
                {
                    IDImportMapping = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true),
                    DateIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    DescriptionIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    AmountIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    DebitIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    CreditIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportMapping", x => x.IDImportMapping);
                    table.ForeignKey(
                        name: "FK_ImportMapping_Account_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Account",
                        principalColumn: "IDAccount",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportMapping_AccountID",
                table: "ImportMapping",
                column: "AccountID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportMapping");
        }
    }
}
