using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransfer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class F13_PaymentReferenceUniquenessPerProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_OrganizationId_PaymentReference",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrganizationId_ProjectId_PaymentReference",
                table: "Payments",
                columns: new[] { "OrganizationId", "ProjectId", "PaymentReference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_OrganizationId_ProjectId_PaymentReference",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrganizationId_PaymentReference",
                table: "Payments",
                columns: new[] { "OrganizationId", "PaymentReference" },
                unique: true);
        }
    }
}
