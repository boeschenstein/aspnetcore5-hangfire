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

TODO: SERVER CANNOT BE SEEN BY HANGFIRE !! WHY ??

## Set or Disable Retry on error

GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });
