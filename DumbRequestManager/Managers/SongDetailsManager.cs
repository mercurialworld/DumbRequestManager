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
    public static SongDetails? CacheInstance;
    internal static readonly BeatSaver BeatSaverInstance = new(nameof(DumbRequestManager), Assembly.GetExecutingAssembly().GetName().Version);

    public void Initialize()
    {
        _ = Task.Run(async () =>
        {
            Plugin.Log.Info("Initializing SongDetailsCache...");
            CacheInstance = await SongDetails.Init();
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

    public static Song? GetByKey(string key)
    {
        if (CacheInstance == null)
        {
            Plugin.Log.Info("SongDetails not initialized");
            return null;
        }

        try
        {
            if (CacheInstance.songs.FindByMapId(key, out Song song))
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
    
    // also get by hash 
}