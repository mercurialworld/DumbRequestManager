using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.Services;
using DumbRequestManager.UI;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace DumbRequestManager.Managers;

[JsonObject(MemberSerialization.OptIn)]
public readonly struct PersistentQueueEntry(string key, string? user, bool isWip = false)
{
    [JsonProperty] internal string Key => key;
    [JsonProperty] internal string? User => user;
    [JsonProperty] internal bool IsWip => isWip;
}

[UsedImplicitly]
public static class QueueManager
{
    private static readonly string PersistentQueueFilename = Path.Combine(Plugin.UserDataDir, "queue.json");
    
    public static readonly List<NoncontextualizedSong> QueuedSongs = [];
    public static readonly List<NoncontextualizedSong> MapsActedOn = [];

    public static async Task<NoncontextualizedSong?> AddKey(string key, string? user = null, bool skipPersistence = false, bool prepend = false)
    {
        NoncontextualizedSong queuedSong;
        
        CachedMap? song = SongDetailsManager.GetByKey(key);
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
            string hash = song.Hash.ToLower();
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

        if (prepend)
        {
            QueuedSongs.Insert(0, queuedSong);
        }
        else
        {
            QueuedSongs.Add(queuedSong);
        }

        ChatRequestButton.Instance.UseAttentiveButton(true);
        QueueViewController.RefreshQueue();

        Plugin.Log.Info($"Added map {key}, queue has {QueuedSongs.Count} map(s)");
        
        // ReSharper disable once InvertIf
        if (!skipPersistence)
        {
            Save();
            
            SocketApi.Broadcast("mapAdded", queuedSong);
            _ = HookApi.TriggerHook("mapAdded", queuedSong);
        }

        return queuedSong;
    }

    public static NoncontextualizedSong AddWip(string key, string? user = null, bool prepend = true)
    {
        NoncontextualizedSong queuedSong = new()
        {
            BsrKey = key,
            User = user,
            Title = $"WIP Map {key}",
            Artist = "N/A",
            Mapper = user ?? "unknown"
        };
        
        if (prepend)
        {
            QueuedSongs.Insert(0, queuedSong);
        }
        else
        {
            QueuedSongs.Add(queuedSong);
        }
        
        ChatRequestButton.Instance.UseAttentiveButton(true);
        QueueViewController.RefreshQueue();
        
        Plugin.Log.Info($"Added WIP map {key}, queue has {QueuedSongs.Count} map(s)");
        
        SocketApi.Broadcast("mapAdded", queuedSong);
        _ = HookApi.TriggerHook("mapAdded", queuedSong);
        
        return queuedSong;
    }

    public static void Shuffle()
    {
        if (QueuedSongs.Count <= 1)
        {
            return;
        }
        
        for (int i = 0; i < QueuedSongs.Count; i++)
        {
            NoncontextualizedSong oldEntry = QueuedSongs[i];
            QueuedSongs.RemoveAt(i);
            QueuedSongs.Insert(Random.Range(0, QueuedSongs.Count), oldEntry);
        }
        
        QueueViewController.RefreshQueue();
        Save();
    }

    public static async Task Load()
    {
        if (!Directory.Exists(Plugin.UserDataDir))
        {
            Directory.CreateDirectory(Plugin.UserDataDir);
        }

        if (!File.Exists(PersistentQueueFilename))
        {
            Plugin.Log.Info("No persistent queue data, skipping loading it");
            return;
        }
        
        Plugin.DebugMessage("Loading persistent queue...");
        PersistentQueueEntry[]? entries = JsonConvert.DeserializeObject<PersistentQueueEntry[]>(await File.ReadAllTextAsync(PersistentQueueFilename));
        if (entries == null)
        {
            Plugin.DebugMessage("Queue was empty");
            return;
        }

        foreach (PersistentQueueEntry entry in entries)
        {
            if (entry.IsWip)
            {
                AddWip(entry.Key, entry.User);
            }
            else
            {
                await AddKey(entry.Key, entry.User, true);
            }
        }
        Plugin.DebugMessage("Loaded persistent queue");
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void Save()
    {
        if (!Directory.Exists(Plugin.UserDataDir))
        {
            Directory.CreateDirectory(Plugin.UserDataDir);
        }
        
        // we can do this the easy way, should be fine
        // if it becomes a problem i'll deal with it later
        
        PersistentQueueEntry[] keys = QueuedSongs.Select(x => new PersistentQueueEntry(x.BsrKey, x.User)).ToArray();
        File.WriteAllText(PersistentQueueFilename, JsonConvert.SerializeObject(keys, Formatting.Indented));
        
        Plugin.DebugMessage("Saved persistent queue");
    }
}