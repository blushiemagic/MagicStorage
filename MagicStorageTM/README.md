# Magic Storage — TerrariaModder port (Terraria 1.4.5.8)

Unofficial port of [Magic Storage](https://github.com/blushiemagic/MagicStorage) (tModLoader 1.4.4, v0.7.1)
to [TerrariaModder](https://github.com/Inidar1/terraria-modder) Core 0.4.x on vanilla Terraria 1.4.5.8.
Singleplayer only. See the [repository README](../README.md) for install instructions and credits.

## Build & install

Set your game folder once in `build/Local.props` (copy `build/Local.props.example`), then:

```
dotnet build -c Release                     # build only
dotnet build -c Release -p:Deploy=true      # build + copy to <Terraria>\TerrariaModder\mods\magic-storage
dotnet build -c Release -p:Deploy=true -p:TerrariaDir="D:\Games\Terraria"   # or pass the folder directly
```

Launch the game with `TerrariaInjector.exe`. **Back up your worlds first.**

## What is included

| Feature | Status |
|---|---|
| Storage Heart, Storage Component, Storage Connector | ✔ real custom tiles, network by adjacency |
| Storage Units: Basic, Demonite, Crimtane, Hellstone, Hallowed, Blue Chlorophyte, Luminite, Terra | ✔ original capacities (40–640), sprite shows fullness |
| Upgrades (right-click unit, or unit + upgrade recipe) | ✔ |
| Storage Access, Remote Storage Access (+ Locator / Locator Drive linking) | ✔ |
| Storage Crafting Interface (45 station slots, liquids via buckets/sinks) | ✔ |
| Portable (Crafting) Access, 3 tiers each, pylon range rules | ✔ (link is per player + world, not per item) |
| Combined Stations 1–4, Combined Furniture Stations 1–2 | ✔ also work for vanilla crafting next to them |
| Shadow Diamond (first kill per boss) / Radiant Jewel (Moon Lord) | ✔ (pity: chance grows ⅔ per failed kill) |
| Storage UI: search, the original sort / filter icon rows, deposit/withdraw, Deposit All, Quick Stack, shift-click deposit | ✔ |
| Crafting UI: the original available/all recipe icons + filter icon row, search, ingredients have/need, craft N / Max, use inventory toggle | ✔ |
| Look: with the companion `VanillaUISkin` mod installed, windows use vanilla/tModLoader panels, slots and scrollbars | ✔ (optional) |
| Decrafting/shimmer UI, Environment Access & modules, Biome Globe, Snow Emulator, Security/Audit, Golem NPC, Selling, Crafting history, recursive crafting, Storage Deactivator/Extractor, Creative Storage Unit, Cores/Empty unit, cross-mod API, localization (English only) | ✘ not ported |

## How the tModLoader pieces were replaced

| tModLoader mechanism (original) | TerrariaModder replacement (this port) |
|---|---|
| `ModTile` + `TileObjectData` | `Tiles/CustomTiles.cs`: types at `TileID.Count`+ (754…), registered into vanilla `TileObjectData` by reflection; every array sized `TileID.Count` is grown (`Tiles/ArrayResizer.cs`) |
| `ModTile` hooks (KillTile, RightClick, MouseOver, AdjTiles…) | `Tiles/TilePatches.cs` Harmony patches on `WorldGen.TileFrameImportant/KillTile/CanKillTile`, `Player.TileInteractionsUse/MouseOver/AdjTiles` |
| Tile textures via autoload | PNG → premultiplied `Texture2D` → pre-loaded `Asset<Texture2D>` in `TextureAssets.Tile` |
| `ModTileEntity` (TEStorageHeart, TEStorageUnit, …) | `Storage/StorageWorld.cs` dictionaries keyed by the object's top-left tile |
| `TileNetworkScanner` | `Storage/Network.cs` (flood fill over edge-adjacent pieces, cached, invalidated on place/break) |
| `TagCompound` world data in `.twld` | `Tiles/WorldSaveLayer.cs` sidecar `World.wld.msdata` (see below) |
| `ModItem` × 30 | Core `ItemDefinition`s (`Content/MSItems.cs`), original sprites |
| `ModRecipe` / `RecipeGroup` | `Content/MSRecipes.cs` injects into `Main.recipe` after vanilla setup (Core's `RecipeDefinition` has no groups) |
| `GlobalNPC` drop rules | `Content/MSDrops.cs` postfix on `NPC.NPCLoot` |
| `UIState`/SerousCommonLib UI | `UI/*` immediate-mode UI on Core's `DraggablePanel` + widgets |
| `ModPlayer.ShiftClickSlot` | `UI/InventoryPatches.cs` on `ItemSlot.OverrideHover/OverrideLeftClick` |
| CraftingGUI engine | `Crafting/CraftingEngine.cs` built on vanilla 1.4.5 `Recipe` (quick lookups, `PlayerMeetsEnvironmentConditions`, `GetIngredientsForOneCraft`, `CraftItem_GrantItem`) |
| `ModPacket` netcode | none — singleplayer only (manifest says `required`, placement is refused when `netMode != 0`) |

## World saving (why your .wld stays vanilla)

* A transpiler on `WorldFile.SaveWorldTiles` hands vanilla an empty copy of any custom tile (walls, liquid,
  wires and paint on the wall are kept). The live world is never modified. If the transpiler cannot match
  the vanilla code, the mod disables block placement instead of risking the world.
* Custom tiles + storage contents + crafting stations + locator links go to `World.wld.msdata`, written after
  the `.wld` and stamped with its SHA-256. The previous sidecar is kept as `.msdata.bak` alongside vanilla's `.wld.bak`.
* On load, the sidecar is used only if its stamp matches the `.wld` (or the `.bak` stamp does). Otherwise
  (e.g. you restored a world backup) blocks are restored **only into empty spaces** and a chat warning is shown.
* Items whose mod is missing are kept verbatim (never deleted) and saved back.
* Removing the mod: the world loads fine in vanilla — Magic Storage blocks just aren't there. Their contents
  stay in the sidecar and come back when the mod is reinstalled.

## Self-test (developer)

Debug commands (Core command registry; e.g. through the DebugTools console/HTTP API):

| Command | Where | Does |
|---|---|---|
| `magic-storage.test-player` | title menu | creates a local Journey character `MSTM_Test` |
| `magic-storage.test-enter` | title menu | enters the local world file `MSTM_Test.wld` with the local character `MSTM_Test` (both matched by exact name, never by list position) |
| `magic-storage.test-build` | world file `MSTM_Test*.wld` | builds a network next to the player and checks placement, adjacency, connector framing, upgrade, breaking, capacity, full storage, deposit/withdraw, prefixes, crafting |
| `magic-storage.test-save` | `MSTM_Test*.wld` | saves the world, checks the sidecar |
| `magic-storage.test-verify` | `MSTM_Test*.wld`, after re-entering | checks the .wld had no custom tiles and everything was restored |
| `magic-storage.test-open storage\|crafting`, `test-close` | `MSTM_Test*.wld` | opens / closes the UI |

UI preview without a world: put a file `uipreview.flag` containing `storage` or `crafting` in the deployed mod
folder and the (empty) window opens on the title screen. Delete the flag afterwards.

All world-touching commands refuse to run unless the loaded world is a **local file** named `MSTM_Test….wld`
(cloud worlds and every other file are refused). Create the test world in the vanilla menu (Journey, any size,
named `MSTM_Test`) with the `MSTM_Test` character selected.
