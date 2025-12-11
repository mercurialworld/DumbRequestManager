using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DumbRequestManager.Services.API.Models;

[JsonObject]
public class APIMessage(string eventName, object? data)
{
    public long Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    // [TODO] aw fuck this is a breaking change
    public string Event = eventName;
    public object? Data = data;

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}

[JsonObject]
public class HookMessage(string eventName, object? data) : APIMessage(eventName, data)
{
    [JsonProperty("id")] private string _id = Guid.NewGuid().ToString();

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings{
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
    }
}