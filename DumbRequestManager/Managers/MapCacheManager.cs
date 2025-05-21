using System;
using System.Collections.Generic;
using System.IO;
using DumbRequestManager.Classes;
using ProtoBuf;
using Zenject;

namespace DumbRequestManager.Managers;

internal class MapCacheManager : IInitializable
{
    private static readonly Dictionary<uint, CachedMap> CachedMaps = new();
    
    public void Initialize()
    {
        using FileStream file = File.OpenRead(Path.Combine(Plugin.UserDataDir, "cached.proto"));
        CachedMapList mapList = Serializer.Deserialize<CachedMapList>(file);

        foreach (CachedMap cachedMap in mapList.Maps)
        {
            CachedMaps.Add(cachedMap.Key, cachedMap);
        }
        
        Plugin.Log.Info($"Detected {mapList.Maps.Length} maps in the protobuf cache");
    }

    public CachedMap? GetMapById(string id)
    {
        uint wantedId = Convert.ToUInt32(id);
        
        try
        {
            return CachedMaps[wantedId];
        }
        catch (Exception)
        {
            return null;
        }
    }
}