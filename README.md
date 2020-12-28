# ASP.NET Core 5 and Hangfire

## Create new Webapi Project

```
dotnet new webapi -o MyHFTest

dotnet new gitignore
```

Toolbar: change from IISExpress to MyHFTest.

## Install Hangfire

Add 3 hangfire packages to csproj

https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="5.0.0" NoWarn="NU1605" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="5.0.0" NoWarn="NU1605" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="5.6.3" />
    <PackageReference Include="Hangfire.Core" Version="1.7.*" />
    <PackageReference Include="Hangfire.SqlServer" Version="1.7.*" />
    <PackageReference Include="Hangfire.AspNetCore" Version="1.7.*" />
  </ItemGroup>
```

Create database in LocalDB

`
CREATE DATABASE [HangfireTest]
GO
`

Add connectionstring and LogLevel to appsettings.json:

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Server=(localdb)\\mssqllocaldb;Database=HangfireTest;Integrated Security=SSPI;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Hangfire": "Information"
    }
  },
  "AllowedHosts": "*"
}
 ```
Add config and Server to services:

```cs
    // Add Hangfire services.
    services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

    // Add the processing server as IHostedService
    services.AddHangfireServer();
```
 Add Dashboard, enqueue first job, add endpoint:

```cs
public void Configure(IApplicationBuilder app, IBackgroundJobClient backgroundJobs, IWebHostEnvironment env)
{
    // add Dashboard
    app.UseHangfireDashboard();
    // add test job 
    backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!")); // check console
    // add recurring job
    RecurringJob.AddOrUpdate(() => Console.WriteLine($"Hello recurring job from Hangfire! {DateTime.Now}"), "0/15 * * * * *"); // cron expression: every 15 seconds

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        // add endpoint
        endpoints.MapHangfireDashboard();
    });

}
```

Press F5 to run the api. Open the Dasboard: https://localhost:5001/hangfire/recurring (ports may vary, check \MyHFTest\Properties\launchSettings.json)

Check job in console and dasboard, under https://localhost:5001/hangfire/jobs/succeeded

`Hello world from Hangfire!`

Check recurring job in console and dashboard: https://localhost:5001/hangfire/recurring

```
Hello recurring job from Hangfire! 05.12.2020 14:59:14
Hello recurring job from Hangfire! 05.12.2020 14:59:14
Hello recurring job from Hangfire! 05.12.2020 14:59:14
Hello recurring job from Hangfire! 05.12.2020 14:59:14
Hello recurring job from Hangfire! 05.12.2020 14:59:14
Hello recurring job from Hangfire! 05.12.2020 14:59:14
...
```
The reason for this unexpected behavior: the call gets serialized and then executed: details: https://docs.hangfire.io/en/latest/background-methods/

To fix this, you need to pass dependency: https://docs.hangfire.io/en/latest/background-methods/passing-dependencies.html

```cs
RecurringJob.AddOrUpdate<CustomHelloWorld>(x => x.LogThis("Hello recurring job from Hangfire (fixed by dependency)!"), "0/15 * * * * *");
```

Add class:

```cs
public class CustomHelloWorld
{
    public void LogThis(string info)
    {
        Console.WriteLine(info + $" {DateTime.Now}");
    }
}
```
 
 Output:
 
```
Hello recurring job from Hangfire (fixed by dependency)! 05.12.2020 15:37:15
Hello recurring job from Hangfire (fixed by dependency)! 05.12.2020 15:37:30
Hello recurring job from Hangfire (fixed by dependency)! 05.12.2020 15:37:45
Hello recurring job from Hangfire (fixed by dependency)! 05.12.2020 15:37:45
Hello recurring job from Hangfire (fixed by dependency)! 05.12.2020 15:38:00
```

## Run Hangfire in Console App

`dotnet new console -o HFConsole`

Add this to the Main:

```cs
private static void Main(string[] args)
{
    GlobalConfiguration.Configuration.UseSqlServerStorage("Server=(localdb)\\mssqllocaldb;Database=HangfireTest;Integrated Security=SSPI;");
    using (var server = new BackgroundJobServer())
    {
        Console.WriteLine("Hangfire Server started. Press any key to exit...");
        Console.ReadKey();
    }
}
```

## Run Hangfire in Windows Service

https://docs.hangfire.io/en/latest/background-processing/processing-jobs-in-windows-service.html

Add a Windows Service. Details: https://github.com/boeschenstein/core3-windows-service/blob/main/README.md

`dotnet new worker -o MyHFService`

Add Hangfire to Service:

```
Install-Package Hangfire.Core
Install-Package Hangfire.SqlServer
```

Copy Connection string to appsettings.json of service.

Disable Hangfire in WebApi:

```cs
// Add the processing server as IHostedService
// services.AddHangfireServer();
```

Use Hangfire in Service:

```cs
public class HFWorker : BackgroundService
{
    private BackgroundJobServer _server;

    public HFWorker()
    {
        GlobalConfiguration.Configuration.UseSqlServerStorage("HangfireConnection");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _server = new BackgroundJobServer();
        return base.StartAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _server.Dispose();
        base.Dispose();
    }
}
```

> TODO: Hangfire server works in debug, but COMPILED SERVER RUN BY SC CANNOT BE SEEN BY HANGFIRE DASHBOARD !! WHY !? ACL ?? FIREWALL ??

## Set or Disable Retry on error

Add filter globally:

```cs
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete }); // global Hangfire retry rule
```

or set the attribute to the job execution:

```cs
[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)] // or set in global fiters
public async Task SendCommand(IRequest request)
{
    await this._mediator.Send(request);
}
```

Source: <https://www.faciletechnolab.com/blog/2018/8/30/5-helpful-tips-to-use-hangfire-for-background-scheduling-in-better-way>

## Filters

### Do not start jobs after restart of HangFire

Hangfire will always try to restart your job in case of an error or outage. You can use a global filter to solve this.

```cs
    /// <summary>
    /// https://github.com/HangfireIO/Hangfire/issues/620#issuecomment-466385193
    /// </summary>
    public class NoMissedRunsAttribute : JobFilterAttribute, IClientFilter
    {
        private readonly IServiceCollection _services;

        public NoMissedRunsAttribute(IServiceCollection services)
        {
            _services = services;
        }

        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(15);

        public void OnCreating(CreatingContext filterContext)
        {
            using (var loggerFactory = _services.BuildServiceProvider().GetService<ILoggerFactory>())
            {
                var logger = loggerFactory.CreateLogger<NoMissedRunsAttribute>();

                logger.LogDebug($"Hangfire Filter OnCreating!");

                // if (context.Parameters.TryGetValue("RecurringJobId", out var recurringJobId) && context.InitialState?.Reason == "Triggered by recurring job scheduler")
                if (filterContext.Parameters.TryGetValue("RecurringJobId", out var recurringJobId))
                {
                    // the job being created looks like a recurring job instance,
                    // and triggered by a scheduler (i.e. not manually) at that.

                    var recurringJob = filterContext.Connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
                    logger.LogDebug($"Hangfire Filter OnCreating! recurringJobId={recurringJobId}");

                    if (recurringJob != null && recurringJob.TryGetValue("NextExecution", out var nextExecution))
                    {
                        // the next execution time of a recurring job is updated AFTER the job instance creation,
                        // so at the moment it still contains the scheduled execution time from the previous run.

                        var scheduledTime = JobHelper.DeserializeDateTime(nextExecution);

                        if (DateTime.UtcNow > scheduledTime + MaxDelay)
                        {
                            // the job is created way later than expected
                            filterContext.Canceled = true;
                            logger.LogWarning($"{nameof(NoMissedRunsAttribute)}: Hangfire Execution canceled because it run too late! recurringJobId={recurringJobId}. plannedUtc={DateTime.UtcNow}, scheduledTime={scheduledTime}, maxDelay={MaxDelay}");
                        }
                        else
                        {
                            logger.LogDebug($"{nameof(NoMissedRunsAttribute)}: Hangfire Execution not canceled. recurringJobId={recurringJobId}. plannedUtc={DateTime.UtcNow}, scheduledTime={scheduledTime}, maxDelay={MaxDelay}");
                        }
                    }
                }
            }
        }

        public void OnCreated(CreatedContext context)
        {
            // logger.LogDebug($"Hangfire Filter OnCreating!");
        }
    }

```

Configure this globally:

```cs
GlobalJobFilters.Filters.Add(new NoMissedRunsAttribute(services) { MaxDelay = 10) }); // global Hangfire filter
```

Or add the attribute to the job execution

```cs
[NoMissedRuns()] already set in global fiters
public async Task SendCommand(IRequest request)
{
    await this._mediator.Send(request);
}
```

## Customize Job name in Hangfire Dashboard

Set JobDisplayNameAttribute to use the ToString() method of the request:

```cs
[JobDisplayName("{0}, {1}")]
public async Task SendCommand(string name, IRequest request) // arguments get numbered 0, 1, ...
{
    await this._mediator.Send(request);
}
```

## Timezones

When you are writing Hangfire jobs and schedule to run hourly or daily or any other recurrence, timezone matters the most in many cases. By default Hangfire uses UTC time. That means when you do not specify timezone information, its considered as UTC. You can configure jobs to run at a time and also pass on TimezoneInfo to it so that you can configure timezone specific time. Here is an example:

`RecurringJob.AddOrUpdate(() => Console.Write(), "15 18 * * *", TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));`

Source: https://www.faciletechnolab.com/blog/2018/8/30/5-helpful-tips-to-use-hangfire-for-background-scheduling-in-better-way

## Information

- to debug, set both projects as startup
