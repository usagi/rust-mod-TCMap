# TCMap forked by usagi

This Rust/Carbon plugin displays Tool Cupboard (TC) locations on a map and allows you to see authorized player names when hovering over them.
This repository is a maintained fork of [**TC Map Markers** v1.1.2 by TheBandolero](https://umod.org/plugins/tc-map-markers).

## Credits

- Original plugin: **TC Map Markers**
- Original author: **TheBandolero**
- Original resource: https://umod.org/plugins/tc-map-markers
- This fork maintainer: **usagi**

## What this fork changes

- Compatibility with the latest Rust/Carbon/uMod environments
- Support for the `authorizedPlayers` type change (`ulong`)
- Marker appearance can be adjusted in the configuration file
- Radius
- Transparency
-Color (hex)
- Added an option to exclude TCs from Raidable Bases
-Code cleanup and refactoring (v1.2.0)

## Features

- Display TC markers on the map
- Display authorized player names in marker tooltips
- Toggle between admin-only and everyone-only visibility
- Ability to exclude TCs from Raidable Bases event areas

## Requirements

- Rust Dedicated Server
- Carbon (or uMod/Oxide compatible environment)
- Optional: Raidable Bases (if using the exclusion feature)

## Installation

1. Place `TCMap.cs` in the `plugins` directory
2.Reload on the server
- `c.reload TCMap`
3. Check and adjust the configuration file
- `configs/TCMap.json`

## Permissions

- `tcmap.admin`
Display markers for administrators / Use the `tcmap` chat command

Example:

-`c.grant group admin tcmap.admin`

## Commands

- `/tcmap clear`
Delete all TC markers
- `/tcmap update`
Regenerate markers
- `/tcmap showtoall`
Toggles display of all players on/off
- `/tcmap help`
Displays help

## Configuration

Configuration file: `configs/TCMap.json`

Key items:

- `Outer Circle`
  - `Enabled`
  - `Radius`
  - `Alpha`
  - `Color1 (hex)`
  - `Color2 (hex)`
- `Inner Circle`
  - `Enabled`
  - `Radius`
  - `Alpha`
  - `Color1 (hex)`
  - `Color2 (hex)`
- `Show Name Tooltip Marker`
- `Exclude Raidable Bases Cupboards` (default: `true`)

## Changelog

See the header section of the source code.

## License/Attribution

- MIT License

### Current Author

- [usagi](https://usagi.network/)

### Original AUthor

- [**TC Map Markers** v1.1.2 by TheBandolero](https://umod.org/plugins/tc-map-markers).
