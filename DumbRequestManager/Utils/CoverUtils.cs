using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using SongDetailsCache.Structs;
using UnityEngine;

namespace DumbRequestManager.Utils;

internal static class RemoteImage
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

internal class Cover
{
    private static readonly Dictionary<string, Sprite> CoverCache = new();
    
    public static async Task<Sprite> LoadCover(Song song)
    {
        if (CoverCache.TryGetValue(song.key, out Sprite cached))
        {
            return cached;
        }

        Sprite? cover = await RemoteImage.Fetch(song.coverURL);
        if (cover == null)
        {
            return SongCore.Loader.defaultCoverImage;
        }
        
        CoverCache.Add(song.key, cover);
        return cover;
    }
}