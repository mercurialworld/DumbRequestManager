using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using DumbRequestManager.Managers;
using DumbRequestManager.Services.API.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DumbRequestManager.Services.API.Routes;

[UsedImplicitly]
public class BlacklistRouter
{
    public static Task<APIResponse> HandleContext(HttpListenerContext context)
    {
        var res = new APIResponse();
        var route = context.Request.Url.Segments;

        if (route.Length <= 2)
        {
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Payload = JsonConvert.SerializeObject(BlacklistManager.BlacklistedIDs);
        }
        else
        {
            var blacklistKey = route[3].Replace("/", string.Empty);
            if (!long.TryParse(
                    blacklistKey, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out _))
            {
                res.Payload = APIResponse.APIMessage("Invalid key");
                return Task.FromResult(res);
            }

            if (route[2].Replace("/", string.Empty).ToLower().Equals("add"))
            {
                BlacklistManager.AddKey(blacklistKey);
                res.Payload = APIResponse.APIMessage($"Blacklisted key {blacklistKey}");
            }
            else
            {
                BlacklistManager.RemoveKey(blacklistKey);
                res.Payload = APIResponse.APIMessage($"Removed key {blacklistKey} from blacklist");
            }

            res.StatusCode = (int)HttpStatusCode.OK;
        }
        return Task.FromResult(res);
    }
}