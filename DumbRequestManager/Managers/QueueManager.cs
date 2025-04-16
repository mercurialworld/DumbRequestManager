using System.Collections.Generic;
using JetBrains.Annotations;
using SongDetailsCache.Structs;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
public static class QueueManager
{
    public static readonly List<Song> QueuedSongs = [];

    public static void AddKey(string key)
    {
        Song? song = SongDetailsManager.GetByKey(key);
        if (song == null)
        {
            Plugin.Log.Info("uh oh cheerio it's null");
            return;
        }
        
        QueuedSongs.Add(song.Value);
        
#if DEBUG
        Plugin.Log.Info($"Added map {key} ({song?.songAuthorName} - {song?.songName} [{song?.levelAuthorName}]), queue has {QueuedSongs.Count} map(s)");
#endif
    }
}