using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyHFService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private BackgroundJobServer _server;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 }); // before storage

            GlobalConfiguration.Configuration.UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("hello3");
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10000, stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Starting Hangfire Server at: {time}", DateTimeOffset.Now);
            _server = new BackgroundJobServer();
            _logger.LogWarning("Hangfire Server started at: {time}", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Stopping Worker at: {time}", DateTimeOffset.Now);
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _server.Dispose();
            base.Dispose();
            _logger.LogWarning("Worker disposed at: {time}", DateTimeOffset.Now);
        }
    }
}