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
using UnityEngine;

namespace DumbRequestManager.Classes;

public class CoverImageContainer
{
    public byte[]? CoverImage;
    public Sprite CoverImageSprite = SongCore.Loader.defaultCoverImage;

    public CoverImageContainer(CachedMap song)
    {
        try
        {
            _ = Get($"https://cdn.beatsaver.com/{song.Hash}.jpg");
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
    [JsonProperty] public readonly bool Chroma;
    [JsonProperty] public readonly bool Cinema;
    [JsonProperty] public readonly bool MappingExtensions;
    [JsonProperty] public readonly bool NoodleExtensions;
    [JsonProperty] public readonly bool Vivify;

    public QueuedSongMapMods(CachedMapMods mapMods)
    {
        Chroma = mapMods.HasFlag(CachedMapMods.Chroma);
        Cinema = mapMods.HasFlag(CachedMapMods.Cinema);
        MappingExtensions = mapMods.HasFlag(CachedMapMods.MappingExtensions);
        NoodleExtensions = mapMods.HasFlag(CachedMapMods.NoodleExtensions);
        Vivify = mapMods.HasFlag(CachedMapMods.Vivify);
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

    // Local cache
    public NoncontextualizedDifficulty(CachedMapDifficulty difficulty, CachedMap map)
    {
        Difficulty = difficulty.Difficulty;
        Characteristic = difficulty.Characteristic;
        NoteJumpSpeed = difficulty.JumpSpeed;
        NotesPerSecond = difficulty.Notes / (float)map.Duration;
        MapMods = new QueuedSongMapMods((CachedMapMods)difficulty.Mods);
        ScoreSaberStars = difficulty.RankedStatus.ScoreSaber.IsRanked ? difficulty.RankedStatus.ScoreSaber.Stars : 0;
        BeatLeaderStars = difficulty.RankedStatus.BeatLeader.Stars;
    }
    
    // BeatSaver
    public NoncontextualizedDifficulty(BeatSaverSharp.Models.BeatmapDifficulty difficulty)
    {
        Difficulty = difficulty.Difficulty.ToString();
        Characteristic = difficulty.Characteristic.ToString();
        NoteJumpSpeed = difficulty.NJS;
        NotesPerSecond = (float)difficulty.NPS;
        int mods = (difficulty.NoodleExtensions ? (int)CachedMapMods.NoodleExtensions : 0) +
                   (difficulty.MappingExtensions ? (int)CachedMapMods.MappingExtensions : 0) +
                   (difficulty.Chroma ? (int)CachedMapMods.Chroma : 0) +
                   (difficulty.Cinema ? (int)CachedMapMods.Cinema : 0);
        // vivify would go here, i should put a little more pressure on getting BS# updated i think
        MapMods = new QueuedSongMapMods((CachedMapMods)mods);
        ScoreSaberStars = difficulty.Stars ?? 0;
        // same with beatleader stars
    }
    
    // Base game
    private static readonly Dictionary<string, int> ModValues = new()
    {
        { "Noodle Extensions", (int)CachedMapMods.NoodleExtensions },
        { "Mapping Extensions", (int)CachedMapMods.MappingExtensions },
        { "Chroma", (int)CachedMapMods.Chroma },
        { "Cinema", (int)CachedMapMods.Cinema },
        { "Vivify", (int)CachedMapMods.Vivify }
    };
    public NoncontextualizedDifficulty(BeatmapLevel level, BeatmapKey key, BeatmapBasicData mapData, CachedMap? cachedDetails)
    {
        ExtraSongData.DifficultyData? diffData = SongCore.Collections.RetrieveDifficultyData(level, key);
        
        int mods = 0;
        if (diffData != null)
        {
            string[] requirements = diffData.additionalDifficultyData._requirements;
            string[] suggestions = diffData.additionalDifficultyData._suggestions;

            mods += requirements.Sum(requirement => ModValues.GetValueOrDefault(requirement, 0));
            mods += suggestions.Sum(suggestion => ModValues.GetValueOrDefault(suggestion, 0));

            MapMods = new QueuedSongMapMods((CachedMapMods)mods);
        }
        
        Difficulty = key.difficulty.ToString();
        Characteristic = key.beatmapCharacteristic.serializedName;
        NoteJumpSpeed = mapData.noteJumpMovementSpeed;
        if (cachedDetails == null)
        {
            NotesPerSecond = mapData.notesCount / level.songDuration;
            return;
        }

        CachedMapDifficulty? cachedDiff;
        try
        {
            cachedDiff = cachedDetails.Difficulties
                .First(x => Normalize.GetDifficultyName(x.Difficulty) == Normalize.GetDifficultyName(key.difficulty.ToString()) &&
                                             Normalize.GetCharacteristicName(x.Characteristic) == Normalize.GetCharacteristicName(key.beatmapCharacteristic.serializedName));
        }
        catch (InvalidOperationException)
        {
            cachedDiff = null;
        }

        BeatLeaderStars = cachedDiff?.RankedStatus.BeatLeader.Stars ?? 0;
        NotesPerSecond = cachedDiff?.Notes / level.songDuration ?? 0;
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
    
    [JsonProperty] public string? Service { get; set; }
    
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
    [JsonProperty]
    public bool MetadataHasSplicedCensor { get; set; }
    
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
    [JsonProperty] public string CuratorName { get; set; } = string.Empty;

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
    [JsonProperty] public bool DataIsFromLocalCache { get; set; }
    [JsonProperty] public bool DataIsFromBeatSaver { get; set; }
    
    [JsonProperty] public bool HasPlayed => SessionHistoryManager.SessionHistory.Values.Select(x => x.Hash).Contains(Hash);

    [JsonProperty] public bool Blacklisted => BlacklistManager.ContainsKey(BsrKey);

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
    
    // Local cache
    public NoncontextualizedSong(CachedMap? song, bool skipCoverImage = false)
    {
        if (song == null)
        {
            return;
        }

        DataIsFromLocalCache = true;
        
        BsrKey = song.Key.ToString("x");
        Hash = song.Hash;
        Title = song.SongName ?? string.Empty;
        CensorTitle = Censor.Check(Title);
        SubTitle = song.SongSubName ?? string.Empty;
        CensorSubTitle = Censor.Check(SubTitle);
        Artist = song.SongAuthorName ?? string.Empty;
        CensorArtist = Censor.Check(Artist);
        Mapper = song.LevelAuthorName ?? string.Empty;
        CensorMapper = Censor.Check(Mapper);
        MetadataHasSplicedCensor = Censor.Check([Title, SubTitle, Artist, Mapper]);
        Duration = song.Duration;
        Votes = [song.Votes.Up, song.Votes.Down];
        Rating = CalculateRating(song.Votes.Up, song.Votes.Down);
        UploadTime = song.UploadTimestamp;
        LastUpdated = song.LastUpdateTimestamp;
        Cover = $"https://cdn.beatsaver.com/{song.Hash}.jpg";
        BeatLeaderRanked = song.Difficulties.Any(x => x.RankedStatus.BeatLeader.IsRanked);
        ScoreSaberRanked = song.Difficulties.Any(x => x.RankedStatus.ScoreSaber.IsRanked);
        Curated = song.Curator != null;
        CuratorName = song.Curator ?? string.Empty;
        UsesChroma = ((CachedMapMods)song.Mods).HasFlag(CachedMapMods.Chroma);
        UsesCinema = ((CachedMapMods)song.Mods).HasFlag(CachedMapMods.Cinema);
        UsesMappingExtensions = ((CachedMapMods)song.Mods).HasFlag(CachedMapMods.MappingExtensions);
        UsesNoodleExtensions = ((CachedMapMods)song.Mods).HasFlag(CachedMapMods.NoodleExtensions);
        UsesVivify = ((CachedMapMods)song.Mods).HasFlag(CachedMapMods.Vivify);
        // (Local cache doesn't cache automapped maps, always false)
        Diffs = song.Difficulties.Select(x => new NoncontextualizedDifficulty(x, song)).ToArray();
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
        MetadataHasSplicedCensor = Censor.Check([Title, SubTitle, Artist, Mapper]);
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
        CuratorName = song.BeatmapCurator?.Name ?? string.Empty;
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
    
    // Base game + supplemental cache values
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
        MetadataHasSplicedCensor = Censor.Check([Title, SubTitle, Artist, Mapper]);
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

        CachedMap? cachedDetails = MapCacheManager.Instance?.GetMapByHash(Hash);
        if (cachedDetails != null)
        {
            DataIsFromLocalCache = true;

            BsrKey = cachedDetails.Key.ToString("x") ?? string.Empty;
            Votes = [cachedDetails.Votes.Up, cachedDetails.Votes.Down];
            Rating = CalculateRating(cachedDetails.Votes.Up, cachedDetails.Votes.Down);
            LastUpdated = cachedDetails.LastUpdateTimestamp;
            UploadTime = cachedDetails.UploadTimestamp;
            Cover = $"https://cdn.beatsaver.com/{cachedDetails.Hash}.jpg";
            BeatLeaderRanked = cachedDetails.Difficulties.Any(x => x.RankedStatus.BeatLeader.IsRanked);
            ScoreSaberRanked = cachedDetails.Difficulties.Any(x => x.RankedStatus.ScoreSaber.IsRanked);
            Curated = cachedDetails.Curator != null;
            CuratorName = cachedDetails.Curator ?? string.Empty;
        }
        else
        {
            Beatmap? beatsaverDetails = Task.Run(() => SongDetailsManager.GetDirectByHash(Hash)).GetAwaiter().GetResult();
            if (beatsaverDetails != null)
            {
                DataIsFromBeatSaver = true;

                BsrKey = beatsaverDetails.ID;
                Votes = [(uint)beatsaverDetails.Stats.Upvotes, (uint)beatsaverDetails.Stats.Downvotes];
                Rating = beatsaverDetails.Stats.Score;
                LastUpdated = (uint)beatsaverDetails.LatestVersion.CreatedAt.Subtract(DateTime.UnixEpoch).TotalSeconds;
                UploadTime = (uint)beatsaverDetails.Uploaded.Subtract(DateTime.UnixEpoch).TotalSeconds;
                Cover = beatsaverDetails.LatestVersion.CoverURL;
                BeatLeaderRanked = false; // BeatSaverSharp doesn't have BL rank tags yet
                ScoreSaberRanked = beatsaverDetails.Ranked;
                Curated = beatsaverDetails.BeatmapCurator != null;
                CuratorName = beatsaverDetails.BeatmapCurator?.Name ?? string.Empty;
            }
        }

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