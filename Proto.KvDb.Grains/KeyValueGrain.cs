using Microsoft.Extensions.Logging;
using Proto.Cluster;

namespace Proto.KvDb.Grains;

public class KeyValueGrain : KeyValueGrainBase
{
    private readonly ILogger<KeyValueGrain> _logger;
    private string Key { get; } 
    private string Value { get; set; } 
    private bool DeadKey { get; set; }
    
    public KeyValueGrain(
        IContext context,
        ILogger<KeyValueGrain> logger)
        : base(context)
    {
        _logger = logger;
        
        Key = context.ClusterIdentity()?.Identity;
        Value = null;
        DeadKey = false;
    }

    public override Task<GetMessageResponse> Get()
    {
        if (DeadKey == true)
            return Task.FromResult(new GetMessageResponse()
            {
                Success = false,
                ErrorDescription = "Key not found" 
            });

        return Task.FromResult(new GetMessageResponse()
        {
            Success = true,
            Value = Value
        });
    }

    public override Task<SetMessageResponse> Set(SetMessageRequest request)
    {
        var result = new SetMessageResponse();

        if (request == null || DeadKey == true)
        {
            result.Success = false;
            result.ErrorDescription = "Invalid request or key not found";
        }
        else
        {
            Value = request.Value;
            result.Success = true;
        }

        return Task.FromResult(result);
    }

    public override Task<DelMessageResponse> Del()
    {
        Value = null;
        DeadKey = true;
        
        Context.Send(Context.Self, new PoisonPill());

        return Task.FromResult(new DelMessageResponse()
        {
            Success = true
        });
    }
}