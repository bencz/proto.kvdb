using System.IO.Compression;
using System.Net;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Proto.KvDb.API.GrpcService;
using Proto.KvDb.API.HostedServices;
using Proto.Remote.HealthChecks;
using Serilog;
using Serilog.Templates;

namespace Proto.KvDb.API;

public static class ProgramExtension
{   
    private const string ApplicationName = "Proto.Actor KV database";
    private const string CorsPolicyName = "_myCorsPolicy";
    
    public static void AddCustomSerilog(this WebApplicationBuilder builder)
    {
        var expressionTemplate = new ExpressionTemplate(
            "[{@t:yyyy-MM-dd HH:mm:ss} {@l:u3} {SourceContext}] CorrelationId={CorrelationId} RequestPath={RequestPath}{#each name, value in Rest()} {name}={value}{#end}    Msg={@m:lj}    \n{@x}");

        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId()
            .Enrich.WithCorrelationIdHeader()
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console(expressionTemplate)
            .CreateLogger();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddLogging((builder) =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });
    }
    
    public static void ConfigureAppSettings(this WebApplicationBuilder builder)
    {
        var secretsPath = Environment.GetEnvironmentVariable("SECRETS_PATH") ?? "";
        if (!string.IsNullOrEmpty(secretsPath))
        {
            builder.Configuration.AddJsonFile(
                secretsPath + "appsettings.json", false);
        }
    }
    
    public static void ConfigureKestrel(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Any, Convert.ToInt32(Environment.GetEnvironmentVariable("HTTP_PORT") ?? "80"), o => o.Protocols = HttpProtocols.Http1AndHttp2);
            kestrel.Listen(IPAddress.Any, Convert.ToInt32(Environment.GetEnvironmentVariable("GRPC_PORT") ?? "51000"), o => o.Protocols = HttpProtocols.Http2);
        });
    }
    
    public static void AddHostedServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<ActorSystemClusterHostedService>();
    }

    public static void AddCustomHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddCheck<ActorSystemHealthCheck>(
                name: "actor-system",
                failureStatus: HealthStatus.Unhealthy);
    }

    public static void AddCustomSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{ApplicationName}", Version = "v1" }); });
    }

    public static void AddApiConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddGrpc(options =>
        {
            options.ResponseCompressionLevel = CompressionLevel.Fastest;
        });
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
    }

    public static void UseCustomMapHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/hc", new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecks("/liveness", new HealthCheckOptions
        {
            Predicate = r => r.Name.Contains("self")
        });
    }
    
    public static void UseCustomSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{ApplicationName} V1"); });
    }

    public static void MapEndpoints(this WebApplication app)
    {
        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();
        app.MapGrpcService<KeyValueDbGrpcService>();
    }
    
    public static void RunApplication(this WebApplication app)
    {
        try
        {
            app.Logger.LogInformation("Starting web host ({ApplicationName})...", ApplicationName);
            app.Run();
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical(ex, "Host terminated unexpectedly ({ApplicationName})...", ApplicationName);
        }
        finally
        {
            Serilog.Log.CloseAndFlush();
        }
    }
}