using Microsoft.EntityFrameworkCore.Migrations;

namespace Service.ActiveOrders.Postgres.Migrations
{
    public partial class ver_1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_balances_balances_wallet",
                schema: "activeorders",
                table: "active_orders",
                newName: "IX_active_orders_wallet");

            migrationBuilder.CreateIndex(
                name: "IX_active_orders_status",
                schema: "activeorders",
                table: "active_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_active_orders_status_lastUpdate",
                schema: "activeorders",
                table: "active_orders",
                columns: new[] { "Status", "LastUpdate" });

            migrationBuilder.CreateIndex(
                name: "IX_active_orders_wallet_status",
                schema: "activeorders",
                table: "active_orders",
                columns: new[] { "WalletId", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_active_orders_status",
                schema: "activeorders",
                table: "active_orders");

            migrationBuilder.DropIndex(
                name: "IX_active_orders_status_lastUpdate",
                schema: "activeorders",
                table: "active_orders");

            migrationBuilder.DropIndex(
                name: "IX_active_orders_wallet_status",
                schema: "activeorders",
                table: "active_orders");

            migrationBuilder.RenameIndex(
                name: "IX_active_orders_wallet",
                schema: "activeorders",
                table: "active_orders",
                newName: "IX_balances_balances_wallet");
        }
    }
}
