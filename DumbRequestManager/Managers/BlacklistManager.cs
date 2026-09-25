using System.Collections.Generic;
using System.IO;
using DumbRequestManager.UI;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Zenject;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
internal class BlacklistManager : IInitializable
{
    public static List<string> BlacklistedIDs { get; private set; } = [];

    private static readonly string BlacklistPath = Path.Combine(Plugin.UserDataDir, "blacklist.json");
    
    public void Initialize()
    {
        if (!File.Exists(BlacklistPath))
        {
            Plugin.Log.Info("No blacklist file -- assuming empty blacklist.");
            return;
        }
        
        string json = File.ReadAllText(BlacklistPath);
        BlacklistedIDs = JsonConvert.DeserializeObject<List<string>>(json)!;
        
        Plugin.Log.Info($"{BlacklistedIDs.Count} map(s) are blacklisted.");
    }

    private static void Save()
    {
        File.WriteAllText(BlacklistPath, JsonConvert.SerializeObject(BlacklistedIDs, Formatting.Indented));
        Plugin.DebugMessage("Saved blacklist");
    }

    public static bool ContainsKey(string bsrKey) => BlacklistedIDs.Contains(bsrKey.ToLower());

    public static void AddKey(string bsrKey)
    {
        bsrKey = bsrKey.ToLower();
        
        if (!BlacklistedIDs.Contains(bsrKey))
        {
            BlacklistedIDs.Add(bsrKey);
        }
        
        int foundIndex = QueueManager.QueuedSongs.FindIndex(x => x.BsrKey == bsrKey);
        if (foundIndex != -1)
        {
            QueueManager.QueuedSongs.RemoveAt(foundIndex);
        }
        QueueViewController.RefreshQueue();
        
        Save();
    }

    public static void RemoveKey(string bsrKey)
    {
        bsrKey = bsrKey.ToLower();
        
        if (BlacklistedIDs.Contains(bsrKey))
        {
            BlacklistedIDs.Remove(bsrKey);
        }
        
        Save();
    }
}