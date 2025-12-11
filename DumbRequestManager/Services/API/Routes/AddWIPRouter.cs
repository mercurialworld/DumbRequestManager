using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using DumbRequestManager.Services.API.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PluginConfig = DumbRequestManager.Configuration.PluginConfig;

namespace DumbRequestManager.Services.API.Routes;

[UsedImplicitly]
// ReSharper disable once InconsistentNaming (screw this)
public class AddWIPRouter
{
    private static PluginConfig Config => PluginConfig.Instance;
    // [TODO] should we just move everything out of sirautil's HttpService???? idk
    private static readonly HttpClient ClientOnlyUsedForHeadRequests = new HttpClient();
        
    
    public static async Task<APIResponse> HandleContext(HttpListenerContext context)
    {
        var res = new APIResponse();
        
        if (!ClientOnlyUsedForHeadRequests.DefaultRequestHeaders.Contains("User-Agent"))
        {
            ClientOnlyUsedForHeadRequests.DefaultRequestHeaders.Add("User-Agent",
                $"DumbRequestManager/{Plugin.PluginVersion} (https://github.com/mercurialworld/DumbRequestManager)");
        }

        if (!context.Request.HttpMethod.Equals("POST"))
        {
            res.Payload = APIResponse.APIMessage("Please use a POST request for this endpoint.");
            return res;
        }
        
        NameValueCollection urlQuery = System.Web.HttpUtility.ParseQueryString(context.Request.Url.Query);
        using StreamReader reader = new(context.Request.InputStream, context.Request.ContentEncoding);
        var maybeLink = await reader.ReadToEndAsync();

        if (String.IsNullOrEmpty(maybeLink))
        {
            res.Payload = APIResponse.APIMessage("Please add a plain text body with the POST request.");
            return res;
        }
        
        string? finalURL = ParseWipInput(maybeLink, ref res);
        if (finalURL == null)
        {
            return res;
        }

        long? wipSize = await FetchWipContentLength(finalURL);
        if (wipSize is null or 0)
        {
            res.Payload = APIResponse.APIMessage("WIP file is empty");
        } 
        else if (wipSize >= Config.MaximumWipSize  * 1048576)
        {
            res.Payload = APIResponse.APIMessage("WIP file exceeds maximum file size");
        }
        else
        {
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Payload = AddWip(finalURL, 
                urlQuery.Get("user"), 
                bool.Parse(urlQuery.Get("prepend") ?? "true"),
                urlQuery.Get("service"));
        }

        return res;
    }

    private static string? ParseWipInput(string input, ref APIResponse res)
    {
        var url = input;
        
        if (int.TryParse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            var urlSuffix = $"/wips/{input}.zip";
            
            switch (input[0])
            {
                case '0':
                    url = "https://wipbot.com" + urlSuffix;
                    break;
                case '8':
                case '9':
                    // both point to the same backend, so there's less work for me to do on my end
                    // https://github.com/mercurialworld/DumbRequestManager/issues/9#issuecomment-3605892011
                    url = "https://wip.hawk.quest" + urlSuffix;
                    break;
                default:
                    res.Payload = APIResponse.APIMessage("Invalid WIP code.");
                    return null;
            }
        } 
        
        bool isValidURL = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) 
                          && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        if (!isValidURL) return null;
        
        string[] domainParts = uriResult.DnsSafeHost.Split(".");
        string domain = $"{domainParts[^2]}.{domainParts[^1]}";
        if (!Config.WhitelistedWipDomains.Contains(domain))
        {
            res.Payload = APIResponse.APIMessage(
                $"WIP URL host domain ({domain}) is not whitelisted, please check your configuration file");
            return null;
        }
        
        // some file hosts navigate you to a page *to* initiate the download, rather than initiating it immediately.
        // lazy way of handling that
        url = url.Replace("?dl=0", "?dl=1");
        url = url.Replace("&dl=0", "&dl=1");

        return url;
    }

    private static async Task<long?> FetchWipContentLength(string url)
    {
        HttpResponseMessage? headResponseMessage = null;
        try
        {
            headResponseMessage = await ClientOnlyUsedForHeadRequests.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
        }
        catch (HttpRequestException)
        {
            try
            {
                Plugin.Log.Warn($"Server does not support HEAD requests, using a GET request instead");
                headResponseMessage = await ClientOnlyUsedForHeadRequests.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            }
            catch (HttpRequestException)
            {
                Plugin.Log.Warn($"Server appears to be unreachable");
                return null;
            }
        }

        return headResponseMessage?.Content.Headers.ContentLength;
    }
    
    private static string AddWip(string url, string? user = null, bool prepend = true, string? service = null)
    {
        Plugin.Log.Info($"Adding wip {url}...");
        NoncontextualizedSong queuedSong = QueueManager.AddWip(url, user, prepend, service);
        
        return JsonConvert.SerializeObject(queuedSong);
    }
}