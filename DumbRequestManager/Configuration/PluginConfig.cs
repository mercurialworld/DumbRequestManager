﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using JetBrains.Annotations;

// ReSharper disable RedundantDefaultMemberInitializer

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace DumbRequestManager.Configuration;

[UsedImplicitly]
internal class PluginConfig
{
    public static PluginConfig Instance { get; set; } = null!;
    
    public virtual string HttpAddress { get; set; } = "localhost";
    public virtual int HttpPort { get; set; } = 13337;
    
    public virtual string WebSocketAddress { get; set; } = "localhost";
    public virtual int WebSocketPort { get; set; } = 13338;
    
    public virtual string WebHookUrl { get; set; } = string.Empty;
    
    public virtual bool PlayAudioPreviews { get; set; } = true;
    public virtual bool PlayRemoteAudioPreviews { get; set; } = false;
    public virtual bool NeverUseLocalCoverImages { get; set; } = true;
    
    public virtual int MapDownloadTimeout { get; set; } = 300;
    public virtual bool ShowRequestersInsteadOfMappers { get; set; } = false;
    public virtual bool QueueOpenStatus { get; set; } = true;
    
    public virtual string ProtobufCacheURL { get; set; } = "https://theblackparrot.me/DumbRequestManager/cache.proto.gz";
    
    [UseConverter(typeof(ListConverter<string>))]
    public virtual List<string> WhitelistedWipDomains { get; set; } =
    [
        "discord.com",
        "discordapp.com",
        "dropbox.com",
        "catbox.moe",
        "wipbot.com",
        "hawk.quest"
    ];

    public virtual int MaximumWipSize { get; set; } = 20;
    public virtual int MaximumDecompressedWipSize { get; set; } = 50;
    
    public virtual int AssumeNewSessionAfterMinutes { get; set; } = 60;
    
    public virtual string DateFormat { get; set; } = "d MMM yyyy";
    public virtual string RequestCommandString { get; set; } = "!bsr";
    public virtual string PrimaryColor { get; set; } = "#CBADFF";
    public virtual string SecondaryColor { get; set; } = "#CBFFAD";
    public virtual string AttentionColor { get; set; } = "#FF7276";
}