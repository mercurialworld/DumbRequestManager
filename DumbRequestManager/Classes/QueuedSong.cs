using BeatSaberMarkupLanguage.Attributes;
using SongDetailsCache.Structs;

namespace DumbRequestManager.Classes;

public readonly struct QueuedSong(Song song)
{
    [UIValue("title")] public string Title => song.songName;
    [UIValue("artist")] public string Artist => song.songAuthorName;
    [UIValue("mapper")] public string Mapper => song.levelAuthorName;
    [UIValue("duration")] public uint Duration => song.songDurationSeconds;
    [UIValue("coverURL")] public string Cover => song.coverURL;
}