using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassBuy.Migrations
{
    /// <inheritdoc />
    public partial class DetailsToCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AutoThreshold",
                table: "PassBuyCards",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "PassBuyCards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TopUpAmount",
                table: "PassBuyCards",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopUpMode",
                table: "PassBuyCards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TopUpSchedule",
                table: "PassBuyCards",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoThreshold",
                table: "PassBuyCards");

            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "PassBuyCards");

            migrationBuilder.DropColumn(
                name: "TopUpAmount",
                table: "PassBuyCards");

            migrationBuilder.DropColumn(
                name: "TopUpMode",
                table: "PassBuyCards");

            migrationBuilder.DropColumn(
                name: "TopUpSchedule",
                table: "PassBuyCards");
        }
    }
}
