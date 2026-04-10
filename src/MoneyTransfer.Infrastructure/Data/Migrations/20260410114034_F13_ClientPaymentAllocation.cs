using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransfer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class F13_ClientPaymentAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientPaymentHeaders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPaymentHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientPaymentHeaders_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientPaymentHeaders_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPaymentHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientPaymentAllocations_ClientPaymentHeaders_ClientPaymentHeaderId",
                        column: x => x.ClientPaymentHeaderId,
                        principalTable: "ClientPaymentHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientPaymentAllocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientPaymentAllocations_ClientPaymentHeaderId",
                table: "ClientPaymentAllocations",
                column: "ClientPaymentHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPaymentAllocations_ProjectId",
                table: "ClientPaymentAllocations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPaymentHeaders_ClientId",
                table: "ClientPaymentHeaders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPaymentHeaders_OrganizationId",
                table: "ClientPaymentHeaders",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPaymentHeaders_OrganizationId_PaymentReference",
                table: "ClientPaymentHeaders",
                columns: new[] { "OrganizationId", "PaymentReference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientPaymentAllocations");

            migrationBuilder.DropTable(
                name: "ClientPaymentHeaders");
        }
    }
}
