using System;
using System.Threading.Tasks;
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

    public static Song? GetByKey(string key)
    {
        if (CacheInstance == null)
        {
            // hey parrot do beat saver stuff here too
            Plugin.Log.Info("SongDetails not initialized");
            return null;
        }

        if (CacheInstance.songs.FindByMapId(key, out Song song))
        {
            return song;
        }

        // do beat saver stuff
        return null;
    }
    
    // also get by hash 
}