using System.Reflection;
using System.Threading.Tasks;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using JetBrains.Annotations;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
public static class SongDetailsManager
{
    internal static readonly BeatSaver BeatSaverInstance = new(nameof(DumbRequestManager), Assembly.GetExecutingAssembly().GetName().Version);

    public static async Task<Beatmap?> GetDirectByKey(string key)
    {
        return await BeatSaverInstance.Beatmap(key.ToLower());
    }

    public static async Task<Beatmap?> GetDirectByHash(string hash)
    {
        return await BeatSaverInstance.BeatmapByHash(hash.ToLower());
    }

    public static CachedMap? GetByKey(string key) => MapCacheManager.Instance?.GetMapById(key.ToLower());
    //public static CachedMap? GetByMapHash(string hash) => MapCacheManager.Instance?.GetMapByHash(hash.ToLower());
}