using System.Collections.Generic;
using DumbRequestManager.Classes;
using DumbRequestManager.UI;
using JetBrains.Annotations;
using SongDetailsCache.Structs;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
public static class QueueManager
{
    public static readonly List<QueuedSong> QueuedSongs = [];

    public static QueuedSong? AddKey(string key)
    {
        Song? song = SongDetailsManager.GetByKey(key);
        if (song == null)
        {
            Plugin.Log.Info("uh oh cheerio it's null");
            return null;
        }

        QueuedSong queuedSong = new(song.Value);
        QueuedSongs.Add(queuedSong);
        
        ChatRequestButton.Instance.UseAttentiveButton(true);

#if DEBUG
        Plugin.Log.Info($"Added map {key} ({song.Value.songAuthorName} - {song.Value.songName} [{song.Value.levelAuthorName}]), queue has {QueuedSongs.Count} map(s)");
#endif
        return queuedSong;
    }
}