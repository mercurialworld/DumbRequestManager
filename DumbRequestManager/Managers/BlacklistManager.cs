using System.Collections.Generic;
using System.IO;
using System.Text;
using DumbRequestManager.UI;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Zenject;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
internal class BlacklistManager : IInitializable
{
    private static List<string> _blacklistedIds = [];
    private static readonly string BlacklistPath = Path.Combine(Plugin.UserDataDir, "blacklist.json");
    
    public void Initialize()
    {
        if (!File.Exists(BlacklistPath))
        {
            Plugin.Log.Info("No blacklist file -- assuming empty blacklist.");
            return;
        }
        
        string json = File.ReadAllText(BlacklistPath);
        _blacklistedIds = JsonConvert.DeserializeObject<List<string>>(json)!;
        
        Plugin.Log.Info($"{_blacklistedIds.Count} map(s) are blacklisted.");
    }

    private static void Save()
    {
        File.WriteAllText(BlacklistPath, JsonConvert.SerializeObject(_blacklistedIds, Formatting.Indented));
        Plugin.DebugMessage("Saved blacklist");
    }

    public static bool ContainsKey(string bsrKey) => _blacklistedIds.Contains(bsrKey.ToLower());

    public static void AddKey(string bsrKey)
    {
        bsrKey = bsrKey.ToLower();
        
        if (!_blacklistedIds.Contains(bsrKey))
        {
            _blacklistedIds.Add(bsrKey);
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
        
        if (_blacklistedIds.Contains(bsrKey))
        {
            _blacklistedIds.Remove(bsrKey);
        }
        
        Save();
    }
    
    public static byte[] GetEncoded() => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_blacklistedIds));
}