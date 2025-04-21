using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using JetBrains.Annotations;
using SongDetailsCache.Structs;
using Zenject;

namespace DumbRequestManager.Managers;

internal abstract class SessionHistoryManager
{
    public static readonly Dictionary<long, NoncontextualizedSong> SessionHistory = new();

    public static void AddToSession(NoncontextualizedSong song)
    {
        if (song.Hash.Contains("wip"))
        {
            Plugin.DebugMessage("Current map is a WIP map, not adding to session history");
        }

        if (SessionHistory.Count > 0)
        {
            if (SessionHistory.Last().Value.Hash == song.Hash)
            {
                Plugin.DebugMessage("Previous map was the same as the current map, not adding to session history");
                return;
            }
        }

        SessionHistory.Add(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), song);
        Plugin.DebugMessage($"Added {song.Title} to session history...");
    }
}

[UsedImplicitly]
internal class StartMapEvent(GameplayCoreSceneSetupData gameplayCoreSceneSetupData) : IInitializable
{
    public void Initialize()
    {
        BeatmapLevel originalMap = gameplayCoreSceneSetupData.beatmapLevel;
        string hash = originalMap.levelID.Split("_").Last();
        if (hash.Length != 40)
        {
            Plugin.Log.Info($"Hash was not of expected length (40): ${hash}");
            return;
        }
        
        NoncontextualizedSong queuedSong;
        
        Song? song = SongDetailsManager.GetByMapHash(hash);
        if (song == null)
        {
            Beatmap? beatmap = SongDetailsManager.GetDirectByHash(hash).Result; 
            if (beatmap != null)
            {
                queuedSong = new NoncontextualizedSong(beatmap, true);
            }
            else
            {
                return;
            }
        }
        else
        {
            queuedSong = new NoncontextualizedSong(song, true);
        }
        
        Plugin.DebugMessage($"Adding {queuedSong.Title} to session history...");
        SessionHistoryManager.AddToSession(queuedSong);
    }
}