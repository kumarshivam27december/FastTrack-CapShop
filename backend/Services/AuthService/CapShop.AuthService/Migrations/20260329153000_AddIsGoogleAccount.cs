using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapShop.AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddIsGoogleAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'IsGoogleAccount') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [IsGoogleAccount] bit NOT NULL CONSTRAINT [DF_Users_IsGoogleAccount] DEFAULT(0);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'IsGoogleAccount') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [DF_Users_IsGoogleAccount];
    ALTER TABLE [Users] DROP COLUMN [IsGoogleAccount];
END");
        }
    }
}
