using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DumbRequestManager.Managers;
using DumbRequestManager.Services.API.Models;
using DumbRequestManager.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DumbRequestManager.Services.API.Routes;

[UsedImplicitly]
public class AddMessageRouter
{
    public static async Task<APIResponse> HandleContext(HttpListenerContext context)
    {
        var res =  new APIResponse();
        
        var route = context.Request.Url.Segments;

        if (route.Length <= 2 || !int.TryParse(route.Last().Replace("/", string.Empty).ToLower(), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out int _))
        {
            return res;
        }
        
        if (!context.Request.HttpMethod.Equals("POST"))
        {
            res.Payload = APIResponse.APIMessage("Please use a POST request for this endpoint.");
            return res;
        }
        
        using StreamReader reader = new(context.Request.InputStream, context.Request.ContentEncoding);
        var reqMessage = await reader.ReadToEndAsync();
        
        if (String.IsNullOrEmpty(reqMessage))
        {
            res.Payload = APIResponse.APIMessage("Please add a plain text body with the POST request.");
            return res;
        }
        
        string? addMsgRes = await AddMessage(route.Last().Replace("/", string.Empty), reqMessage.StripTMPTags());

        if (addMsgRes != null)
        {
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Payload = addMsgRes;
        }
        else
        {
            res.StatusCode = (int)HttpStatusCode.NotFound;
            res.Payload = APIResponse.APIMessage("Map not found in the queue.");
        }
        return res;
    }

    private static async Task<string?> AddMessage(string key, string message)
    {
        Plugin.Log.Info($"Adding message for map ${key}...");
        bool messageAdded = await QueueManager.EditMessageByKey(key, message, out var song);
        
        return messageAdded && song != null ? JsonConvert.SerializeObject(song) : null;
    }
}