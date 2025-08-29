# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

# UNRELEASED (`0.6.5`)

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