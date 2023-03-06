using Grpc.Core;
using Proto.Cluster;
using Proto.KvDb.Grains;
using Proto.KvDb.GrpcService;

namespace Proto.KvDb.API.GrpcService;

public class KeyValueDbGrpcService : KeyValueDbService.KeyValueDbServiceBase
{
    private readonly ActorSystem _actorSystem;
    private readonly ILogger<KeyValueDbGrpcService> _logger;

    public KeyValueDbGrpcService(
        ActorSystem actorSystem,
        ILogger<KeyValueDbGrpcService> logger)
    {
        _actorSystem = actorSystem;
        _logger = logger;
    }

    public override async Task<GetMessageResponse> Get(GetRequest request, ServerCallContext context)
    {
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(request.Key)
            .Get(CancellationTokens.WithTimeout(TimeSpan.FromSeconds(1)));
    }

    public override async Task<SetMessageResponse> Set(SetRequest request, ServerCallContext context)
    {
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(request.Key)
            .Set(new SetMessageRequest()
            {
                Value = request.Value
            },CancellationTokens.WithTimeout(TimeSpan.FromSeconds(1)));
    }
}