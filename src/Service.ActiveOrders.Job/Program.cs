using System;
using System.Net;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MySettingsReader;
using Service.ActiveOrders.Domain.Settings;

namespace Service.ActiveOrders.Job
{
    public class Program
    {
        public const string SettingsFileName = ".myjetwallet";

        public static SettingsModel Settings { get; private set; }

        public static Func<T> ReloadedSettings<T>(Func<SettingsModel, T> getter)
        {
            return () =>
            {
                var settings = SettingsReader.GetSettings<SettingsModel>(SettingsFileName);
                var value = getter.Invoke(settings);
                return value;
            };
        }

        public static ILoggerFactory LoggerFactory { get; private set; }

        public static void Main(string[] args)
        {
            Console.Title = "MyJetWallet Service.ActiveOrders";

            Settings = SettingsReader.GetSettings<SettingsModel>(SettingsFileName);

            using var loggerFactory = LogConfigurator.ConfigureElk("MyJetWallet", Settings.SeqServiceUrl, Settings.ElkLogs);

            LoggerFactory = loggerFactory;

            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                logger.LogInformation("Application is being started");

                CreateHostBuilder(loggerFactory, args).Build().Run();

                logger.LogInformation("Application has been stopped");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Application has been terminated unexpectedly");
            }
        }

        public static IHostBuilder CreateHostBuilder(ILoggerFactory loggerFactory, string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 8080, o => o.Protocols = HttpProtocols.Http1);
                        options.Listen(IPAddress.Any, 80, o => o.Protocols = HttpProtocols.Http2);
                    });

                    webBuilder.UseUrls("http://*:5000", "http://*:5001");

                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(loggerFactory);
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                });
    }
}
