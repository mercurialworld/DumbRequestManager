using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaverSharp.Models;
using DumbRequestManager.Configuration;
using DumbRequestManager.Managers;
using DumbRequestManager.Utils;
using IPA.Utilities.Async;
using Newtonsoft.Json;
using SongCore.Data;
using SongDetailsCache.Structs;
using UnityEngine;

namespace DumbRequestManager.Classes;

public class CoverImageContainer
{
    public byte[]? CoverImage;
    public Sprite CoverImageSprite = SongCore.Loader.defaultCoverImage;

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

    public CoverImageContainer(BeatmapLevel beatmapLevel)
    {
        _ = GetLocal(beatmapLevel);
    }

    public CoverImageContainer(string hash)
    {
        try
        {
            _ = Get($"https://cdn.beatsaver.com/{hash.ToLower()}.jpg");
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }

    public CoverImageContainer()
    {
    }

    private async Task GetLocal(BeatmapLevel beatmapLevel)
    {
        await UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            CoverImageSprite = await beatmapLevel.previewMediaData.GetCoverSpriteAsync();
        });
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

    public QueuedSongMapMods(ReadOnlyCollection<BeatSaverSharp.Models.BeatmapDifficulty> difficulties, bool hasVivify = false)
    {
        foreach (BeatSaverSharp.Models.BeatmapDifficulty diff in difficulties)
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

    // SongDetailsCache
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
    
    // BeatSaver
    public NoncontextualizedDifficulty(BeatSaverSharp.Models.BeatmapDifficulty difficulty)
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
    
    // Base game
    private static readonly Dictionary<string, int> ModValues = new()
    {
        { "Noodle Extensions", (int)SongDetailsCache.Structs.MapMods.NoodleExtensions },
        { "Mapping Extensions", (int)SongDetailsCache.Structs.MapMods.MappingExtensions },
        { "Chroma", (int)SongDetailsCache.Structs.MapMods.Chroma },
        { "Cinema", (int)SongDetailsCache.Structs.MapMods.Cinema }
    };
    public NoncontextualizedDifficulty(BeatmapLevel level, BeatmapKey key, BeatmapBasicData mapData, Song? cachedDetails)
    {
        ExtraSongData.DifficultyData? diffData = SongCore.Collections.RetrieveDifficultyData(level, key);
        
        int mods = 0;
        if (diffData != null)
        {
            string[] requirements = diffData.additionalDifficultyData._requirements;
            string[] suggestions = diffData.additionalDifficultyData._suggestions;

            mods += requirements.Sum(requirement => ModValues.GetValueOrDefault(requirement, 0));
            mods += suggestions.Sum(suggestion => ModValues.GetValueOrDefault(suggestion, 0));

            MapMods = new QueuedSongMapMods((MapMods)mods);
        }
        
        Difficulty = key.difficulty.ToString();
        Characteristic = key.beatmapCharacteristic.serializedName;
        NoteJumpSpeed = mapData.noteJumpMovementSpeed;
        if (cachedDetails == null)
        {
            return;
        }
        
        // using SDC for now, look at StandardLevelDetailView.CalculateAndSetContentAsync
        SongDifficulty? cachedDiff = cachedDetails.Value.difficulties.First(x =>
        {
            Plugin.DebugMessage($"SDC diff name:  {x.difficulty.ToString()}");
            Plugin.DebugMessage($"Base diff name: {key.difficulty.ToString()}");
            Plugin.DebugMessage($"SDC characteristic name:  {x.characteristic.ToString()}");
            Plugin.DebugMessage($"Base characteristic name: {key.beatmapCharacteristic.serializedName}");
            
            return Normalize.GetDifficultyName(x.difficulty.ToString()) == Normalize.GetDifficultyName(key.difficulty.ToString()) &&
                   Normalize.GetCharacteristicName(x.characteristic.ToString()) == Normalize.GetCharacteristicName(key.beatmapCharacteristic.serializedName);
        });
        NotesPerSecond = cachedDiff?.notes / level.songDuration ?? 0;
        
        //Plugin.DebugMessage($"{mapData.notesCount} / {level.songDuration}");
        //NotesPerSecond = mapData.notesCount / level.songDuration;
    }
}

internal static class StringExtensions
{
    // https://gist.github.com/Caeden117/1e0187d8f7f89717974a1b558c0acc2a
    
    /*
     * Original code by @lolPants, put into an Extension class.
     *
     * The second replace is not necessary, but is there for completeness. If you need to strip TMP tags regularly,
     * you can easily remove it for added performance.
     */
    public static string StripTMPTags(this string source) => source.Replace(@"<", "<\u200B").Replace(@">", "\u200B>");
}

[JsonObject(MemberSerialization.OptIn)]
public class NoncontextualizedSong
{
    // ReSharper disable MemberCanBePrivate.Global
    [JsonProperty] [UIValue("bsrKey")]
    public string BsrKey { get; set; } = string.Empty;

    [JsonProperty] [UIValue("hash")]
    public string Hash { get; set; } = string.Empty;
    
    [JsonProperty]
    public bool IsWip { get; set; }
    
    [JsonProperty] [UIValue("user")]
    public string? User { get; set; }
    
    private string HoverStringForMapperDisplay => $"requested by {User}";
    private string HoverStringForRequesterDisplay => $"mapped by {Mapper}";
    
    [UIValue("hoverString")]
    internal string HoverString => PluginConfig.Instance.ShowRequestersInsteadOfMappers
        ? HoverStringForRequesterDisplay
        : HoverStringForMapperDisplay;

    [JsonProperty] [UIValue("title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty]
    public bool CensorTitle { get; set; }

    [JsonProperty] [UIValue("subtitle")]
    public string SubTitle { get; set; } = string.Empty;
    [JsonProperty]
    public bool CensorSubTitle { get; set; }
    
    [UIValue("displayedTitle")]
    internal string DisplayedTitle => SubTitle == string.Empty ? Title : $"{Title.StripTMPTags()} <size=75%><alpha=#AA>{SubTitle.StripTMPTags()}";
    
    [JsonProperty] [UIValue("artist")]
    public string Artist { get; set; } = string.Empty;
    [JsonProperty]
    public bool CensorArtist { get; set; }

    [JsonProperty] [UIValue("mapper")]
    public string Mapper { get; set; } = string.Empty;
    [JsonProperty]
    public bool CensorMapper { get; set; }
    
    private string SecondaryLineWithMapper => $"<alpha=#AA>{Artist.StripTMPTags()} <alpha=#FF>[<color=#CBADFF>{Mapper.StripTMPTags()}<color=#FFFFFF>]";
    private string SecondaryLineWithRequester => $"<alpha=#AA>{Artist.StripTMPTags()} <alpha=#FF>[<color=#CBFFAD>{User?.StripTMPTags()}<color=#FFFFFF>]";
    
    [UIValue("displayedSecondaryLine")]
    public string DisplayedSecondaryLine => PluginConfig.Instance.ShowRequestersInsteadOfMappers
        ? SecondaryLineWithRequester
        : SecondaryLineWithMapper;

    [JsonProperty] [UIValue("duration")]
    public uint Duration { get; set; }
    
    [JsonProperty]
    public uint[] Votes { get; set; } = [0, 0];

    [JsonProperty]
    public float Rating { get; set; }

    [JsonProperty]
    public uint UploadTime { get; set; }
    [JsonProperty]
    public uint LastUpdated { get; set; }
    
    [UIValue("durationFormatted")]
    public string DurationFormatted => $"{Mathf.FloorToInt(Duration / 60f)}:{(Duration % 60):00}";
    
    [JsonProperty] [UIValue("coverURL")]
    public string Cover { get; set; } = string.Empty;
    
    [JsonProperty]
    public bool Automapped { get; set; }
    
    [JsonProperty] public bool ScoreSaberRanked { get; set; }
    [JsonProperty] public bool BeatLeaderRanked { get; set; }
    [JsonProperty] public bool Curated { get; set; }

    [JsonProperty] public string[] Playlists { get; set; } = [];

    [JsonProperty] public int VoteStatus => string.IsNullOrEmpty(Hash) ? 0 : BeatSaverVoting.Plugin.CurrentVoteStatus(Hash) switch
    {
        BeatSaverVoting.Plugin.VoteType.Upvote => 1,
        BeatSaverVoting.Plugin.VoteType.Downvote => 2,
        _ => 0
    };
    
    [JsonProperty] public bool UsesChroma { get; set; }
    [JsonProperty] public bool UsesCinema { get; set; }
    [JsonProperty] public bool UsesMappingExtensions { get; set; }
    [JsonProperty] public bool UsesNoodleExtensions { get; set; }
    [JsonProperty] public bool UsesVivify { get; set; }
    
    [JsonProperty] public bool DataIsFromLocalMap { get; set; }
    [JsonProperty] public bool DataIsFromSongDetailsCache { get; set; }
    [JsonProperty] public bool DataIsFromBeatSaver { get; set; }

    [JsonProperty] public NoncontextualizedDifficulty[] Diffs { get; set; } = [];

    private CoverImageContainer _coverImageContainer = null!;
    public byte[]? CoverImage => _coverImageContainer.CoverImage ?? null;
    public Sprite CoverImageSprite => _coverImageContainer.CoverImageSprite;
    // ReSharper restore MemberCanBePrivate.Global
    
    // https://github.com/TheBlackParrot/DumbRequestManager/issues/7#issue-3072545753 (ty Lack)
    private static float CalculateRating(uint upvotes = 0, uint downvotes = 0)
    {
        uint votes = upvotes + downvotes;
        if (votes == 0)
        {
            return 0.5f;
        }

        float reviewScore = (float)upvotes / votes;
        float adjustment = (reviewScore - 0.5f) * Mathf.Pow(2, -(Mathf.Log10(votes / 2f + 1) / Mathf.Log10(3)));
        float rating = reviewScore - adjustment;

        return rating;
    }
    
    // SongDetailsCache
    public NoncontextualizedSong(Song? guh, bool skipCoverImage = false)
    {
        if (guh == null)
        {
            return;
        }

        Song song = guh.Value;

        DataIsFromSongDetailsCache = true;
        
        BsrKey = song.key;
        Hash = song.hash.ToLower();
        Title = song.songName;
        CensorTitle = Censor.Check(Title);
        Artist = song.songAuthorName;
        CensorArtist = Censor.Check(Artist);
        Mapper = song.levelAuthorName;
        CensorMapper = Censor.Check(Mapper);
        Duration = song.songDurationSeconds;
        Votes = [song.upvotes, song.downvotes];
        Rating = CalculateRating(song.upvotes, song.downvotes);
        UploadTime = song.uploadTimeUnix; // SDC returns the wrong value, you'll have to skip SDC if you want the real upload time
        LastUpdated = song.uploadTimeUnix;
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
    
    // BeatSaver
    public NoncontextualizedSong(Beatmap song, bool skipCoverImage = false)
    {
        DataIsFromBeatSaver = true;
        
        BsrKey = song.ID;
        Hash = song.LatestVersion.Hash.ToLower();
        Title = song.Metadata.SongName;
        CensorTitle = Censor.Check(Title);
        SubTitle = song.Metadata.SongSubName;
        CensorSubTitle = Censor.Check(SubTitle);
        Artist = song.Metadata.SongAuthorName;
        CensorArtist = Censor.Check(Artist);
        Mapper = song.Metadata.LevelAuthorName;
        CensorMapper = Censor.Check(Mapper);
        Duration = (uint)song.Metadata.Duration;
        Votes = [(uint)song.Stats.Upvotes, (uint)song.Stats.Downvotes];
        Rating = song.Stats.Score;
        LastUpdated = (uint)song.LatestVersion.CreatedAt.Subtract(DateTime.UnixEpoch).TotalSeconds;
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
    
    // Base game + supplemental SDC
    public NoncontextualizedSong(BeatmapLevel level, bool skipCoverImage = false)
    {
        DataIsFromLocalMap = true;
        
        Hash = level.levelID.Split('_').Last().Split(' ').First().ToLower();
        Title = level.songName;
        CensorTitle = Censor.Check(Title);
        SubTitle = level.songSubName;
        CensorSubTitle = Censor.Check(SubTitle);
        Artist = level.songAuthorName;
        CensorArtist = Censor.Check(Artist);
        Mapper = string.Join(", ", level.allMappers);
        CensorMapper = Censor.Check(Mapper);
        Duration = (uint)level.songDuration;

        UsesChroma = false;
        UsesCinema = false;
        UsesMappingExtensions = false;
        UsesNoodleExtensions = false;
        UsesVivify = false;
        foreach (BeatmapKey key in level.GetBeatmapKeys())
        {
            ExtraSongData.DifficultyData? diffData = SongCore.Collections.RetrieveDifficultyData(level, key);

            if (diffData == null)
            {
                continue;
            }
            
            string[] mods = diffData.additionalDifficultyData._requirements.Concat(diffData.additionalDifficultyData._suggestions).ToArray();
            
            if (mods.Any(x => x == "Chroma")) { UsesChroma = true; }
            if (mods.Any(x => x == "Cinema")) { UsesCinema = true; }
            if (mods.Any(x => x == "Noodle Extensions")) { UsesNoodleExtensions = true; }
            if (mods.Any(x => x == "Mapping Extensions")) { UsesMappingExtensions = true; }
            if (mods.Any(x => x == "Vivify")) { UsesVivify = true; }
        }

        Song? cachedDetails = SongDetailsManager.GetByMapHash(Hash); // some stuff just isn't available base game
        
        DataIsFromSongDetailsCache = cachedDetails != null;

        BsrKey = cachedDetails?.key ?? string.Empty;
        Votes = [cachedDetails?.upvotes ?? 0, cachedDetails?.downvotes ?? 0];
        Rating = cachedDetails == null ? 0.5f : CalculateRating(cachedDetails.Value.upvotes, cachedDetails.Value.downvotes);
        LastUpdated = cachedDetails?.uploadTimeUnix ?? (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // it's *probably* very new if the null check triggers
        UploadTime = cachedDetails?.uploadTimeUnix ?? (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // it's *probably* very new if the null check triggers
        Cover = cachedDetails?.coverURL ?? string.Empty;
        BeatLeaderRanked = (cachedDetails?.rankedStates & RankedStates.BeatleaderRanked) != 0;
        ScoreSaberRanked = (cachedDetails?.rankedStates & RankedStates.ScoresaberRanked) != 0;
        Curated = (cachedDetails?.uploadFlags & UploadFlags.Curated) != 0;
        
        Diffs = level.GetBeatmapKeys().Select(key =>
        {
            BeatmapBasicData? diff = level.GetDifficultyBeatmapData(key.beatmapCharacteristic, key.difficulty);
            return new NoncontextualizedDifficulty(level, key, diff, cachedDetails);
        }).ToArray();

        LevelIDComparer comparer = new();
        Playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists()
            .Where(playlist => playlist.BeatmapLevels.Contains(level, comparer)).Select(playlist => playlist.Title).ToArray();

        if (skipCoverImage)
        {
            return;
        }
        
        _coverImageContainer = PluginConfig.Instance.NeverUseLocalCoverImages ? new CoverImageContainer(Hash) : new CoverImageContainer(level);
    }

    public NoncontextualizedSong()
    {
        // blank, assume a WIP
        IsWip = true;
        _coverImageContainer = new CoverImageContainer();
    }
}

internal class LevelIDComparer : IEqualityComparer<BeatmapLevel>
{
    public bool Equals(BeatmapLevel? x, BeatmapLevel? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }
        
        return string.Equals(x.levelID, y.levelID, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(BeatmapLevel level)
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(level.levelID);
    }
}