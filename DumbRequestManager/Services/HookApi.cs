using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DumbRequestManager.Configuration;
using Newtonsoft.Json;

namespace DumbRequestManager.Services;

[JsonObject(MemberSerialization.OptIn)]
internal readonly struct HookObject(string eventName, object? data)
{
    [JsonProperty("timestamp")] private long Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    [JsonProperty("id")] private readonly string _id = Guid.NewGuid().ToString();
    [JsonProperty("event")] private string Event => eventName;
    [JsonProperty("data")] private object? Data => data;
}

internal abstract class HookApi
{
    private static PluginConfig Config => PluginConfig.Instance;
    private static string HookUrl => Config.WebHookUrl;
    
    private static readonly HttpClient HookClient = new();

    internal static async Task TriggerHook(string eventName, object? data = null)
    {
        if (HookUrl == string.Empty)
        {
            Plugin.DebugMessage($"Not sending out Webhook event {eventName}, no Webhook URL");
            return;
        }

        if (Uri.TryCreate(HookUrl, UriKind.Absolute, out Uri uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            Dictionary<string, string> formValues = new Dictionary<string, string>
            {
                { "data", JsonConvert.SerializeObject(new HookObject(eventName, data)) }
            };
            FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(formValues);
            
            HttpResponseMessage response = await HookClient.PostAsync(uri, formUrlEncodedContent);

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.Warn($"Error sending Webhook event {eventName}, HTTP {response.StatusCode}");
            }
        }
        else
        {
            Plugin.Log.Warn($"{HookUrl} is not a valid Webhook URL");
        }
    }
}