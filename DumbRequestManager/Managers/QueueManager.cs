using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.UI;
using JetBrains.Annotations;
using SongDetailsCache.Structs;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
public static class QueueManager
{
    public static readonly List<NoncontextualizedSong> QueuedSongs = [];

    public static async Task<NoncontextualizedSong?> AddKey(string key, string? user = null)
    {
        NoncontextualizedSong queuedSong;
        
        Song? song = SongDetailsManager.GetByKey(key);
        if (song == null)
        {
            Beatmap? beatmap = await SongDetailsManager.GetDirectByKey(key); 
            if (beatmap != null)
            {
                queuedSong = new NoncontextualizedSong(beatmap);
            }
            else
            {
                return null;
            }
        }
        else
        {
            queuedSong = new NoncontextualizedSong(song);
        }

        if (user != null)
        {
            queuedSong.User = user;
        }

        QueuedSongs.Add(queuedSong);
        
        ChatRequestButton.Instance.UseAttentiveButton(true);
        
        Plugin.Log.Info($"Added map {key}, queue has {QueuedSongs.Count} map(s)");
        return queuedSong;
    }
}