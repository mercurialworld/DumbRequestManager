using System.Collections.Generic;
using JetBrains.Annotations;
using ProtoBuf;
// ReSharper disable UnusedAutoPropertyAccessor.Local

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
[UsedImplicitly]
public class CachedMapRankValues
{
    [ProtoMember(1)] public bool IsRanked { get; private set; }
    [ProtoMember(2)] public float Stars { get; private set; }
}

[ProtoContract]
[UsedImplicitly]
public class CachedMapRankStatus
{
    [ProtoMember(1)] public CachedMapRankValues ScoreSaber { get; private set; } = null!;
    [ProtoMember(2)] public CachedMapRankValues BeatLeader { get; private set; } = null!;
}

[ProtoContract]
[UsedImplicitly]
public class CachedMapDifficulty
{
    [ProtoMember(1)] public float JumpSpeed { get; private set; }
    [ProtoMember(2)] public uint Notes { get; private set; }
    [ProtoMember(3)] public string Characteristic { get; private set; } = string.Empty;
    [ProtoMember(4)] public string Difficulty { get; private set; } = string.Empty;
    [ProtoMember(5)] public uint Mods { get; private set; }
    [ProtoMember(6)] public string Environment { get; private set; } = string.Empty;
    [ProtoMember(7)] public CachedMapRankStatus RankedStatus { get; private set; } = null!;
}

[ProtoContract]
[UsedImplicitly]
public class CachedMapVotes
{
    [ProtoMember(1)] public uint Up { get; private set; }
    [ProtoMember(2)] public uint Down { get; private set; }
}

[ProtoContract]
[UsedImplicitly]
public class CachedMap
{
    [ProtoMember(1)] public uint Key { get; private set; }
    [ProtoMember(2)] public string Hash { get; private set; } = string.Empty;
    [ProtoMember(3)] public string? SongName { get; private set; }
    [ProtoMember(4)] public string? SongSubName { get; private set; }
    [ProtoMember(5)] public string? SongAuthorName { get; private set; }
    [ProtoMember(6)] public string? LevelAuthorName { get; private set; }
    [ProtoMember(7)] public uint Duration { get; private set; }
    [ProtoMember(8)] public uint UploadTimestamp { get; private set; }
    [ProtoMember(9)] public uint LastUpdateTimestamp { get; private set; }
    [ProtoMember(10)] public uint Mods { get; private set; }
    [ProtoMember(11)] public string? Curator { get; private set; }
    [ProtoMember(12)] public CachedMapVotes Votes { get; private set; } = null!;
    [ProtoMember(13)] public CachedMapDifficulty[] Difficulties { get; private set; } = null!;
}

[ProtoContract]
public class CachedMapList
{
    [ProtoMember(1), ProtoMap] public Dictionary<string, CachedMap> Maps { get; private set; } = null!;
}