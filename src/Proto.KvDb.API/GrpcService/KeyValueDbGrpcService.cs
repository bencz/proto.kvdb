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
            .Get(CancellationTokens.FromSeconds(TimeSpan.FromSeconds(1)));
    }

    public override async Task<SetMessageResponse> Set(SetRequest request, ServerCallContext context)
    {
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(request.Key)
            .Set(new SetMessageRequest()
            {
                Value = request.Value
            },CancellationTokens.FromSeconds(TimeSpan.FromSeconds(1)));
    }

    public override async Task<DelMessageResponse> Del(DelRequest request, ServerCallContext context)
    {
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(request.Key)
            .Del(CancellationTokens.FromSeconds(TimeSpan.FromSeconds(1)));
    }

    public override async Task<HGetMessageResponse> HGet(HGetRequest request, ServerCallContext context)
    {
        var grainRequest = new HGetMessageRequest();
        foreach (var getKey in request.HashKeys) 
            grainRequest.Keys.Add(getKey);

        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(request.Key)
            .HGet(grainRequest, CancellationTokens.FromSeconds(TimeSpan.FromSeconds(1)));
    }

    public override async Task<HSetMessageResponse> HSet(HSetRequest request, ServerCallContext context)
    {
        var grainRequest = new HSetMessageRequest();
        foreach (var hashKeyValue in request.HashKeysValues)
        {
            grainRequest.KeysValues.Add(new HSetMessageRequest.Types.HSetKeyValue()
            {
                Key = hashKeyValue.HashKey,
                Value = hashKeyValue.HashValue
            });
        }
        
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(request.Key)
            .HSet(grainRequest, CancellationTokens.FromSeconds(TimeSpan.FromSeconds(1)));
    }

    public override async Task<HDelMessageResponse> HDel(HDelRequest request, ServerCallContext context)
    {
        var grainRequest = new HDelMessageRequest();
        foreach (var getKey in request.HashKeys) 
            grainRequest.Keys.Add(getKey);

        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(request.Key)
            .HDel(grainRequest, CancellationTokens.FromSeconds(TimeSpan.FromSeconds(1)));
    }
}