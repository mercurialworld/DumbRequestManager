using System;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaverSharp.Models;
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
            _ = Get(song.coverURL);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }
    
    public CoverImageContainer(Beatmap song)
    {
        try
        {
            _ = Get(song.LatestVersion.CoverURL);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }

    private async Task Get(string url)
    {
        CoverImage = await Cover.LoadCover(url);
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
public class NoncontextualizedDifficulty
{
    [JsonProperty] public string Difficulty { get; set; }
    [JsonProperty] public string Characteristic { get; set; }
    [JsonProperty] public float NoteJumpSpeed { get; set; }
    [JsonProperty] public float NotesPerSecond { get; set; }
    [JsonProperty] public QueuedSongMapMods MapMods { get; set; }
    [JsonProperty] public float ScoreSaberStars { get; set; }
    [JsonProperty] public float BeatLeaderStars { get; set; }

    public NoncontextualizedDifficulty(SongDifficulty difficulty)
    {
        Difficulty = difficulty.difficulty.ToString();
        Characteristic = difficulty.characteristic.ToString();
        NoteJumpSpeed = difficulty.njs;
        NotesPerSecond = difficulty.notes / (float)difficulty.song.songDurationSeconds;
        MapMods = new QueuedSongMapMods(difficulty.mods);
        ScoreSaberStars = difficulty.stars;
        BeatLeaderStars = difficulty.starsBeatleader;
    }
    
    public NoncontextualizedDifficulty(BeatmapDifficulty difficulty)
    {
        Difficulty = difficulty.Difficulty.ToString();
        Characteristic = difficulty.Characteristic.ToString();
        NoteJumpSpeed = difficulty.NJS;
        NotesPerSecond = (float)difficulty.NPS;
        int mods = (difficulty.NoodleExtensions ? (int)SongDetailsCache.Structs.MapMods.NoodleExtensions : 0) +
                   (difficulty.MappingExtensions ? (int)SongDetailsCache.Structs.MapMods.MappingExtensions : 0) +
                   (difficulty.Chroma ? (int)SongDetailsCache.Structs.MapMods.Chroma : 0) +
                   (difficulty.Cinema ? (int)SongDetailsCache.Structs.MapMods.Cinema : 0);
        MapMods = new QueuedSongMapMods((MapMods)mods);
        ScoreSaberStars = difficulty.Stars ?? 0;
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class NoncontextualizedSong
{
    // ReSharper disable MemberCanBePrivate.Global
    [JsonProperty] [UIValue("bsrKey")]
    public string BsrKey { get; set; } = string.Empty;

    [JsonProperty] [UIValue("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonProperty] [UIValue("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty] [UIValue("subtitle")]
    public string SubTitle { get; set; } = string.Empty;
    
    [JsonProperty] [UIValue("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonProperty] [UIValue("mapper")]
    public string Mapper { get; set; } = string.Empty;

    [JsonProperty] [UIValue("duration")]
    public uint Duration { get; set; }
    
    [JsonProperty]
    public uint[] Votes { get; set; } = [0, 0];

    [JsonProperty]
    public float Rating { get; set; }

    [JsonProperty]
    public uint UploadTime { get; set; }
    
    [UIValue("durationFormatted")]
    public string DurationFormatted => $"{Mathf.FloorToInt(Duration / 60f)}:{(Duration % 60):00}";
    
    [JsonProperty] [UIValue("coverURL")]
    public string Cover { get; set; } = string.Empty;

    [JsonProperty] public NoncontextualizedDifficulty[] Diffs { get; set; } = [];

    private CoverImageContainer _coverImageContainer = null!;
    [UIValue("coverImage")]
    public byte[]? CoverImage => _coverImageContainer.CoverImage;
    // ReSharper restore MemberCanBePrivate.Global

    // sigh
    public NoncontextualizedSong(Song? guh)
    {
        if (guh == null)
        {
            return;
        }

        Song song = guh.Value;
        
        BsrKey = song.key;
        Hash = song.hash;
        Title = song.songName;
        Artist = song.songAuthorName;
        Mapper = song.levelAuthorName;
        Duration = song.songDurationSeconds;
        Votes = [song.upvotes, song.downvotes];
        Rating = song.rating;
        UploadTime = song.uploadTimeUnix;
        Cover = song.coverURL;
        Diffs = song.difficulties.Select(x => new NoncontextualizedDifficulty(x)).ToArray();
        _coverImageContainer = new CoverImageContainer(song);
    }
    public NoncontextualizedSong(Beatmap song)
    {
        BsrKey = song.ID;
        Hash = song.LatestVersion.Hash;
        Title = song.Metadata.SongName;
        SubTitle = song.Metadata.SongSubName;
        Artist = song.Metadata.SongAuthorName;
        Mapper = song.Metadata.LevelAuthorName;
        Duration = (uint)song.Metadata.Duration;
        Votes = [(uint)song.Stats.Upvotes, (uint)song.Stats.Downvotes];
        Rating = song.Stats.Score;
        UploadTime = (uint)song.Uploaded.Subtract(DateTime.UnixEpoch).TotalSeconds;
        Cover = song.LatestVersion.CoverURL;
        Diffs = song.LatestVersion.Difficulties.Select(x => new NoncontextualizedDifficulty(x)).ToArray();
        _coverImageContainer = new CoverImageContainer(song);
    }
}