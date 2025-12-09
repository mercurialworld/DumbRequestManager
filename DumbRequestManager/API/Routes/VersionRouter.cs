using System.Net;
using System.Threading.Tasks;
using DumbRequestManager.API.Models;
using IPA.Utilities;
using Newtonsoft.Json;

namespace DumbRequestManager.API.Routes;

[JsonObject(MemberSerialization.OptIn)]
internal class VersionOutput
{
    [JsonProperty] internal static string GameVersion => UnityGame.GameVersion.ToString();
    [JsonProperty] internal static string ModVersion => Plugin.PluginVersion.ToString();
}

public class VersionRouter
{
    public static Task<APIResponse> HandleContext()
    {
        var res = new APIResponse(
            HttpStatusCode.OK,
            JsonConvert.SerializeObject(new VersionOutput()));

        return Task.FromResult(res);
    }
}