using System;
using System.Globalization;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using DumbRequestManager.Utils;
using Newtonsoft.Json;
using SongDetailsCache.Structs;
using UnityEngine;

namespace DumbRequestManager.Classes;

public class CoverImageContainer
{
    public byte[]? CoverImage;

    public CoverImageContainer(Song song)
    {
        try
        {
            _ = Get(song);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }

    private async Task Get(Song song)
    {
        CoverImage = await Cover.LoadCover(song);
    }
}

[JsonObject(MemberSerialization.OptIn)]
public readonly struct QueuedSong(Song song)
{
    public Song OriginalSong => song;
    
    // ReSharper disable MemberCanBePrivate.Global
    [JsonProperty] [UIValue("bsrKey")]
    public string BsrKey => song.key;
    
    [JsonProperty] [UIValue("hash")]
    public string Hash => song.hash;
    
    [JsonProperty] [UIValue("title")]
    public string Title => song.songName;
    
    [JsonProperty] [UIValue("artist")]
    public string Artist => song.songAuthorName;
    
    [JsonProperty] [UIValue("mapper")]
    public string Mapper => song.levelAuthorName;
    
    [JsonProperty] [UIValue("duration")]
    public uint Duration => song.songDurationSeconds;
    
    [JsonProperty]
    public uint[] Votes => [song.upvotes, song.downvotes];

    [JsonProperty]
    public float Rating => song.rating;

    [JsonProperty]
    public uint UploadTime => song.uploadTimeUnix;
    
    [UIValue("durationFormatted")]
    public string DurationFormatted => $"{Mathf.FloorToInt(Duration / 60f)}:{(Duration % 60):00}";
    
    [JsonProperty] [UIValue("coverURL")]
    public string Cover => song.coverURL;
    
    private readonly CoverImageContainer _coverImageContainer = new(song);
    [UIValue("coverImage")]
    public byte[]? CoverImage => _coverImageContainer.CoverImage;
    // ReSharper restore MemberCanBePrivate.Global
}