using System;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.Managers;

[UsedImplicitly]
public class SongDetailsManager : IInitializable, IDisposable
{
    internal static readonly BeatSaver BeatSaverInstance = new(nameof(DumbRequestManager), Assembly.GetExecutingAssembly().GetName().Version);

    public void Initialize()
    {
    }

    public void Dispose()
    {
    }

    public static async Task<Beatmap?> GetDirectByKey(string key)
    {
        return await BeatSaverInstance.Beatmap(key);
    }

    public static async Task<Beatmap?> GetDirectByHash(string hash)
    {
        return await BeatSaverInstance.BeatmapByHash(hash);
    }

    public static CachedMap? GetByKey(string key) => MapCacheManager.Instance?.GetMapById(key);
    public static CachedMap? GetByMapHash(string hash) => MapCacheManager.Instance?.GetMapByHash(hash);
}