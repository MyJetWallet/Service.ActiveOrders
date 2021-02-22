﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Service.ActiveOrders.Postgres;

namespace Service.ActiveOrders.Postgres.Migrations
{
    [DbContext(typeof(ActiveOrdersContext))]
    [Migration("20210222121624_ver_0")]
    partial class ver_0
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("activeorders")
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.3")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("Service.ActiveOrders.Postgres.OrderEntity", b =>
                {
                    b.Property<string>("OrderId")
                        .HasColumnType("text");

                    b.Property<string>("BrokerId")
                        .HasColumnType("text");

                    b.Property<string>("ClientId")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("InstrumentSymbol")
                        .HasColumnType("text");

                    b.Property<long>("LastSequenceId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<double>("Price")
                        .HasPrecision(20)
                        .HasColumnType("double precision");

                    b.Property<double>("RemainingVolume")
                        .HasPrecision(20)
                        .HasColumnType("double precision");

                    b.Property<int>("Side")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<double>("Volume")
                        .HasPrecision(20)
                        .HasColumnType("double precision");

                    b.Property<string>("WalletId")
                        .HasColumnType("text");

                    b.HasKey("OrderId")
                        .HasName("PK_active_orders");

                    b.HasIndex("WalletId")
                        .HasDatabaseName("IX_balances_balances_wallet");

                    b.HasIndex("BrokerId", "ClientId")
                        .HasDatabaseName("IX_active_orders_broker_client");

                    b.HasIndex("WalletId", "OrderId")
                        .HasDatabaseName("IX_active_orders_wallet_order");

                    b.ToTable("active_orders");
                });
#pragma warning restore 612, 618
        }
    }
}
