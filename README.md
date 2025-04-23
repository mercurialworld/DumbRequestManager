# DumbRequestManager
A map request manager for Beat Saber that abstracts out functions to an HTTP GET API, with (eventually) a WebSocket for real-time in-game queue event responses.  

Essentially meaning, any streaming bot you use (e.g. Streamer.bot, Firebot, MixItUp, etc.) now has request queue functionality -- so long as it supports making HTTP requests (at the minimum).

> [!IMPORTANT]
> This is not a mod that is instantly ready to go, unlike most other request queue managers. It *will* require manual setup on your part. Filters, limits, so on and so forth will be something you add in the bot of your choice.

## Dependencies
Currently only tested on Beat Saber versions 1.39.1 or newer. Versions of the mod for older game versions are not planned at this time.
> [!NOTE]
> Some issues involving version 3.15.3 of SongCore may intermittently pop up from time to time. **This only effects 1.40.1, 1.40.2, 1.40.3, and 1.40.4.**
>
> 1.39.1 and 1.40.0 *(which use older versions)* seem to be stable.

### Mods
- BeatSaberMarkupLanguage
- SiraUtil
- SongCore
- SongDetailsCache
- BeatSaverSharp
- BeatSaverDownloader
----

# HTTP API
By default, a simple HTTP server is started on `http://localhost:13337`. Port and IP can be changed in the mod's JSON configuration file. A game restart (a hard restart) is required for changes to take effect.
> [!CAUTION]
> It is **NOT RECOMMENDED** to let this listen on a public IP address. Unless you know what you're doing, stick to `localhost`/`127.x.x.x` IP ranges or any LAN IP range (`10.x.x.x`; `172.16.x.x - 172.31.x.x`; or `192.168.x.x`).

> [!CAUTION]
> If you are behind a firewall, it is **NOT RECOMMENDED** to forward the port you use for this HTTP server, unless you know what you are doing. Forwarding this means allowing anyone outside your local network will have full read/write access to your queue.

As this is really only a web server, you can test any of these endpoints in any web browser of your choice, while the game is running of course. 

| Endpoint   | Sub-command | Description/Example                                                                                                                                                                                                  | Returns                                            |
|------------|-------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------|
| `/query`   |             | Queries SongDetailsCache (and then BeatSaver if map data hasn't been cached yet) for map information.<br/>`/query/25f`                                                                                               | [Map Data](#map-data-type)                         |
|            | `/nocache`  | Queries BeatSaver directly, skipping SongDetailsCache.<br/>`/query/nocache/25f`                                                                                                                                      | [Map Data](#map-data-type)                         |
| `/addKey`  |             | Adds a map to the queue.<br/>User identifiers can be tacked on with a `user` query parameter. Internally this is set as a string, anything can be used so long as it's unique.<br/>`/addKey/25f?user=TheBlackParrot` | [Map Data](#map-data-type)                         |
| `/queue`   |             | Get maps currently in the queue.<br/>`/queue`                                                                                                                                                                        | (Array) [Map Data](#map-data-type)                 |
|            | `/where`    | Get user positions in the queue, along with map data from the maps the targeted user has in queue.<br/>`/queue/where/TheBlackParrot`                                                                                 | (Array) [Queue Position Data](#queue-data-type)    |
|            | `/clear`    | Clears the queue.<br/>`/queue/clear`                                                                                                                                                                                 | [Message](#message-data-type)                      |
| `/history` |             | Gets the current play session history, sorted most recent to least recent.<br/>Response limits can be tacked on with a `limit` query parameter.<br/>`/history?limit=1`                                               | (Array) [Session History Data](#history-data-type) |

## Return schemas
<a name="map-data-type"></a>
### Map data
```json
{
  "BsrKey": "1ad3b",
  "Hash": "A2EE3D6E6C82B89B10B9395BEBF47CF05F316B10",
  "User": "TheBlackParrot",
  "Title": "Megalovania",
  "SubTitle": "",
  "Artist": "Toby Fox",
  "Mapper": "Joshabi & Olaf",
  "Duration": 369,
  "Votes": [
    4951,
    236
  ],
  "Rating": 0.919891059,
  "UploadTime": 1627825945,
  "Cover": "https://cdn.beatsaver.com/a2ee3d6e6c82b89b10b9395bebf47cf05f316b10.jpg",
  "Automapped": false,
  "Diffs": [
    {
      "Difficulty": "Hard",
      "Characteristic": "Standard",
      "NoteJumpSpeed": 17,
      "NotesPerSecond": 5.929539,
      "MapMods": {
        "NoodleExtensions": false,
        "Chroma": false,
        "MappingExtensions": false,
        "Cinema": false
      },
      "ScoreSaberStars": 5.34,
      "BeatLeaderStars": 6.8
    },
    {
      "Difficulty": "Expert",
      "Characteristic": "Standard",
      "NoteJumpSpeed": 20,
      "NotesPerSecond": 7.601626,
      "MapMods": {
        "NoodleExtensions": false,
        "Chroma": false,
        "MappingExtensions": false,
        "Cinema": false
      },
      "ScoreSaberStars": 6.67,
      "BeatLeaderStars": 8.6
    },
    {
      "Difficulty": "ExpertPlus",
      "Characteristic": "Standard",
      "NoteJumpSpeed": 22,
      "NotesPerSecond": 10.1300812,
      "MapMods": {
        "NoodleExtensions": false,
        "Chroma": false,
        "MappingExtensions": false,
        "Cinema": false
      },
      "ScoreSaberStars": 9.68,
      "BeatLeaderStars": 10.43
    }
  ]
}
```

# WebSocket API
By default, a WebSocket server is started on `http://localhost:13338`, acting as a firehose (meaning it just spits out information, no input is taken into account). Port and IP can be changed in the mod's JSON configuration file. A game restart (a hard restart) is required for changes to take effect.

> [!CAUTION]
> It is **NOT RECOMMENDED** to let this listen on a public IP address. Unless you know what you're doing, stick to `localhost`/`127.x.x.x` IP ranges or any LAN IP range (`10.x.x.x`; `172.16.x.x - 172.31.x.x`; or `192.168.x.x`).

**This is only used for button-press events to avoid feature creep with other mods adding WebSocket support for other data.** You do not need to use this if you don't want to or can't use it.

## Events
Both events (`pressedPlay` and `pressedSkip`) follow the same data structure:
```json
{
  "Timestamp": 1745374880148,
  "EventType": "pressedSkip",
  "Data": [map data]
}
```

# Data structures/schema
<a name="queue-data-type"></a>
## Queue position data
```json
{
  "Spot": 1,
  "QueueItem": [map data]
}
```

<a name="history-data-type"></a>
## Session history item
```json
{
  "Timestamp": [unix timestamp],
  "HistoryItem": [map data]
}
```

<a name="message-data-type"></a>
## Message
```json
{
  "message": "Message text"
}
```