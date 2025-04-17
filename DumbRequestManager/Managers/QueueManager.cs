using System.Collections.Generic;
using DumbRequestManager.Classes;
using JetBrains.Annotations;
using SongDetailsCache.Structs;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
public static class QueueManager
{
    public static readonly List<QueuedSong> QueuedSongs = [];

    public static void AddKey(string key)
    {
        Song? song = SongDetailsManager.GetByKey(key);
        if (song == null)
        {
            Plugin.Log.Info("uh oh cheerio it's null");
            return;
        }

        QueuedSong queuedSong = new(song.Value);
        QueuedSongs.Add(queuedSong);

#if DEBUG
        Plugin.Log.Info($"Added map {key} ({song.Value.songAuthorName} - {song.Value.songName} [{song.Value.levelAuthorName}]), queue has {QueuedSongs.Count} map(s)");
#endif
    }
}