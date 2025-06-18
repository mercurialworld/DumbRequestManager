using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DumbRequestManager.Classes;
using DumbRequestManager.Configuration;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using ProtoBuf;
using SiraUtil.Web;
using Zenject;
using InvalidOperationException = System.InvalidOperationException;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
internal class MapCacheManager(IHttpService httpService) : IInitializable
{
    private static PluginConfig Config => PluginConfig.Instance;
    private static readonly Dictionary<dynamic, CachedMap> CachedMaps = new();

    private static readonly string CacheFilename = Path.Combine(Plugin.UserDataDir, "cache.proto.gz");
    private static readonly string BeatLeaderCacheFilename = Path.Combine(Plugin.UserDataDir, "blcache.proto.gz");
    private static readonly string CacheURL = Config.ProtobufCacheURL;
    private const string BeatLeaderCacheURL = "https://api.beatleader.com/map/allstars";

    internal static MapCacheManager? Instance;
    
    public void Initialize()
    {
        Instance = this;

        UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            await SetupCacheStuff();
            await SetupBeatLeaderCacheStuff();
        });
    }

    private async Task SetupCacheStuff()
    {
        if (File.Exists(CacheFilename))
        {
            if (DateTime.UtcNow < File.GetLastWriteTimeUtc(CacheFilename).AddHours(1))
            {
                Plugin.Log.Info("Cache was obtained less than an hour ago, not refreshing");
                await LoadProtoCache();
                return;
            }
        }
        
        Plugin.Log.Info($"Grabbing the cache from {CacheURL}...");
        IHttpResponse response = await httpService.GetAsync(CacheURL).ConfigureAwait(false);
        
        if (!response.Successful)
        {
            Plugin.Log.Warn($"Couldn't download the cache (HTTP {response.Code})");
            await LoadProtoCache();
            return;
        }
        
        Plugin.Log.Info($"Grabbed cache from {CacheURL}, saving...");

        await File.WriteAllBytesAsync(CacheFilename, await response.ReadAsByteArrayAsync());
        
        Plugin.Log.Info($"Saved cache to {CacheFilename}");
        
        await LoadProtoCache();
    }

    private async Task SetupBeatLeaderCacheStuff()
    {
        if (File.Exists(BeatLeaderCacheFilename))
        {
            if (DateTime.UtcNow < File.GetLastWriteTimeUtc(CacheFilename).AddDays(1))
            {
                Plugin.Log.Info("BeatLeader cache was obtained less than 24 hours ago, not refreshing");
                await LoadBeatLeaderCache();
                return;
            }
        }
        
        Plugin.Log.Info($"Grabbing the BeatLeader star value cache from {BeatLeaderCacheURL}...");
        IHttpResponse response = await httpService.GetAsync(BeatLeaderCacheURL).ConfigureAwait(false);
        
        if (!response.Successful)
        {
            Plugin.Log.Warn($"Couldn't download the BeatLeader star value cache (HTTP {response.Code})");
            await LoadBeatLeaderCache();
            return;
        }
        
        Plugin.Log.Info($"Grabbed cache from {BeatLeaderCacheURL}, saving...");

        await File.WriteAllBytesAsync(BeatLeaderCacheFilename, await response.ReadAsByteArrayAsync());
        
        Plugin.Log.Info($"Saved cache to {BeatLeaderCacheFilename}");
        
        await LoadBeatLeaderCache();
    }

    private static async Task LoadProtoCache()
    {
        if (!File.Exists(CacheFilename))
        {
            Plugin.Log.Warn($"No cache file exists at {CacheFilename}");
            return;
        }

        Plugin.Log.Info($"Decompressing and loading cache from {CacheFilename}...");
        await using FileStream file = File.OpenRead(CacheFilename);
        await using GZipStream decompressor = new(file, CompressionMode.Decompress);
        CachedMapList mapList;
        try
        {
            mapList = Serializer.Deserialize<CachedMapList>(decompressor);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
            throw;
        }
        Plugin.Log.Info("Loaded cache");

        foreach (KeyValuePair<string, CachedMap> cachedMap in mapList.Maps)
        {
            CachedMaps.Add(cachedMap.Value.Key, cachedMap.Value);
            CachedMaps.Add(cachedMap.Value.Hash, cachedMap.Value);
        }
        
        Plugin.Log.Info($"{mapList.Maps.Count} maps are in the protobuf cache");
    }

    private static async Task LoadBeatLeaderCache()
    {
        Plugin.Log.Info($"Decompressing and loading BeatLeader star value cache from {BeatLeaderCacheFilename}...");
        await using FileStream file = File.OpenRead(BeatLeaderCacheFilename);
        await using GZipStream decompressor = new(file, CompressionMode.Decompress);
        
        BeatLeaderStarsCache beatLeaderStarsCache = Serializer.Deserialize<BeatLeaderStarsCache>(decompressor);
        Plugin.DebugMessage($"{beatLeaderStarsCache.Items.Length} items in the BeatLeader star value cache");

        foreach (BeatLeaderStarsCacheItem item in beatLeaderStarsCache.Items)
        {
            string hash = item.Hash.ToLower();
            if (!CachedMaps.TryGetValue(hash, out CachedMap? foundMap))
            {
                continue;
            }

            try
            {
                if (item.Mode.Contains("Inverted") || item.Mode.Contains("Inverse") || item.Mode.Contains("Vertical") || item.Mode.Contains("Horizontal") ||
                    item.Mode.Contains("RhythmGame", StringComparison.InvariantCultureIgnoreCase) ||
                    item.Mode.Contains("Generated") ||
                    item.Mode.Contains("Ghost") || item.Mode.Contains("OldDots") ||
                    item.Mode.Contains("Controllable") ||
                    item.Mode.Contains("BL_BS"))
                {
                    // these are generated diffs, we don't care
                    continue;
                }
                
                foundMap.Difficulties.First(x => x.Difficulty + x.Characteristic == item.Mode).RankedStatus.BeatLeader.Stars = item.Stars;
            }
            catch (InvalidOperationException)
            {
                Plugin.DebugMessage($"Could not find {item.Mode} in {hash}");
            }
        }
    }

    public CachedMap? GetMapById(string id)
    {
        uint wantedId = Convert.ToUInt32(id, 16);
        
        try
        {
            return CachedMaps[wantedId];
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    public CachedMap? GetMapByHash(string hash)
    {
        try
        {
            return CachedMaps[hash];
        }
        catch (Exception)
        {
            return null;
        }
    }
}