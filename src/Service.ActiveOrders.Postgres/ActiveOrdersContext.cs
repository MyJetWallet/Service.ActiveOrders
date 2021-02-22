using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Service.ActiveOrders.Postgres
{
    public class ActiveOrdersContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;
        public const string Schema = "activeorders";

        public const string ActiveOrderTableName = "active_orders";

        public DbSet<OrderEntity> ActiveOrders { get; set; }

        public ActiveOrdersContext(DbContextOptions options, ILoggerFactory loggerFactory) : base(options)
        {
            _loggerFactory = loggerFactory;
            InitSqlStatement();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_loggerFactory != null)
            {
                //optionsBuilder.UseLoggerFactory(_loggerFactory).EnableSensitiveDataLogging();
            }
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.Entity<OrderEntity>().ToTable(ActiveOrderTableName);
            modelBuilder.Entity<OrderEntity>().HasKey(e => e.OrderId).HasName("PK_active_orders");
            modelBuilder.Entity<OrderEntity>().HasIndex(e => new { e.WalletId, e.OrderId }).HasDatabaseName("IX_active_orders_wallet_order");
            modelBuilder.Entity<OrderEntity>().HasIndex(e => new { e.BrokerId, e.ClientId }).HasDatabaseName("IX_active_orders_broker_client");
            modelBuilder.Entity<OrderEntity>().HasIndex(e => e.WalletId).HasDatabaseName("IX_balances_balances_wallet");
            modelBuilder.Entity<OrderEntity>().Property(e => e.Price).HasPrecision(20);
            modelBuilder.Entity<OrderEntity>().Property(e => e.Volume).HasPrecision(20);
            modelBuilder.Entity<OrderEntity>().Property(e => e.RemainingVolume).HasPrecision(20);

            base.OnModelCreating(modelBuilder);
        }

        //todo: add metrics to this method
        public async Task<int> InsertOrUpdateAsync(IEnumerable<OrderEntity> entities)
        {
            var list = entities.ToList();
            var index = 0;
            var countInsert = 0;

            while (index < list.Count)
            {
                var paramString = "";
                foreach (var entity in list.Skip(index).Take(500))
                {
                    if (!string.IsNullOrEmpty(paramString))
                        paramString += ",";

                    paramString += string.Format(_sqlInsertValues,
                        entity.OrderId,
                        entity.WalletId,
                        //entity.BrokerId,
                        //entity.ClientId,
                        entity.WalletId,
                        entity.WalletId,

                        (int)entity.Type,
                        entity.InstrumentSymbol,
                        (int)entity.Side,
                        entity.Price.ToString(CultureInfo.InvariantCulture),
                        entity.Volume.ToString(CultureInfo.InvariantCulture),
                        entity.RemainingVolume.ToString(CultureInfo.InvariantCulture),
                        entity.CreatedTime.ToString("O"),
                        entity.LastUpdate.ToString("O"),
                        (int)entity.Status,
                        entity.LastSequenceId);

                    index++;
                }

                var sql = $"{_sqlInsert} {paramString} {_sqlInsertWhere}";

                try
                {
                    var result = await Database.ExecuteSqlRawAsync(sql);

                    countInsert += result;
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"InsertOrUpdateAsync exception:\n{ex}\n{sql}");

                }
            }

            return countInsert;
        }

        public async Task<int> DeleteAsync(IEnumerable<string> orderIdList)
        {
            var list = orderIdList.ToList();

            if (!list.Any())
            {
                return 0;
            }

            var index = 0;
            var countDelete = 0;

            while (index < list.Count)
            {
                var paramString = "";
                foreach (var orderId in list.Skip(index).Take(100))
                {
                    if (string.IsNullOrEmpty(paramString))
                    {
                        paramString += $"'{orderId}'";
                    }
                    else
                    {
                        paramString += $", '{orderId}'";
                    }

                    index++;
                }

                var sql = $"{_sqlDelete1} {paramString} {_sqlDelete2}";

                try
                {
                    var result = await Database.ExecuteSqlRawAsync(sql);

                    countDelete += result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"InsertOrUpdateAsync exception:\n{ex}\n{sql}");

                }
            }

            return countDelete;
        }


        private string _sqlInsert;
        private string _sqlInsertValues;
        private string _sqlInsertWhere;

        private string _sqlDelete1;
        private string _sqlDelete2;

        private void InitSqlStatement()
        {
            // ReSharper disable once EntityNameCapturedOnly.Local
            OrderEntity entity;

            _sqlInsert = $" insert into {Schema}.{ActiveOrderTableName} " +
                         $" (\"{nameof(entity.OrderId)}\", \"{nameof(entity.WalletId)}\", \"{nameof(entity.BrokerId)}\", \"{nameof(entity.ClientId)}\", \"{nameof(entity.Type)}\", \"{nameof(entity.InstrumentSymbol)}\", \"{nameof(entity.Side)}\", \"{nameof(entity.Price)}\", \"{nameof(entity.Volume)}\", \"{nameof(entity.RemainingVolume)}\", \"{nameof(entity.CreatedTime)}\", \"{nameof(entity.LastUpdate)}\", \"{nameof(entity.Status)}\", \"{nameof(entity.LastSequenceId)}\")" +
                         " values ";

            _sqlInsertWhere = $" ON CONFLICT( \"{nameof(entity.OrderId)}\" )" +
                              " DO UPDATE SET" +
                              $" \"{nameof(entity.RemainingVolume)}\" = EXCLUDED.\"{nameof(entity.RemainingVolume)}\"," +
                              $" \"{nameof(entity.Status)}\" = EXCLUDED.\"{nameof(entity.Status)}\"," +
                              $" \"{nameof(entity.LastUpdate)}\" = EXCLUDED.\"{nameof(entity.LastUpdate)}\"," +
                              $" \"{nameof(entity.LastSequenceId)}\" = EXCLUDED.\"{nameof(entity.LastSequenceId)}\"" +
                              $" WHERE EXCLUDED.\"{nameof(entity.LastSequenceId)}\" > {Schema}.{ActiveOrderTableName}.\"{nameof(entity.LastSequenceId)}\"";

            //"(OrderId, WalletId, BrokerId, ClientId, Type (4), InstrumentSymbol, Side, Price, Volume, RemainingVolume, CreatedTime, LastUpdate, Status, LastSequenceId)"
            _sqlInsertValues = " ('{0}', '{1}', '{2}', '{3}', {4}, '{5}', {6}, {7}, {8}, {9}, '{10}', '{11}', {12}, {13})";

            _sqlDelete1 = $" delete from {Schema}.{ActiveOrderTableName} where \"{nameof(entity.OrderId)}\" in (";
            _sqlDelete2 = ")";
        }

    }
}
