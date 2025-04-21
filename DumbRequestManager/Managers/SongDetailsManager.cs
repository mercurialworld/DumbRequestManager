using System;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using JetBrains.Annotations;
using SongDetailsCache;
using SongDetailsCache.Structs;
//using SongDetailsCache.Structs;
using Zenject;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
public class SongDetailsManager : IInitializable, IDisposable
{
    private static SongDetails? _cacheInstance;
    internal static readonly BeatSaver BeatSaverInstance = new(nameof(DumbRequestManager), Assembly.GetExecutingAssembly().GetName().Version);

    public void Initialize()
    {
        _ = Task.Run(async () =>
        {
            Plugin.Log.Info("Initializing SongDetailsCache...");
            _cacheInstance = await SongDetails.Init();
            Plugin.Log.Info("Initialized SongDetailsCache");
        });
    }

    public void Dispose()
    {
    }

    public static async Task<Beatmap?> GetDirectByKey(string key)
    {
        return await BeatSaverInstance.Beatmap(key);
    }

    public static async Task<Beatmap?> GetDirectByHash(string hash)
    {
        return await BeatSaverInstance.BeatmapByHash(hash);
    }

    public static Song? GetByKey(string key)
    {
        if (_cacheInstance == null)
        {
            Plugin.Log.Info("SongDetails not initialized");
            return null;
        }

        try
        {
            if (_cacheInstance.songs.FindByMapId(key, out Song song))
            {
                return song;
            }
        }
#pragma warning disable CS0168
        catch (Exception e)
#pragma warning restore CS0168
        {
            Plugin.Log.Info("Could not find key in SongDetailsCache");
#if DEBUG
            Plugin.Log.Error(e);
#endif
        }

        return null;
    }

    public static Song? GetByMapHash(string hash)
    {
        if (_cacheInstance == null)
        {
            Plugin.Log.Info("SongDetails not initialized");
            return null;
        }

        try
        {
            if (_cacheInstance.songs.FindByHash(hash, out Song song))
            {
                return song;
            }
        }
#pragma warning disable CS0168
        catch (Exception e)
#pragma warning restore CS0168
        {
            Plugin.Log.Info("Could not find hash in SongDetailsCache");
#if DEBUG
            Plugin.Log.Error(e);
#endif
        }

        return null;
    }
    
    // also get by hash 
}