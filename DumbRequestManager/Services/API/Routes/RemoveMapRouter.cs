using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DumbRequestManager.Managers;
using DumbRequestManager.Services.API.Models;
using Newtonsoft.Json;

namespace DumbRequestManager.Services.API.Routes;

public class RemoveMapRouter
{
    public static async Task<APIResponse> HandleContext(HttpListenerContext context)
    {
        var res =  new APIResponse();
        
        var route = context.Request.Url.Segments;
        NameValueCollection urlQuery = System.Web.HttpUtility.ParseQueryString(context.Request.Url.Query);

        if (route.Length <= 2 || !int.TryParse(route.Last().Replace("/", string.Empty).ToLower(), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out int _))
        {
            return res;
        }
        
        string? keyRes = await RemoveKey(route.Last().Replace("/", string.Empty));

        if (keyRes != null)
        {
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Payload = keyRes;
        }
        else
        {
            res.StatusCode = (int)HttpStatusCode.BadRequest;
            res.Payload = APIResponse.APIMessage("Map not in queue.");
        }
        return res;
    }

    private static async Task<string?> RemoveKey(string key)
    {
        Plugin.Log.Info($"Removing key {key} from queue...");
        bool keyRemoved = await QueueManager.RemoveKey(key, out var song);

        return keyRemoved && song != null ? JsonConvert.SerializeObject(song) : null;
    }
}