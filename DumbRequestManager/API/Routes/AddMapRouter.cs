using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DumbRequestManager.API.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DumbRequestManager.API.Routes;

[UsedImplicitly]
public class AddMapRouter
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
        
        string? keyRes = await AddKey(route.Last().Replace("/", string.Empty),
            urlQuery.Get("user"),
            bool.Parse(urlQuery.Get("prepend") ?? "false"),
            urlQuery.Get("service"));

        if (keyRes != null)
        {
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Message = keyRes;
        }
        else
        {
            res.StatusCode = (int)HttpStatusCode.BadGateway;
            res.Message = "Map not found on BeatSaver.";
        }
        return res;
    }
    
    private static async Task<string?> AddKey(string key, string? user = null, bool prepend = false, string? service = null)
    {
        Plugin.Log.Info($"Adding key {key}...");
        NoncontextualizedSong? queuedSong = await QueueManager.AddKey(key, user, false, prepend, service);
        
        return queuedSong == null ? null : JsonConvert.SerializeObject(queuedSong);
    }
}