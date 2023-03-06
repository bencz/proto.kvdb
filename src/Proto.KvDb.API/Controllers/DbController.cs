using Microsoft.AspNetCore.Mvc;
using Proto.Cluster;
using Proto.KvDb.Grains;
using Proto.KvDb.GrpcService;

namespace Proto.KvDb.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DbController : ControllerBase
{
    private readonly ActorSystem _actorSystem;
    private readonly ILogger<DbController> _logger;

    public DbController(
        ActorSystem actorSystem,
        ILogger<DbController> logger)
    {
        _actorSystem = actorSystem;
        _logger = logger;
    }

    [HttpGet("get")]
    public async Task<GetMessageResponse> Get(string key)
    {
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(key)
            .Get(CancellationTokens.WithTimeout(TimeSpan.FromSeconds(1)));
    }

    [HttpPost("set")]
    public async Task<SetMessageResponse> Set(SetRequest request)
    {
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(request.Key)
            .Set(new SetMessageRequest()
            {
                Value = request.Value
            },CancellationTokens.WithTimeout(TimeSpan.FromSeconds(1)));
    }

    [HttpDelete("del")]
    public async Task<DelMessageResponse> Del(string key)
    {
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(key)
            .Del(CancellationTokens.WithTimeout(TimeSpan.FromSeconds(1)));
    }
    
    // -------------------------------------------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------------------------------------------
    
    [HttpGet("hget")]
    public async Task<HGetMessageResponse> HGet(string key, string hkeys)
    {
        if(string.IsNullOrEmpty(key))
            return new HGetMessageResponse()
            {
                Success = false,
                ErrorDescription = "invalid key"
            };
        
        if (string.IsNullOrEmpty(hkeys))
            return new HGetMessageResponse()
            {
                Success = false,
                ErrorDescription = "invalid hash keys"
            };
        
        var grainRequest = new HGetMessageRequest();
        foreach (var hkey in hkeys.Split(',').Select(x => x.Trim()))
        {
            grainRequest.Keys.Add(hkey);
        }
        
        return await _actorSystem
            .Cluster()
            .GetKeyValueGrain(key)
            .HGet(grainRequest,CancellationTokens.WithTimeout(TimeSpan.FromSeconds(1)));
    }
    
    [HttpPost("hset")]
    public async Task<HSetMessageResponse> HSet(HSetRequest request)
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
            .HSet(grainRequest,CancellationTokens.WithTimeout(TimeSpan.FromSeconds(1)));
    }
}