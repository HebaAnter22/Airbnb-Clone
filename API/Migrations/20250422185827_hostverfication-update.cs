using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class hostverficationupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HostVerifications_hosts_HostId",
                table: "HostVerifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingPayments_Status",
                table: "BookingPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingPayments_TransactionId",
                table: "BookingPayments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "HostVerifications");

            migrationBuilder.RenameColumn(
                name: "DocumentUrl",
                table: "HostVerifications",
                newName: "DocumentUrl2");

            migrationBuilder.AlterColumn<int>(
                name: "HostId",
                table: "HostVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl1",
                table: "HostVerifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HostVerifications_hosts_HostId",
                table: "HostVerifications",
                column: "HostId",
                principalTable: "hosts",
                principalColumn: "host_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HostVerifications_hosts_HostId",
                table: "HostVerifications");

            migrationBuilder.DropColumn(
                name: "DocumentUrl1",
                table: "HostVerifications");

            migrationBuilder.RenameColumn(
                name: "DocumentUrl2",
                table: "HostVerifications",
                newName: "DocumentUrl");

            migrationBuilder.AlterColumn<int>(
                name: "HostId",
                table: "HostVerifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "HostVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingPayments_Status",
                table: "BookingPayments",
                sql: "[status] IN ('pending', 'completed', 'failed', 'refunded')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingPayments_TransactionId",
                table: "BookingPayments",
                sql: "[payment_method_type] IN ('credit_card', 'paypal', 'bank_transfer', 'other')");

            migrationBuilder.AddForeignKey(
                name: "FK_HostVerifications_hosts_HostId",
                table: "HostVerifications",
                column: "HostId",
                principalTable: "hosts",
                principalColumn: "host_id");
        }
    }
}
