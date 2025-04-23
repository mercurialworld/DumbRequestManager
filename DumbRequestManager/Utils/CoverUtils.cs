using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IPA.Utilities;

namespace DumbRequestManager.Utils;

internal abstract class RemoteImage
{
    private static readonly HttpClient ImageHttpClient = new();
    private static readonly string CachePath = Path.Combine(Plugin.UserDataDir, "Cache");

    private static async Task<byte[]?> FetchData(string url)
    {
        Uri uri = new(url);
        string filename = Path.Combine(CachePath, uri.Segments.Last().Replace("/", string.Empty));
        
        if (IsCoverCached(url))
        {
            Plugin.DebugMessage("Cover was hard cached");
            return await File.ReadAllBytesAsync(filename);
        }
        
        HttpResponseMessage imageResponse = await ImageHttpClient.GetAsync(url);
        
        if (!imageResponse.IsSuccessStatusCode)
        {
            Plugin.Log.Warn($"Failed to fetch cover from {imageResponse.Headers.Location}, image response was bad");
            return null;
        }
        
        Plugin.DebugMessage("Fetched cover");
        byte[] bytes = await imageResponse.Content.ReadAsByteArrayAsync();

        await File.WriteAllBytesAsync(filename, bytes);
        
        return bytes;
    }

    private static bool IsCoverCached(string url)
    {
        if (!Directory.Exists(CachePath))
        {
            Plugin.DebugMessage("Creating hard cache directory");
            Directory.CreateDirectory(CachePath);
            return false;
        }
        
        Uri uri = new(url);
        string filename = Path.Combine(CachePath, uri.Segments.Last().Replace("/", string.Empty));
        Plugin.DebugMessage($"Wants hard cached cover {filename}");

        return File.Exists(filename);
    }

    internal static async Task<byte[]?> Fetch(string url)
    {
        byte[]? imageBytes = await FetchData(url);
        if (imageBytes != null)
        {
            return imageBytes;
        }
        
        Plugin.DebugMessage("imageBytes was null");
        return null;
    }
}

internal static class Cover
{
    private static readonly Dictionary<string, byte[]> CoverCache = new();
    
    public static async Task<byte[]?> LoadCover(string url)
    {
        Plugin.DebugMessage($"Loading cover from {url}");
        Plugin.DebugMessage($"{CoverCache.Count} covers have been cached");
        
        if (CoverCache.TryGetValue(url, out byte[] cached))
        {
            Plugin.DebugMessage("Cover was cached, using it");
            return cached;
        }
        
        Plugin.DebugMessage("Cover not cached, fetching...");

        byte[]? cover = null;
        try
        {
            cover = await RemoteImage.Fetch(url);
            if (cover == null)
            {
                Plugin.DebugMessage("Cover was null");
                return null;
            }
            
            Plugin.DebugMessage(CoverCache.TryAdd(url, cover) ? "Cover was cached" : "Cover was not cached???");
            return cover;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        return cover;
    }
}