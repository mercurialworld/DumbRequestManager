using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.UI;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SongDetailsCache.Structs;

namespace DumbRequestManager.Managers;

[JsonObject(MemberSerialization.OptIn)]
public readonly struct PersistentQueueEntry(string key, string? user)
{
    [JsonProperty] internal string Key => key;
    [JsonProperty] internal string? User => user;
}

[UsedImplicitly]
public static class QueueManager
{
    private static readonly string PersistentQueueFilename = Path.Combine(Plugin.UserDataDir, "queue.json");
    public static readonly List<NoncontextualizedSong> QueuedSongs = [];

    public static async Task<NoncontextualizedSong?> AddKey(string key, string? user = null, bool skipPersistence = false)
    {
        NoncontextualizedSong queuedSong;
        
        Song? song = SongDetailsManager.GetByKey(key);
        if (song == null)
        {
            Beatmap? beatmap = await SongDetailsManager.GetDirectByKey(key); 
            if (beatmap != null)
            {
                Plugin.DebugMessage("Using BeatSaverSharp method");
                queuedSong = new NoncontextualizedSong(beatmap);
            }
            else
            {
                Plugin.Log.Info("oh we're screwed");
                return null;
            }
        }
        else
        {
            string hash = song.Value.hash.ToLower();
            Plugin.DebugMessage($"Checking SongCore for hash {hash}");
            if (SongCore.Collections.songWithHashPresent(hash))
            {
                Plugin.DebugMessage("Using local map method");
                
                // can't be null here
                BeatmapLevel beatmapLevel = SongCore.Loader.GetLevelByHash(hash)!;
                queuedSong = new NoncontextualizedSong(beatmapLevel);
            }
            else
            {
                Plugin.DebugMessage("Using SongDetailsCache method");
                queuedSong = new NoncontextualizedSong(song);
            }
        }

        if (user != null)
        {
            queuedSong.User = user;
        }

        QueuedSongs.Add(queuedSong);
        
        ChatRequestButton.Instance.UseAttentiveButton(true);
        
        Plugin.Log.Info($"Added map {key}, queue has {QueuedSongs.Count} map(s)");
        if (!skipPersistence)
        {
            Save();
        }

        return queuedSong;
    }

    public static async Task Load()
    {
        Plugin.DebugMessage("Loading persistent queue...");
        PersistentQueueEntry[]? entries = JsonConvert.DeserializeObject<PersistentQueueEntry[]>(await File.ReadAllTextAsync(PersistentQueueFilename));
        if (entries == null)
        {
            Plugin.DebugMessage("Queue was empty");
            return;
        }

        foreach (PersistentQueueEntry entry in entries)
        {
            await AddKey(entry.Key, entry.User, true);
        }
        Plugin.DebugMessage("Loaded persistent queue");
    }

    private static void Save()
    {
        // we can do this the easy way, should be fine
        // if it becomes a problem i'll deal with it later
        
        PersistentQueueEntry[] keys = QueuedSongs.Select(x => new PersistentQueueEntry(x.BsrKey, x.User)).ToArray();
        File.WriteAllText(PersistentQueueFilename, JsonConvert.SerializeObject(keys, Formatting.Indented));
        
        Plugin.DebugMessage("Saved persistent queue");
    }
}