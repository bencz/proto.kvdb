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
}