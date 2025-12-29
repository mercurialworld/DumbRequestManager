# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

# UNRELEASED

## Changes
- Code structure changes
  - all routes are now in their own separate static handler, woo! HTTPApi is no longer a 600+ line long file.

## Additions
- Add endpoint to remove a map from queue given the map's BSR key/ID/whatever they call it
  - `/removeKey/:bsr`

# `0.6.7`

## Changes
- Update, once again, WIPBot code parsing (https://github.com/Danielduel/wipbot/releases/tag/1.21.0)
  - hawk and daniel have talked about a standardization and it takes a big weight off my shoulders! yay
  - if a code starts with 0, it's from wipbot.com; if it starts with 8/9, it's from wip.hawk.quest

# `0.6.6`

## Changes
- Make AttentionButton a lot more visible
  - changes the color of the pro mode icon for now. trying to find a way to change the color of the background because that tends to be more visible
  - there's now an option to change the color of this, different from the primary/secondary colors (`AttentionColor`)
- Add endpoint for queue status
  - `/queue/status`: returns an object with whether the queue is open or not

## Fixes
- Fix queue status not being consistent on (soft) restart
  - i say fix but in all honesty it's just me adding yet another value to the config

# `0.6.5`

## Changes
- Moved version checker over to [my personal API](https://rustlang.pocha.moe/pochamoe-api)
    - for now, it only works with this mod, but if anyone wants to maintain another of Parrot's mods and wants to make sure people are keeping up-to-date, please contact me! My Discord is `mercurial_world`.
- Add new domain to the `WhitelistedWipDomains` configuration option
    - https://wip.hawk.quest namely
- Change WIP parsing to add support for https://wip.hawk.quest codes by themselves
- WIP description now shows the domain that the WIP came from
    - sanity check!

# `0.6.4`

## Fixes
- Fix markup error on map ID primary color highlight in song download popup
- Fix primary color gradients not reflecting changes to the primary color after a soft restart
    - this *might* not have fixed much, actually. probably only necessary for me.

## Changes
- Removed workaround for wipbot.com, as it's no longer necessary (see this [comment](https://github.com/Danielduel/wipbot-website/issues/1#issuecomment-3215786715))
- Edited README.md to further specify the data required in `POST /addWip` and add the `IsWip` map parameter
    - it's just like how i had to dig into the code to find that DataPuller's MapData message has an `InLevel` parameter