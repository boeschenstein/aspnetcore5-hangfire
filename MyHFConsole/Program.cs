using Hangfire;
using System;

namespace MyHFConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var dummy = new MyHFTest.CustomHelloWorld(); // to start the recurring test job, a reference to this class needed!

            GlobalConfiguration.Configuration.UseSqlServerStorage("Server=(localdb)\\mssqllocaldb;Database=HangfireTest;Integrated Security=SSPI;");

            using (var server = new BackgroundJobServer())
            {
                Console.WriteLine("Hangfire Server started. Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}