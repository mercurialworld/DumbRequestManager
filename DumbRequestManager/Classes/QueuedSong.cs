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
    [UIValue("title")] public string Title => song.songName;
    [UIValue("artist")] public string Artist => song.songAuthorName;
    [UIValue("mapper")] public string Mapper => song.levelAuthorName;
    [UIValue("duration")] public uint Duration => song.songDurationSeconds;
    [UIValue("coverURL")] public string Cover => song.coverURL;
    
    private readonly CoverImageContainer _coverImageContainer = new(song);
    [UIValue("coverImage")]
    public Sprite CoverImage => _coverImageContainer.CoverImage;
}