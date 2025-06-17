using JetBrains.Annotations;
using ProtoBuf;

namespace DumbRequestManager.Classes;

[ProtoContract]
[UsedImplicitly]
public class BeatLeaderStarsCacheItem
{
    [ProtoMember(1)] public string Hash { get; private set; } = null!;
    [ProtoMember(2)] public string Mode { get; private set; } = null!;
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    // (wtf)
    [ProtoMember(3)] public float Stars { get; private set; }
}

[ProtoContract]
[UsedImplicitly]
public class BeatLeaderStarsCache
{
    [ProtoMember(1)] public BeatLeaderStarsCacheItem[] Items { get; private set; } = null!;
}