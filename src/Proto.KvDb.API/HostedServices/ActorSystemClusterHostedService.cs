using System.Diagnostics;
using Proto.Cluster;

namespace Proto.KvDb.API.HostedServices;

public class ActorSystemClusterHostedService : IHostedService
{
    private readonly ActorSystem _actorSystem;
    private readonly ILogger<ActorSystemClusterHostedService> _logger;

    public ActorSystemClusterHostedService(
        ActorSystem actorSystem,
        ILogger<ActorSystemClusterHostedService> logger)
    {
        _actorSystem = actorSystem;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting a proto.actor cluster member");

        var sw = new Stopwatch();
        sw.Start();
        await _actorSystem
            .Cluster()
            .StartMemberAsync();
        sw.Stop();
        
        _logger.LogInformation($"Proto.actor cluster member started: {sw.Elapsed.TotalSeconds} seconds");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down a proto.actor cluster member");
        
        var sw = new Stopwatch();
        sw.Start();
        await _actorSystem
            .Cluster()
            .ShutdownAsync();
        sw.Stop();
    }
}