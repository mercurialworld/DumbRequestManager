using System;
using System.Collections.Generic;
using System.IO;
using DumbRequestManager.Classes;
using JetBrains.Annotations;
using ProtoBuf;
using Zenject;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
internal class MapCacheManager : IInitializable
{
    private static readonly Dictionary<uint, CachedMap> CachedMaps = new();
    private static readonly Dictionary<string, CachedMap> CachedMapsByHash = new();
    
    internal static MapCacheManager? Instance;
    
    public void Initialize()
    {
        Instance = this;
        
        using FileStream file = File.OpenRead(Path.Combine(Plugin.UserDataDir, "cached.proto"));
        CachedMapList mapList = Serializer.Deserialize<CachedMapList>(file);

        foreach (CachedMap cachedMap in mapList.Maps)
        {
            CachedMaps.Add(cachedMap.Key, cachedMap);
            CachedMapsByHash.Add(cachedMap.Hash, cachedMap);
        }
        
        Plugin.Log.Info($"Detected {mapList.Maps.Length} maps in the protobuf cache");
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