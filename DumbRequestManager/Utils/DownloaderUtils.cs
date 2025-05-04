using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using DumbRequestManager.Configuration;
#if !DEBUG
using DumbRequestManager.Managers;
#endif
using JetBrains.Annotations;
using SiraUtil.Web;
using Zenject;

namespace DumbRequestManager.Utils;

[UsedImplicitly]
internal class DownloaderUtils(IHttpService httpService) : IInitializable
{
    private static PluginConfig Config => PluginConfig.Instance;
    
    public void Initialize()
    {
        try
        {
            httpService.Timeout = Config.MapDownloadTimeout;
        }
        catch (Exception)
        {
            // do nothing, this is new. submitted a PR to SiraUtil (still pending)
        }
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

        string folderName = $"{beatmap.ID} ({beatmap.Metadata.SongName} - {beatmap.Metadata.LevelAuthorName})";
        Plugin.DebugMessage($"[DownloadUtils] Folder name before: {folderName}...");
        Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Aggregate(folderName,
            (current, invalidCharacter) => current.Replace(invalidCharacter.ToString(), string.Empty));
        Plugin.DebugMessage($"[DownloadUtils] Folder name after: {folderName}...");
        
        string fullFolderPath = Path.Combine(customSongsPath, folderName);
        if (!Directory.Exists(fullFolderPath))
        {
            Plugin.DebugMessage("[DownloadUtils] Folder doesn't exist, making it");
            Directory.CreateDirectory(fullFolderPath);
        }
        else
        {
            Plugin.DebugMessage("[DownloadUtils] Folder exists, making a dupe");
            
            string originalFullFolderPath = fullFolderPath;
            for (int dupes = 1; Directory.Exists(fullFolderPath); dupes++)
            {
                fullFolderPath = originalFullFolderPath + $" ({dupes++})";
            }
            
            Plugin.DebugMessage($"[DownloadUtils] Dupe folder name: {fullFolderPath}");
            Directory.CreateDirectory(fullFolderPath);
        }
        
        MemoryStream stream = new(result);
        Plugin.DebugMessage("[DownloadUtils] Created MemoryStream");
        ZipArchive archive = new(stream, ZipArchiveMode.Read);
        Plugin.DebugMessage("[DownloadUtils] Created ZipArchive");
        archive.ExtractToDirectory(fullFolderPath);
        Plugin.DebugMessage($"[DownloadUtils] Extracted to {fullFolderPath}");
        
        await stream.DisposeAsync();
        archive.Dispose();
        Plugin.DebugMessage("[DownloadUtils] Disposed stuff");
        
        SongCore.Loader.Instance.RefreshSongs(false);
#if !DEBUG
        QueueManager.Save();
#endif
    }
}