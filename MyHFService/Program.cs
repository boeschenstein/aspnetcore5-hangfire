using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyHFService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var dummy = new MyHFTest.CustomHelloWorld(); // to start the recurring test job configured in MyHFTest, a reference to this class needed!

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    //builder
                    //    .AddJsonFile("appsettings.json")
                    //    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                    //    .AddEnvironmentVariables()
                    //    .Build();
                })
                .ConfigureLogging(loggerFactory => loggerFactory.AddEventLog()) // needs `using Microsoft.Extensions.Logging;`
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                })
                .UseWindowsService();
    }
}