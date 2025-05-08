using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.Configuration;
using DumbRequestManager.UI;
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
        MethodInfo? timeoutMethod = httpService.GetType().GetMethod("set_Timeout");
        
        if (timeoutMethod == null)
        {
            Plugin.Log.Info("Timeout value cannot be set, doesn't exist in SiraUtil yet.");
        }
        else
        {
            timeoutMethod.Invoke(httpService, [Config.MapDownloadTimeout]);
            Plugin.Log.Info($"Set timeout value to {Config.MapDownloadTimeout} seconds.");
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

    internal async Task DownloadWip(NoncontextualizedSong beatmap, CancellationToken token = default, IProgress<float>? progress = null)
    {
        string customSongsPath = Path.Combine(CustomLevelPathHelper.baseProjectPath, "CustomWIPLevels");
        if (!Directory.Exists(customSongsPath))
        {
            Directory.CreateDirectory(customSongsPath);
        }
        
        Plugin.DebugMessage($"[DownloadUtils] Downloading WIP map {beatmap.BsrKey}...");
        byte[] result = [];
        try
        {
            result = await DownloadZip($"https://catse.net/wips/{beatmap.BsrKey}.zip", token, progress);
        }
        catch (Exception exception)
        {
            if (exception is TaskCanceledException or OperationCanceledException)
            {
                throw new TaskCanceledException(exception.Message, exception);
            }
        }
        
        token.ThrowIfCancellationRequested();
        Plugin.DebugMessage($"[DownloadUtils] Downloaded WIP map {beatmap.BsrKey}");
        
        if (result.Length == 0)
        {
            Plugin.Log.Warn("[DownloadUtils] Map download was empty");
            return;
        }
        
        string folderName = $"WIP-{beatmap.BsrKey}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        if (beatmap.User != null)
        {
            folderName += $" ({beatmap.User})";
        }
        
        Plugin.DebugMessage($"[DownloadUtils] Folder name before: {folderName}...");
        Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Aggregate(folderName,
            (current, invalidCharacter) => current.Replace(invalidCharacter.ToString(), string.Empty));
        Plugin.DebugMessage($"[DownloadUtils] Folder name after: {folderName}...");
        
        // this is guaranteed to not exist
        string fullFolderPath = Path.Combine(customSongsPath, folderName);
        Directory.CreateDirectory(fullFolderPath);
        
        MemoryStream stream = new(result);
        Plugin.DebugMessage("[DownloadUtils] Created MemoryStream");
        ZipArchive archive = new(stream, ZipArchiveMode.Read);
        Plugin.DebugMessage("[DownloadUtils] Created ZipArchive");
        archive.ExtractToDirectory(fullFolderPath);
        Plugin.DebugMessage($"[DownloadUtils] Extracted to {fullFolderPath}");
        
        await stream.DisposeAsync();
        archive.Dispose();
        Plugin.DebugMessage("[DownloadUtils] Disposed stuff");
        
        SongCore.Loader.SongsLoadedEvent += LoaderOnSongsLoadedEvent;
        SongCore.Loader.Instance.RefreshSongs(false);

        void LoaderOnSongsLoadedEvent(SongCore.Loader loader, ConcurrentDictionary<string, BeatmapLevel> concurrentDictionary)
        {
            (string, BeatmapLevel)? wipLevelData = SongCore.Loader.LoadCustomLevel(fullFolderPath);
            if (wipLevelData != null)
            {
                beatmap.Hash = $"{wipLevelData.Value.Item1} WIP";
                QueueViewController.Instance.OkGoBack(beatmap, wipLevelData.Value.Item2);
            }

            SongCore.Loader.SongsLoadedEvent -= LoaderOnSongsLoadedEvent;
        }
#if !DEBUG
        QueueManager.Save();
#endif
    }

    internal async Task DownloadUsingKey(Beatmap beatmap, CancellationToken token = default, IProgress<float>? progress = null)
    {
        string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
        if (!Directory.Exists(customSongsPath))
        {
            Directory.CreateDirectory(customSongsPath);
        }
        
        Plugin.DebugMessage($"[DownloadUtils] Downloading map {beatmap.ID}...");
        byte[] result = [];
        try
        {
            result = await DownloadZip(beatmap.LatestVersion.DownloadURL, token, progress);
        }
        catch (Exception exception)
        {
            if (exception is TaskCanceledException or OperationCanceledException)
            {
                throw new TaskCanceledException(exception.Message, exception);
            }
        }

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
#if !DEBUG
        QueueManager.Save();
#endif
    }
}