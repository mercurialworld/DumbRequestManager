using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using DumbRequestManager.Services.API.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DumbRequestManager.Services.API.Routes;

[JsonObject(MemberSerialization.OptIn)]
internal struct HistorySpotItem(long timestamp, NoncontextualizedSong historyItem)
{
    [JsonProperty] private int Timestamp => (int)timestamp;
    [JsonProperty] private NoncontextualizedSong HistoryItem => historyItem;
}

[UsedImplicitly]
public class HistoryRouter
{   
    // [TODO] move this to a utilclass?
    private class TimestampComparer : IComparer
    {
        private readonly CaseInsensitiveComparer _comparer = new();
        public int Compare(object? x, object? y)
        {
            return _comparer.Compare(y, x);
        }
    }
    private static readonly TimestampComparer TimestampComparerInstance = new();
    
    public static Task<APIResponse> HandleContext(HttpListenerContext context)
    {
        var res = new APIResponse();
        var route = context.Request.Url.Segments;
        NameValueCollection urlQuery = System.Web.HttpUtility.ParseQueryString(context.Request.Url.Query);
        
        res.StatusCode = (int)HttpStatusCode.OK;
        res.Payload = GetSessionHistory(int.Parse(urlQuery.Get("limit") ?? "0"));

        return Task.FromResult(res);
    }
    
    private static string GetSessionHistory(int limit = 0)
    {
        long[] keys = SessionHistoryManager.SessionHistory.Keys.ToArray();
        Array.Sort(keys, TimestampComparerInstance);
        if (limit > 0)
        {
            limit = Math.Min(keys.Length, limit);
            keys = keys[0..limit];
        }

        HistorySpotItem[] historyItems = keys.Select(timestamp => new HistorySpotItem(timestamp, SessionHistoryManager.SessionHistory[timestamp])).ToArray();
        
        return JsonConvert.SerializeObject(historyItems);
    }
}