using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Volo.Opcua.Server
{
    class Program
    {
        static void Main(string[] args)
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

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var server = serviceProvider.GetRequiredService<ServerApplication>();

            var logger = new ConsoleLogger();
            var master = new LibUA.Server.Master(server, 7718, 10, 30, 100, logger);
            master.Start();
            
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


