using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using DumbRequestManager.Utils;
using SongDetailsCache.Structs;
using UnityEngine;

namespace DumbRequestManager.Classes;

public class CoverImageContainer
{
    public Sprite CoverImage = SongCore.Loader.defaultCoverImage;

    public CoverImageContainer(Song song)
    {
        _ = Get(song);
    }

    private async Task Get(Song song)
    {
        CoverImage = await Cover.LoadCover(song);
    }
}

public readonly struct QueuedSong(Song song)
{
    /* oh screw this
    private static string TimeFormatter(int x) => $"{Mathf.FloorToInt(x / 60f)}:{(x % 60):00}";
    public string TimeFormatter(string x) => TimeFormatter(int.Parse(x));
    */

    // ReSharper disable MemberCanBePrivate.Global
    [UIValue("title")] public string Title => song.songName;
    [UIValue("artist")] public string Artist => song.songAuthorName;
    [UIValue("mapper")] public string Mapper => song.levelAuthorName;
    [UIValue("duration")] public uint Duration => song.songDurationSeconds;
    [UIValue("durationFormatted")] public string DurationFormatted => $"{Mathf.FloorToInt(Duration / 60f)}:{(Duration % 60):00}";
    [UIValue("coverURL")] public string Cover => song.coverURL;
    
    private readonly CoverImageContainer _coverImageContainer = new(song);
    [UIValue("coverImage")]
    public Sprite CoverImage => _coverImageContainer.CoverImage;
    // ReSharper restore MemberCanBePrivate.Global
}