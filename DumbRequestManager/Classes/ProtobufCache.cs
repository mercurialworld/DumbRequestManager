using System.IO;
using ProtoBuf;

namespace DumbRequestManager.Classes;

public enum CachedMapMods
{
    Cinema = 1,
    MappingExtensions = 2,
    Chroma = 4,
    NoodleExtensions = 8,
    Vivify = 16
};

[ProtoContract]
internal class CachedMapRankValues
{
    [ProtoMember(1)] public bool IsRanked { get; set; }
    [ProtoMember(2)] public uint Stars { get; set; }
}

[ProtoContract]
internal class CachedMapRankStatus
{
    [ProtoMember(1)] public CachedMapRankValues ScoreSaber { get; set; } = null!;
    [ProtoMember(2)] public CachedMapRankValues BeatLeader { get; set; } = null!;
}

[ProtoContract]
public class CachedMapDifficulty
{
    [ProtoMember(1)] internal float JumpSpeed { get; set; }
    [ProtoMember(2)] internal uint Notes { get; set; }
    [ProtoMember(3)] internal string Characteristic { get; set; } = string.Empty;
    [ProtoMember(4)] internal string Difficulty { get; set; } = string.Empty;
    [ProtoMember(5)] internal CachedMapMods Mods { get; set; }
    [ProtoMember(6)] internal string Environment { get; set; } = string.Empty;
    [ProtoMember(7)] internal CachedMapRankStatus RankedStatus { get; set; } = null!;
}

[ProtoContract]
internal class CachedMapVotes
{
    [ProtoMember(1)] internal uint Up { get; set; }
    [ProtoMember(2)] internal uint Down { get; set; }
}

[ProtoContract]
public class CachedMap
{
    [ProtoMember(1)] internal uint Key { get; set; }
    [ProtoMember(2)] internal string Hash { get; set; } = string.Empty;
    [ProtoMember(3)] internal string? SongName { get; set; }
    [ProtoMember(4)] internal string? SongSubName { get; set; }
    [ProtoMember(5)] internal string? SongAuthorName { get; set; }
    [ProtoMember(6)] internal string? LevelAuthorName { get; set; }
    [ProtoMember(7)] internal uint Duration { get; set; }
    [ProtoMember(8)] internal uint UploadTimestamp { get; set; }
    [ProtoMember(9)] internal uint LastUpdateTimestamp { get; set; }
    [ProtoMember(10)] internal CachedMapMods Mods { get; set; }
    [ProtoMember(11)] internal string? Curator { get; set; }
    [ProtoMember(12)] internal CachedMapVotes Votes { get; set; } = null!;
    [ProtoMember(13)] internal CachedMapDifficulty[] Difficulties { get; set; } = null!;
}

[ProtoContract]
internal class CachedMapList
{
    [ProtoMember(1)] internal CachedMap[] Maps { get; set; } = null!;
}