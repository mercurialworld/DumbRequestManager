using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using DumbRequestManager.Services.API.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DumbRequestManager.Services.API.Routes;

[UsedImplicitly]
public class QueryRouter
{
    public static async Task<APIResponse> HandleContext(HttpListenerContext context)
    {
        var res = new APIResponse();
        var route = context.Request.Url.Segments;

        if (!int.TryParse(route.Last().Replace("/", String.Empty).ToLower(),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return res;
        }
        
        string? queryResponse = route[2][..^1].ToLower() == "nocache"
            ? await QuerySkipCache(route.Last().Replace("/", string.Empty))
            : await Query(route.Last().Replace("/", string.Empty));

        if (queryResponse == null)
        {
            res.StatusCode = (int)HttpStatusCode.NotFound;
            res.Payload = APIResponse.APIMessage("Could not find map.");
        } 
        else 
        {
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Payload = queryResponse;
        }
        
        return res;
    }
    
    private static async Task<string?> QuerySkipCache(string key)
    {
        Plugin.Log.Info($"Querying key {key} (directly)...");
        
        Beatmap? beatmap = await SongDetailsManager.GetDirectByKey(key);
        if (beatmap != null)
        {
            return JsonConvert.SerializeObject(new NoncontextualizedSong(beatmap));
        }

        Plugin.Log.Info($"No beatmap found for key {key}");
        return null;
    }

    private static async Task<string?> Query(string key)
    {
        Plugin.Log.Info($"Querying key {key}...");

        NoncontextualizedSong queriedSong;
        
        CachedMap? song = SongDetailsManager.GetByKey(key);
        if (song == null)
        {
            Beatmap? beatmap = await SongDetailsManager.GetDirectByKey(key); 
            if (beatmap != null)
            {
                Plugin.DebugMessage("Using BeatSaverSharp method");
                queriedSong = new NoncontextualizedSong(beatmap);
            }
            else
            {
                Plugin.Log.Info("oh we're screwed");
                return null;
            }
        }
        else
        {
            string hash = song.Hash.ToLower();
            Plugin.DebugMessage($"Checking SongCore for hash {hash}");
            
            BeatmapLevel? beatmapLevel = SongCore.Loader.GetLevelByHash(hash);
            if (beatmapLevel != null)
            {
                Plugin.DebugMessage("Using local map method");
                queriedSong = new NoncontextualizedSong(beatmapLevel);
            }
            else
            {
                Plugin.DebugMessage("Using protobuf cache method");
                queriedSong = new NoncontextualizedSong(song);
            }
        }

        return JsonConvert.SerializeObject(queriedSong);
    }
}