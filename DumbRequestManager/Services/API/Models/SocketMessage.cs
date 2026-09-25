using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DumbRequestManager.Services.API.Models;

internal class WebsocketContractResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        // how to maintain backwards compatibility with empoleon
        var baseProperties = base.CreateProperties(type, memberSerialization);

        foreach (var property in baseProperties)
        {
            if (property.PropertyName is not "Event") continue;
            
            property.PropertyName = property.PropertyName.Replace("Event", "EventType");
            break;
        }
        return baseProperties;
    }
}

internal class MIUHookContractResolver : DefaultContractResolver
{
    // this is fucking insane empoleon
    private readonly string[] _hackyProperties = ["Timestamp", "Event", "Data"];
    
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        // only the level 1 object stuff needs to be lowercase because Mix It Up
        var baseProperties = base.CreateProperties(type, memberSerialization);
        
        // god damnit
        foreach (var property in baseProperties)
        {
            if (property.PropertyName is not null && _hackyProperties.Contains(property.PropertyName))
            {
                property.PropertyName = property.PropertyName.ToLower();
            }
        }
        
        return baseProperties;
    }
}

[JsonObject]
public class SocketMessage(string eventName, object? data)
{
    public long Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    // [TODO] try to make this not a breaking change if you can help it
    public string Event = eventName;
    public object? Data = data;

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings{
            ContractResolver = new WebsocketContractResolver()
        });
    }
}

[JsonObject]
public class HookMessage(string eventName, object? data) : SocketMessage(eventName, data)
{
    [JsonProperty("id")] private string _id = Guid.NewGuid().ToString();

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings{
            ContractResolver = new MIUHookContractResolver()
        });
    }
}