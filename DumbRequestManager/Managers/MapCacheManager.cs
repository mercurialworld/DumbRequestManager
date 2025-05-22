using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using DumbRequestManager.Classes;
using JetBrains.Annotations;
using ProtoBuf;
using SiraUtil.Web;
using Zenject;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
internal class MapCacheManager(IHttpService httpService) : IInitializable
{
    private static readonly Dictionary<uint, CachedMap> CachedMaps = new();
    private static readonly Dictionary<string, CachedMap> CachedMapsByHash = new();

    private static readonly string CacheFilename = Path.Combine(Plugin.UserDataDir, "cached.proto");
    private const string CacheURL = "https://theblackparrot.me/DumbRequestManager/cache.proto.gz";

    internal static MapCacheManager? Instance;
    
    public void Initialize()
    {
        Instance = this;

        _ = SetupCacheStuff();
    }

    private async Task SetupCacheStuff()
    {
        if (File.Exists(CacheFilename))
        {
            if (DateTime.UtcNow < File.GetLastWriteTimeUtc(CacheFilename).AddHours(12))
            {
                Plugin.Log.Info("Cache was obtained less than 12 hours ago, not refreshing");
                LoadProtoCache();
                return;
            }
        }
        
        Plugin.Log.Info($"Grabbing the cache from {CacheURL}...");
        IHttpResponse response = await httpService.GetAsync(CacheURL).ConfigureAwait(false);
        
        if (!response.Successful)
        {
            Plugin.Log.Warn($"Couldn't download the cache (HTTP {response.Code})");
            LoadProtoCache();
            return;
        }
        
        Plugin.Log.Info($"Grabbed cache from {CacheURL}, decompressing...");
        
        Stream result = response.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        await using FileStream outputFileStream = File.Create(CacheFilename);
        await using GZipStream decompressor = new(result, CompressionMode.Decompress);
        await decompressor.CopyToAsync(outputFileStream);
        
        Plugin.Log.Info($"Saved cache to {CacheFilename}");
        
        LoadProtoCache();
    }

    private static void LoadProtoCache()
    {
        if (!File.Exists(CacheFilename))
        {
            Plugin.Log.Warn($"No cache file exists at {CacheFilename}");
            return;
        }

        using FileStream file = File.OpenRead(CacheFilename);
        CachedMapList mapList = Serializer.Deserialize<CachedMapList>(file);

        foreach (CachedMap cachedMap in mapList.Maps)
        {
            CachedMaps.Add(cachedMap.Key, cachedMap);
            CachedMapsByHash.Add(cachedMap.Hash, cachedMap);
        }
        
        Plugin.Log.Info($"{mapList.Maps.Length} maps are in the protobuf cache");
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
            return CachedMapsByHash[hash];
        }
        catch (Exception)
        {
            return null;
        }
    }
}