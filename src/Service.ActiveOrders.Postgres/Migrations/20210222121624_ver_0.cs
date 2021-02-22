using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Service.ActiveOrders.Postgres.Migrations
{
    public partial class ver_0 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "activeorders");

            migrationBuilder.CreateTable(
                name: "active_orders",
                schema: "activeorders",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "text", nullable: false),
                    WalletId = table.Column<string>(type: "text", nullable: true),
                    BrokerId = table.Column<string>(type: "text", nullable: true),
                    ClientId = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    InstrumentSymbol = table.Column<string>(type: "text", nullable: true),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<double>(type: "double precision", precision: 20, nullable: false),
                    Volume = table.Column<double>(type: "double precision", precision: 20, nullable: false),
                    RemainingVolume = table.Column<double>(type: "double precision", precision: 20, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastSequenceId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_active_orders", x => x.OrderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_active_orders_broker_client",
                schema: "activeorders",
                table: "active_orders",
                columns: new[] { "BrokerId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_active_orders_wallet_order",
                schema: "activeorders",
                table: "active_orders",
                columns: new[] { "WalletId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_balances_balances_wallet",
                schema: "activeorders",
                table: "active_orders",
                column: "WalletId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "active_orders",
                schema: "activeorders");
        }
    }
}
