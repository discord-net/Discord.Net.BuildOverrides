using BuildOverrideService;
using BuildOverrideService.Http;
using BuildOverrideService.Services;
using Discord.Webhook;
using EdgeDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

Logger.AddStream(Console.OpenStandardError(), StreamType.StandardError);
Logger.AddStream(Console.OpenStandardOutput(), StreamType.StandardOut);

var log = Logger.GetLogger<Program>();

log.Debug($"Env test: {(GetEnv("EDGEDB_HOST") == null ? "failed" : "succeeded")}");

try
{
    var builder = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(x => new JsonSerializer());
        services.AddSingleton<OverrideService>();
        services.AddSingleton<DependencyService>();
        services.AddSingleton(x => new DiscordWebhookClient(GetEnv("WEBHOOK")));
        services.AddSingleton(x =>
        {
            return new EdgeDBClient(new EdgeDBConnection
            {
                Hostname = GetEnv("EDGEDB_HOST"),
                Username = GetEnv("EDGEDB_USER"),
                Password = GetEnv("EDGEDB_PASS"),
                Database = "edgedb",
                Port = int.Parse(GetEnv("EDGEDB_PORT")!),

            }, new EdgeDBConfig
            {
                Logger = ProxyLogger.Create<EdgeDBClient>(),
                AllowUnsecureConnection = true,
                RequireCertificateMatch = false,
            });
        });
        services.AddSingleton<DatabaseService>();
        services.AddHostedService<HttpServer>();
    })
    .UseConsoleLifetime();

    using (var host = builder.Build())
    {
        await host.RunAsync();
    }
}
catch(Exception x)
{
    log.Critical("Failed in main", Severity.Core, x);
}

string? GetEnv(string n)
    => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.Process);

public class ProxyLogger : ILogger
{
    private readonly Logger _logger;

    private ProxyLogger(Logger logger)
    {
        _logger = logger;
    }

    public static ProxyLogger Create<TType>()
        => new ProxyLogger(Logger.GetLogger<TType>());

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var level = logLevel switch
        {
            LogLevel.Debug => Severity.Debug,
            LogLevel.Critical => Severity.Critical,
            LogLevel.Error => Severity.Error,
            LogLevel.Information => Severity.Info,
            LogLevel.None => Severity.Core,
            LogLevel.Trace => Severity.Trace,
            LogLevel.Warning => Severity.Warning,
            _ => Severity.Core,
        };

        var msg = formatter(state, exception);

        _logger.Write(msg, exception, severity: new Severity[] { Severity.Database, level });
    }
}