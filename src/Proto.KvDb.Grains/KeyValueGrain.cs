using Microsoft.Extensions.Logging;
using Proto.Cluster;

namespace Proto.KvDb.Grains;

public class KeyValueGrain : KeyValueGrainBase
{
    private readonly ILogger<KeyValueGrain> _logger;
    private string Key { get; } 
    private Dictionary<string, string> ValueDictionary { get; set; } 
    private bool KillActorRequest { get; set; }

    private const string DefaultKey = "^~";

    public KeyValueGrain(
        IContext context,
        ILogger<KeyValueGrain> logger)
        : base(context)
    {
        _logger = logger;
        
        Key = context.ClusterIdentity()?.Identity;
        ValueDictionary = new Dictionary<string, string>();
        KillActorRequest = false;
    }

    public override Task<GetMessageResponse> Get()
    {
        if (KillActorRequest)
            return Task.FromResult(new GetMessageResponse()
            {
                Success = false,
                ErrorDescription = "Key not found" 
            });

        var success = ValueDictionary.TryGetValue(DefaultKey, out var value);
        return Task.FromResult(new GetMessageResponse()
        {
            Success = success,
            ErrorDescription = success ? null : "Key not found",
            Value = value
        });
    }

    public override Task<SetMessageResponse> Set(SetMessageRequest request)
    {
        var result = new SetMessageResponse();

        if (request == null || KillActorRequest)
        {
            result.Success = false;
            result.ErrorDescription = "Invalid request or key not found";
        }
        else
        {
            if (ValueDictionary.ContainsKey(DefaultKey))
            {
                ValueDictionary[DefaultKey] = request.Value;
                result.Success = true;
            }
            else
            {
                var success = ValueDictionary.TryAdd(DefaultKey, request.Value);
                result.Success = success;
                result.ErrorDescription = success ? null : "Fail to add the value";
            }
        }

        return Task.FromResult(result);
    }

    public override Task<DelMessageResponse> Del()
    {
        ValueDictionary.Clear();
        ValueDictionary = null;
        KillActorRequest = true;
        
        Context.Send(Context.Self, new PoisonPill());

        return Task.FromResult(new DelMessageResponse()
        {
            Success = true
        });
    }

    // -------------------------------------------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------------------------------------------
    
    public override Task<HGetMessageResponse> HGet(HGetMessageRequest request)
    {
        if (request == null || KillActorRequest)
        {
            return Task.FromResult(new HGetMessageResponse()
            {
                Success = false,
                ErrorDescription = "Invalid request or key not found"
            });
        }
        
        var result = new HGetMessageResponse
        {
            Success = true
        };

        if(request.Keys.Count == 0)
            return Task.FromResult(result);

        foreach (var key in request.Keys)
        {
            var success = ValueDictionary.TryGetValue(key, out var value);
            result.Values.Add(new HGetMessageResponse.Types.HGetKeyResult()
            {
                Success = success,
                ErrorDescription = success ? null : "Key not found",
                Key = key,
                Value = value
            });
        }
        
        return Task.FromResult(result);
    }

    public override Task<HSetMessageResponse> HSet(HSetMessageRequest request)
    {
        if (request == null || KillActorRequest)
        {
            return Task.FromResult(new HSetMessageResponse()
            {
                Success = false,
                ErrorDescription = "Invalid request or key not found"
            });
        }

        var result = new HSetMessageResponse
        {
            Success = true
        };
        
        if(request.KeysValues.Count == 0)
            return Task.FromResult(result);

        foreach (var keyValue in request.KeysValues)
        {
            var key = keyValue.Key;
            var value = keyValue.Value;
            
            var keyValueInsertResult = new HSetMessageResponse.Types.HSetKeyResult();
            
            if (ValueDictionary.ContainsKey(key))
            {
                ValueDictionary[DefaultKey] = value;
                keyValueInsertResult.Success = true;
            }
            else
            {
                var success = ValueDictionary.TryAdd(key, value);
                keyValueInsertResult.Success = success;
                keyValueInsertResult.ErrorDescription = success ? null : "Fail to add the value";
            }
            
            result.Values.Add(keyValueInsertResult);
        }

        return Task.FromResult(result);
    }

    public override Task<HDelMessageResponse> HDel(HDelMessageRequest request)
    {
        throw new NotImplementedException();
    }
}