# Magic Storage for TerrariaModder

An **unofficial** port of [Magic Storage](https://github.com/blushiemagic/MagicStorage) (the tModLoader mod,
v0.7.1) to [TerrariaModder](https://github.com/Inidar1/terraria-modder), for **vanilla Terraria 1.4.5.8**.

Store all your items in one network of Storage Units, access them from anywhere on the network, and craft
straight from storage, with real Magic Storage blocks in the world.

| Mod | Folder | What it is |
|---|---|---|
| **Magic Storage** | [`MagicStorageTM/`](MagicStorageTM/README.md) | The port: Storage Heart, 8 tiers of Storage Units, connectors, access points, remote/portable access, crafting interface, combined crafting stations, Shadow Diamond / Radiant Jewel drops. |
| **Vanilla UI Skin** (optional) | [`VanillaUISkin/`](VanillaUISkin/README.md) | Makes every TerrariaModder window look like vanilla / tModLoader UI (works for all TerrariaModder mods). |

> **Singleplayer only.** Magic Storage adds real custom blocks, which vanilla multiplayer can't sync.

## Requirements

- Terraria **1.4.5.8** (Windows)
- [TerrariaModder](https://github.com/Inidar1/terraria-modder) Core **0.4.x** installed, game launched with `TerrariaInjector.exe`

## Install

1. Download `magic-storage-*.zip` (and optionally `vanilla-ui-skin-*.zip`) from
   [Releases](https://github.com/rvance2/MagicStorageForTerrariaModder1.4.5/releases), or build them (see below).
2. Extract each into `<Terraria>\TerrariaModder\mods\` so you get `mods\magic-storage\` and `mods\vanilla-ui-skin\`.
3. **Back up your worlds**, then start the game with `TerrariaInjector.exe`.

### How your worlds are kept safe

- Magic Storage blocks are **never written into the vanilla `.wld` file**. They and all stored items go into a
  sidecar file next to the world (`<world>.wld.msdata`, previous version kept as `.msdata.bak`).
- The sidecar is stamped with a hash of the `.wld`. If they don't match (e.g. you restored an old world backup),
  blocks are only restored into empty spaces and you get a chat warning.
- Removing the mod: the world still loads in vanilla; the blocks just aren't there. Their contents stay in the
  sidecar and come back if you reinstall.
- If the mod can't install its save protection (e.g. after a game update), it disables block placement rather
  than risk your world.

## Building from source

Needs the .NET SDK (8 or newer), and Terraria + TerrariaModder installed.

```
copy build\Local.props.example build\Local.props   # then edit TerrariaDir to your game folder
dotnet build MagicStorageTM -c Release -p:Deploy=true
dotnet build VanillaUISkin  -c Release -p:Deploy=true
```

`-p:Deploy=true` copies the mod into your game's `TerrariaModder\mods` folder. Instead of `Local.props` you can
pass `-p:TerrariaDir="X:\path\to\Terraria"`; the default is the standard Steam folder.

`ReLogic.dll` (Re-Logic's library) is needed to compile but is **not** in this repository: the build extracts it
from your own `Terraria.exe` into the git-ignored `lib/` folder (`build/ReLogic.targets`).

To make release zips: `powershell -ExecutionPolicy Bypass -File build\pack.ps1` → `dist\`.

## Credits

- **Magic Storage**: originally by Kaylee Minsuh Kim (blushiemagic), maintained by absoluteAquarian & contributors.
  All sprites and most of the game logic come from it.
- **TerrariaModder**: the modding framework this runs on.

Not affiliated with or endorsed by Re-Logic or the Magic Storage authors. Please report problems with this port
[here](https://github.com/rvance2/MagicStorageForTerrariaModder1.4.5/issues), not to the original Magic Storage project.

## License

MIT, see [LICENSE](LICENSE) (includes Magic Storage's original MIT notice).
