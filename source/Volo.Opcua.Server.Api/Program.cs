using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;
using Grpc.Core;
using System.Timers;
using LibUA;

namespace Volo.Opcua.Server.Api
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    aliases: new[] { "--settings" },
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
            var opcuaServer = serviceProvider.GetRequiredService<ServerApplication>();

            var logger = new ConsoleLogger();

            StartOpcuaServer(opcuaServer, appSettings, logger);
            logger.Log(LogLevel.Info, $"OPC-UA Server listening on port {appSettings.OpcuaPort}...");

            StartGrpcServer(opcuaServer, appSettings);
            logger.Log(LogLevel.Info, $"Grpc Server listening on port {appSettings.GrpcPort}...");
        }

        private static void StartOpcuaServer(ServerApplication opcuaServer, AppSettings appSettings, ILogger logger)
        {
            var master = new LibUA.Server.Master(opcuaServer, appSettings.OpcuaPort, appSettings.Timeout, appSettings.Backlog, appSettings.MaxClients, logger);
            master.Start();

            var timer = new Timer(appSettings.MonitoringInterval);
            timer.Elapsed += (sender, e) => { opcuaServer.PlayRow(); };

            timer.Start();
        }

        private static void StartGrpcServer(ServerApplication opcuaServer, AppSettings appSettings)
        {
            var grpcServer = new Grpc.Core.Server
            {
                Services = { DatapointService.BindService(new DatapointApi(opcuaServer)) },
                Ports = { new ServerPort(appSettings.GrpcHost, appSettings.GrpcPort, ServerCredentials.Insecure) }
            };

            grpcServer.Start();
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