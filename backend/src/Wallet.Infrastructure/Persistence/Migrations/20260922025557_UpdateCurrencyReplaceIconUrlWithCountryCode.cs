using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCurrencyReplaceIconUrlWithCountryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "icon_url",
                table: "currencies");

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "currencies",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "currencies",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"),
                column: "country_code",
                value: "BD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "country_code",
                table: "currencies");

            migrationBuilder.AddColumn<string>(
                name: "icon_url",
                table: "currencies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "currencies",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-4111-8111-111111111111"),
                column: "icon_url",
                value: "");
        }
    }
}
