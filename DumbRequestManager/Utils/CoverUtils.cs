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

    internal static async Task<Sprite?> Fetch(string url)
    {
        byte[]? imageBytes = await FetchData(url);
        if (imageBytes == null)
        {
            return null;
        }
        
        return await Utilities.LoadSpriteAsync(imageBytes);
    }
}

internal static class Cover
{
    private static readonly Dictionary<string, Sprite> CoverCache = new();
    
    public static async Task<Sprite> LoadCover(Song song)
    {
#if DEBUG
        Plugin.Log.Info($"Loading cover from {song.coverURL}");
        Plugin.Log.Info($"{CoverCache.Count} covers have been cached");
#endif
        if (CoverCache.TryGetValue(song.key, out Sprite cached))
        {
#if DEBUG
            Plugin.Log.Info("Cover was cached, using it");
#endif
            return cached;
        }
        
#if DEBUG
        Plugin.Log.Info("Cover not cached, fetching...");
#endif

        Sprite? cover = await RemoteImage.Fetch(song.coverURL);
        if (cover == null)
        {
#if DEBUG
            Plugin.Log.Info("Cover was null, using SongCore's default cover");
#endif
            return SongCore.Loader.defaultCoverImage;
        }

        try
        {
            Plugin.Log.Info(CoverCache.TryAdd(song.key, cover) ? "Cover was cached" : "Cover was not cached???");
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        return cover;
    }
}