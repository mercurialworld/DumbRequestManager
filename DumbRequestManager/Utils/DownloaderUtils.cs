using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverDownloader.Misc;
using BeatSaverSharp.Models;
using JetBrains.Annotations;
using SiraUtil.Web;
using Zenject;

namespace DumbRequestManager.Utils;

[UsedImplicitly]
internal class DownloaderUtils(IHttpService httpService) : IInitializable
{
    public void Initialize()
    {
    }
    
    // bypassing BeatSaverDownloader's downloading code as it doesn't properly handle token cancellations
    
    private async Task<byte[]> DownloadZip(string url, CancellationToken token = default, IProgress<float>? progress = null)
    {
        IHttpResponse response = await httpService.GetAsync(url, progress, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        
        if (!response.Successful)
        {
            return [];
        }

        byte[] result = response.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        token.ThrowIfCancellationRequested();

        return result;
    }

    internal async Task DownloadUsingKey(Beatmap beatmap, CancellationToken token = default, IProgress<float>? progress = null)
    {
        string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
        if (!Directory.Exists(customSongsPath))
        {
            Directory.CreateDirectory(customSongsPath);
        }
        
        Plugin.DebugMessage($"[DownloadUtils] Downloading map {beatmap.ID}...");
        byte[] result = await DownloadZip(beatmap.LatestVersion.DownloadURL, token, progress);
        token.ThrowIfCancellationRequested();
        Plugin.DebugMessage($"[DownloadUtils] Downloaded {beatmap.ID}");

        if (result.Length == 0)
        {
            Plugin.Log.Warn("[DownloadUtils] Map download was empty");
            return;
        }
        
        await SongDownloader.ExtractZipAsync(beatmap, result, customSongsPath);
        SongCore.Loader.Instance.RefreshSongs(false);
    }
}