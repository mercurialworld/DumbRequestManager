using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DumbRequestManager.Classes;
using DumbRequestManager.Configuration;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Zenject;

namespace DumbRequestManager.Managers;

internal abstract class SessionHistoryManager
{
    private static PluginConfig Config => PluginConfig.Instance;
    public static Dictionary<long, NoncontextualizedSong> SessionHistory = new();

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

        try
        {
            SessionHistory.Add(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), song);
            Plugin.DebugMessage($"Added {song.Title} to session history");
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
        
        File.WriteAllText(Path.Combine(Plugin.UserDataDir, "history.json"), JsonConvert.SerializeObject(SessionHistory, Formatting.Indented));
    }

    internal static void LoadPreviousSessionHistory()
    {
        string filename = Path.Combine(Plugin.UserDataDir, "history.json");
        if (!File.Exists(filename))
        {
            Plugin.DebugMessage("No history file found");
            return;
        }

        if (DateTime.UtcNow > File.GetLastWriteTimeUtc(filename).AddMinutes(Config.AssumeNewSessionAfterMinutes))
        {
            Plugin.Log.Info($"History file was last modified more than {Config.AssumeNewSessionAfterMinutes} minutes ago, assuming this is a new session");
            return;
        }

        try
        {
            SessionHistory = JsonConvert.DeserializeObject<Dictionary<long, NoncontextualizedSong>>(File.ReadAllText(filename))!;
            Plugin.Log.Info("Loaded previous session history");
        }
        catch (Exception e)
        {
            Plugin.Log.Warn("Could not load previous session history");
            Plugin.Log.Warn(e);
        }
    }
}

[UsedImplicitly]
internal class StartMapEvent : IInitializable
{
    private static GameplayCoreSceneSetupData? _gameplayCoreSceneSetupData;
    
    [Inject]
    public StartMapEvent(GameplayCoreSceneSetupData gameplayCoreSceneSetupData)
    {
        _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
    }
    
    public void Initialize()
    {
        BeatmapLevel? originalMap = null;
        
        try
        {
            originalMap = _gameplayCoreSceneSetupData?.beatmapLevel;
            string? hash = originalMap?.levelID.Split("_").Last();
            
            if (hash?.Length != 40)
            {
                Plugin.Log.Info($"Hash was not of expected length (40): ${hash}");
                return;
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        try
        {
            if (originalMap != null)
            {
                NoncontextualizedSong queuedSong = new(originalMap, true);

                Plugin.DebugMessage($"Adding {queuedSong.Title} to session history...");
                SessionHistoryManager.AddToSession(queuedSong);
            }
            else
            {
                Plugin.DebugMessage("originalMap is null?");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }
}