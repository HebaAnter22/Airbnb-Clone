using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class paymentPayoutChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvailableBalance",
                table: "hosts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPayoutMethod",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutAccountDetails",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeAccountId",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEarnings",
                table: "hosts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "BookingPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "HostPayout",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HostId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PayoutMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PayoutAccountDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostPayout", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HostPayout_hosts_HostId",
                        column: x => x.HostId,
                        principalTable: "hosts",
                        principalColumn: "host_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Violations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    reported_by_id = table.Column<int>(type: "int", nullable: false),
                    reported_property_id = table.Column<int>(type: "int", nullable: true),
                    reported_host_id = table.Column<int>(type: "int", nullable: true),
                    violation_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    admin_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Violations", x => x.id);
                    table.ForeignKey(
                        name: "FK_Violations_Properties_reported_property_id",
                        column: x => x.reported_property_id,
                        principalTable: "Properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Violations_Users_reported_by_id",
                        column: x => x.reported_by_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Violations_hosts_reported_host_id",
                        column: x => x.reported_host_id,
                        principalTable: "hosts",
                        principalColumn: "host_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HostPayout_HostId",
                table: "HostPayout",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_reported_by_id",
                table: "Violations",
                column: "reported_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_reported_host_id",
                table: "Violations",
                column: "reported_host_id");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_reported_property_id",
                table: "Violations",
                column: "reported_property_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostPayout");

            migrationBuilder.DropTable(
                name: "Violations");

            migrationBuilder.DropColumn(
                name: "AvailableBalance",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "DefaultPayoutMethod",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "PayoutAccountDetails",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "StripeAccountId",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "TotalEarnings",
                table: "hosts");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "BookingPayments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
