using Hangfire;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace MyHFConsole
{
    public class App
    {
        private readonly IConfigurationRoot configuration;

        public App(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public Task Run()
        {
            Console.WriteLine("Hello World!");

            var dummy = new MyHFTest.CustomHelloWorld(); // to start the recurring test job configured in MyHFTest, a reference to this class needed!

            GlobalConfiguration.Configuration.UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection"));

            using (var server = new BackgroundJobServer())
            {
                Console.WriteLine("Hangfire Server started. Press any key to exit...");
                Console.ReadKey();
            }
            return Task.CompletedTask;
        }
    }
}