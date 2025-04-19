using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using SongDetailsCache.Structs;
using UnityEngine;

namespace DumbRequestManager.Utils;

internal abstract class RemoteImage
{
    private static readonly HttpClient ImageHttpClient = new();

    private static async Task<byte[]?> FetchData(string url)
    {
        HttpResponseMessage imageResponse = await ImageHttpClient.GetAsync(url);
        
        if (!imageResponse.IsSuccessStatusCode)
        {
            Plugin.Log.Warn($"Failed to fetch cover from {imageResponse.Headers.Location}, image response was bad");
            return null;
        }
        
        Plugin.Log.Info("Fetched cover");
        return await imageResponse.Content.ReadAsByteArrayAsync();
    }

    internal static async Task<byte[]?> Fetch(string url)
    {
        byte[]? imageBytes = await FetchData(url);
        if (imageBytes != null)
        {
            return imageBytes;
        }
        
        Plugin.Log.Info("imageBytes was null");
        return null;

        //return await Utilities.LoadSpriteAsync(imageBytes);
    }
}

internal static class Cover
{
    private static readonly Dictionary<string, byte[]> CoverCache = new();
    
    public static async Task<byte[]?> LoadCover(string url)
    {
#if DEBUG
        Plugin.Log.Info($"Loading cover from {url}");
        Plugin.Log.Info($"{CoverCache.Count} covers have been cached");
#endif
        if (CoverCache.TryGetValue(url, out byte[] cached))
        {
#if DEBUG
            Plugin.Log.Info("Cover was cached, using it");
#endif
            return cached;
        }
        
#if DEBUG
        Plugin.Log.Info("Cover not cached, fetching...");
#endif

        byte[]? cover = null;
        try
        {
            cover = await RemoteImage.Fetch(url);
            if (cover == null)
            {
#if DEBUG
                Plugin.Log.Info("Cover was null");
#endif
                return null;
            }
            
            Plugin.Log.Info(CoverCache.TryAdd(url, cover) ? "Cover was cached" : "Cover was not cached???");
            return cover;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        return cover;
    }
}