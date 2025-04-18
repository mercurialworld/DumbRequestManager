using System;
using System.Linq;
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
public readonly struct QueuedSongMapMods(MapMods mapMods)
{
    [JsonProperty] public bool NoodleExtensions => mapMods.HasFlag(MapMods.NoodleExtensions);
    [JsonProperty] public bool Chroma => mapMods.HasFlag(MapMods.Chroma);
    [JsonProperty] public bool MappingExtensions => mapMods.HasFlag(MapMods.MappingExtensions);
    [JsonProperty] public bool Cinema => mapMods.HasFlag(MapMods.Cinema);
}

[JsonObject(MemberSerialization.OptIn)]
public readonly struct QueuedSongDiff(SongDifficulty difficulty)
{
    [JsonProperty] public string Difficulty => difficulty.difficulty.ToString();
    [JsonProperty] public string Characteristic => difficulty.characteristic.ToString();
    [JsonProperty] public float NoteJumpSpeed => difficulty.njs;
    [JsonProperty] public float NotesPerSecond => difficulty.notes / (float)difficulty.song.songDurationSeconds;
    [JsonProperty] public readonly QueuedSongMapMods MapMods = new(difficulty.mods);
    [JsonProperty] public float ScoreSaberStars => difficulty.stars;
    [JsonProperty] public float BeatLeaderStars => difficulty.starsBeatleader;
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

    [JsonProperty] public readonly QueuedSongDiff[] Diffs = song.difficulties.Select(x => new QueuedSongDiff(x)).ToArray();
    
    private readonly CoverImageContainer _coverImageContainer = new(song);
    [UIValue("coverImage")]
    public byte[]? CoverImage => _coverImageContainer.CoverImage;
    // ReSharper restore MemberCanBePrivate.Global
}