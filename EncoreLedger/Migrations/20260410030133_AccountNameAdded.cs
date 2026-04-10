using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EncoreLedger.Migrations
{
    /// <inheritdoc />
    public partial class AccountNameAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "Account",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "Account");
        }
    }
}
