using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaverSharp.Models;
using DumbRequestManager.Managers;
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
public readonly struct QueuedSongMapMods
{
    // Vivify isn't cached in SDC yet
    [JsonProperty] public readonly bool Chroma;
    [JsonProperty] public readonly bool Cinema;
    [JsonProperty] public readonly bool MappingExtensions;
    [JsonProperty] public readonly bool NoodleExtensions;
    [JsonProperty] public readonly bool Vivify;

    public QueuedSongMapMods(MapMods mapMods, bool hasVivify = false)
    {
        Chroma = mapMods.HasFlag(MapMods.Chroma);
        Cinema = mapMods.HasFlag(MapMods.Cinema);
        MappingExtensions = mapMods.HasFlag(MapMods.MappingExtensions);
        NoodleExtensions = mapMods.HasFlag(MapMods.NoodleExtensions);
        Vivify = hasVivify;
    }

    public QueuedSongMapMods(ReadOnlyCollection<BeatmapDifficulty> difficulties, bool hasVivify = false)
    {
        foreach (BeatmapDifficulty diff in difficulties)
        {
            if (diff.Chroma) { Chroma = true; }
            if (diff.Cinema) { Cinema = true; }
            if (diff.MappingExtensions) { MappingExtensions = true; }
            if (diff.NoodleExtensions) { NoodleExtensions = true; }
        }
        if (hasVivify) { Vivify = true; } // temp
    }
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
    
    [JsonProperty] [UIValue("user")]
    public string? User { get; set; }

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
    
    [JsonProperty]
    public bool Automapped { get; set; }
    
    [JsonProperty] public bool ScoreSaberRanked { get; set; }
    [JsonProperty] public bool BeatLeaderRanked { get; set; }
    [JsonProperty] public bool Curated { get; set; }
    
    [JsonProperty] public bool UsesChroma { get; set; }
    [JsonProperty] public bool UsesCinema { get; set; }
    [JsonProperty] public bool UsesMappingExtensions { get; set; }
    [JsonProperty] public bool UsesNoodleExtensions { get; set; }
    [JsonProperty] public bool UsesVivify { get; set; }

    [JsonProperty] public NoncontextualizedDifficulty[] Diffs { get; set; } = [];

    private CoverImageContainer _coverImageContainer = null!;
    [UIValue("coverImage")]
    public byte[]? CoverImage => _coverImageContainer.CoverImage ?? null;
    // ReSharper restore MemberCanBePrivate.Global

    // sigh
    public NoncontextualizedSong(Song? guh, bool skipCoverImage = false)
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
        BeatLeaderRanked = (song.rankedStates & RankedStates.BeatleaderRanked) != 0;
        ScoreSaberRanked = (song.rankedStates & RankedStates.ScoresaberRanked) != 0;
        Curated = (song.uploadFlags & UploadFlags.Curated) != 0;
        UsesChroma = song.difficulties.Any(x => x.mods.HasFlag(MapMods.Chroma));
        UsesCinema = song.difficulties.Any(x => x.mods.HasFlag(MapMods.Cinema));
        UsesMappingExtensions = song.difficulties.Any(x => x.mods.HasFlag(MapMods.MappingExtensions));
        UsesNoodleExtensions = song.difficulties.Any(x => x.mods.HasFlag(MapMods.NoodleExtensions));
        // (SDC doesn't cache automapped maps, always false)
        Diffs = song.difficulties.Select(x => new NoncontextualizedDifficulty(x)).ToArray();
        if (!skipCoverImage)
        {
            _coverImageContainer = new CoverImageContainer(song);
        }
    }
    public NoncontextualizedSong(Beatmap song, bool skipCoverImage = false)
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
        Automapped = song.Automapper;
        BeatLeaderRanked = false; // BeatSaverSharp doesn't have BL rank tags yet
        ScoreSaberRanked = song.Ranked;
        Curated = song.BeatmapCurator != null;
        UsesChroma = song.LatestVersion.Difficulties.Any(x => x.Chroma);
        UsesCinema = song.LatestVersion.Difficulties.Any(x => x.Cinema);
        UsesMappingExtensions = song.LatestVersion.Difficulties.Any(x => x.MappingExtensions);
        UsesNoodleExtensions = song.LatestVersion.Difficulties.Any(x => x.NoodleExtensions);
        Diffs = song.LatestVersion.Difficulties.Select(x => new NoncontextualizedDifficulty(x)).ToArray();
        if (!skipCoverImage)
        {
            _coverImageContainer = new CoverImageContainer(song);
        }
    }
}