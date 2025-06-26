using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SiraUtil.Web;
using Zenject;

namespace DumbRequestManager.Utils;

[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class BeatLeaderDifficulty
{ 
    [JsonProperty("difficultyName")] private string? _rawDifficultyName = null;
    public string DifficultyName => Normalize.GetDifficultyName(_rawDifficultyName ?? "ExpertPlus");
    
    [JsonProperty("modeName")] private string? _rawCharacteristicName = null;
    public string CharacteristicName => Normalize.GetCharacteristicName(_rawCharacteristicName ?? "Standard");
    
    [JsonProperty("status")] 
    public int? Status;
    
    [JsonProperty("stars")] 
    public double? Stars;
}

public class BeatLeaderObject
{ 
    [JsonProperty("difficulties")] 
    public List<BeatLeaderDifficulty>? Difficulties;
}

[UsedImplicitly]
internal class BeatLeaderUtils(IHttpService httpService) : IInitializable
{
    private static readonly string CachePathRoot = Path.Combine(Plugin.UserDataDir, "Cache");
    private static readonly string CachePath = Path.Combine(CachePathRoot, "BeatLeader");
    private const string BeatLeaderAPI = "https://api.beatleader.com/map/hash/{0}";
    private static BeatLeaderUtils _instance = null!;
    private IHttpService HttpService => httpService;
    
    public void Initialize()
    {
        _instance = this;
    }

    public static async Task<List<BeatLeaderDifficulty>?> GetStarValueForHash(string hash, CancellationToken token = default)
    {
        if (!Directory.Exists(CachePathRoot))
        {
            Directory.CreateDirectory(CachePathRoot);
        }
        if (!Directory.Exists(CachePath))
        {
            Directory.CreateDirectory(CachePath);
        }

        string filename = Path.Combine(CachePath, $"{hash}.json");

        if (File.Exists(filename))
        {
            Plugin.DebugMessage("[BeatLeaderUtils] Trying cached data first...");
            
            bool failed = false;
            BeatLeaderObject? cachedBeatLeaderObject = null;
            try
            {
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation (synchronous is fine)
                cachedBeatLeaderObject = JsonConvert.DeserializeObject<BeatLeaderObject>(File.ReadAllText(Path.Combine(CachePath, $"{hash}.json")));
            }
            catch (Exception exception)
            {
                Plugin.Log.Warn($"[BeatLeaderUtils] Failed to parse cached BeatLeader data: {exception}");
                Plugin.Log.Warn("[BeatLeaderUtils] Refreshing...");
                failed = true;
            }

            if (!failed)
            {
                return cachedBeatLeaderObject?.Difficulties;
            }
        }
        
        IHttpResponse response = await _instance.HttpService.GetAsync(string.Format(BeatLeaderAPI, hash));
        token.ThrowIfCancellationRequested();
        
        if (!response.Successful)
        {
            Plugin.Log.Warn($"[BeatLeaderUtils] Failed to parse cached BeatLeader data, HTTP {response.Code}");
            return [];
        }
        
        string data = response.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        token.ThrowIfCancellationRequested();
        
        Plugin.DebugMessage("Got BeatLeader data");

        BeatLeaderObject? beatLeaderObject = null;
        try
        {
            beatLeaderObject = JsonConvert.DeserializeObject<BeatLeaderObject>(data);
        }
        catch (Exception exception)
        {
            Plugin.Log.Warn($"[BeatLeaderUtils] Failed to parse BeatLeader data: {exception}");
        }

        await File.WriteAllTextAsync(filename, data, token);
        
        return beatLeaderObject?.Difficulties;
    }
}