using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapShop.AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticatorSecret",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentOtp",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAuthenticatorEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailOtpEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSmsOtpEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOtpSentUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpExpiryUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticatorSecret",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentOtp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAuthenticatorEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailOtpEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSmsOtpEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastOtpSentUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OtpExpiryUtc",
                table: "Users");
        }
    }
}
