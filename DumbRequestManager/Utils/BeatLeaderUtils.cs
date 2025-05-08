using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SiraUtil.Web;
using Zenject;

namespace DumbRequestManager.Utils;

/*
[JsonObject(MemberSerialization.OptIn)]
internal readonly struct BeatLeaderStarsObject
{
    [JsonProperty("stars")]
    public readonly float Stars;

    [JsonProperty("rankedTime")]
    public readonly int RankedTime;
    
    [JsonProperty("difficultyName")]
    public readonly string RawDifficultyName;
    
    [JsonProperty("modeName")]
    public readonly string RawCharacteristicName;
    
    public string DifficultyName => Normalize.GetDifficultyName(RawDifficultyName);
    public string CharacteristicName => Normalize.GetCharacteristicName(RawCharacteristicName);
    public bool IsRanked => RankedTime != 0;
}

internal struct BeatLeaderObject
{
    [JsonProperty("difficulties")]
    public readonly BeatLeaderStarsObject[] Difficulties = [];

    public BeatLeaderObject(string jsonData)
    {
        JsonConvert.DeserializeObject<BeatLeaderObject>(data)
    }
}
*/
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
    private const string BeatLeaderAPI = "https://api.beatleader.com/map/hash/{0}";
    internal static BeatLeaderUtils Instance = null!;
    private IHttpService HttpService => httpService;
    
    public void Initialize()
    {
        Instance = this;
        
        MethodInfo? timeoutMethod = httpService.GetType().GetMethod("set_Timeout");
        
        if (timeoutMethod == null)
        {
            Plugin.Log.Info("[BeatLeaderUtils] Timeout value cannot be set, doesn't exist in SiraUtil yet.");
        }
        else
        {
            timeoutMethod.Invoke(httpService, [5]);
            Plugin.Log.Info($"[BeatLeaderUtils] Set timeout value to 5 seconds.");
        }
    }

    public async Task<List<BeatLeaderDifficulty>?> GetStarValueForHash(string hash, CancellationToken token = default)
    {
        // guh
        IHttpResponse response = await Instance.HttpService.GetAsync(string.Format(BeatLeaderAPI, hash));
        token.ThrowIfCancellationRequested();
        
        if (!response.Successful)
        {
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
        
        return beatLeaderObject?.Difficulties;
    }
}