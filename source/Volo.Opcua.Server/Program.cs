using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Timers;

namespace Volo.Opcua.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    aliases: new[] { "--settings" },
                    getDefaultValue: () => "appsettings.json",
                    description: "The path to the settings file")
            };

            rootCommand.Description = "Creates the OPCUA server for the volo project";
            rootCommand.Handler = CommandHandler.Create<string>(Initialize);
            rootCommand.Invoke(args);
        }

        private static void Initialize(string settings)
        {
            var appSettings = GetAppSettings(settings);
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(appSettings);
            serviceCollection.AddSingleton<ServerApplication>();
            serviceCollection.AddSingleton<SecurityProvider>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var server = serviceProvider.GetRequiredService<ServerApplication>();

            var logger = new ConsoleLogger();
            var master = new LibUA.Server.Master(server, appSettings.Port, appSettings.Timeout, appSettings.Backlog, appSettings.MaxClients, logger);
            master.Start();

            var timer = new Timer(appSettings.MonitoringInterval);
            timer.Elapsed += (sender, e) => { server.PlayRow(); };

            timer.Start();

            Console.WriteLine($"Server listening on port {appSettings.Port}...");
        }

        private static AppSettings GetAppSettings(string settings)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(settings);
            var config = configBuilder.Build();

            var appSettings = new AppSettings();
            config.Bind("applicationConfig", appSettings);

            return appSettings;
        }
    }
}