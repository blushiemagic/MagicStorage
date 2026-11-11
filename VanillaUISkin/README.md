# Vanilla UI Skin (TerrariaModder)

Makes every TerrariaModder window — Core's mod menu, Magic Storage, and any mod built on Core's UI widgets —
look like vanilla / tModLoader UI instead of TerrariaModder's flat dark boxes. Uses the game's own textures at
runtime; no Re-Logic files are included.

```
dotnet build -c Release -p:Deploy=true     # → <Terraria>\TerrariaModder\mods\vanilla-ui-skin  (game folder: build/Local.props)
```

Remove the `vanilla-ui-skin` mod folder to go back to the flat style. Nothing in Core is modified on disk.

## How it works

Core draws every box through `UIRenderer.DrawRect` using one of the semantic `UIColors`. A Harmony prefix
recognises the colour (compared to the live `UIColors`, so Core's colour-blind themes keep working) and draws
the vanilla element instead:

| Core colour | Drawn as |
|---|---|
| `PanelBg`, `DrawPanel(...)` | `UIPanel` (PanelBackground + PanelBorder, 12px corners), bg (63,82,151)×0.7, black border |
| `HeaderBg` inside `DraggablePanel.BeginDraw` | nothing (tModLoader windows have no header strip) |
| `ItemBg` / `ItemHoverBg` / `ItemActiveBg` (square) | `Inventory_Back` / brighter / `Inventory_Back14` (gold) |
| same colours, wide rows/tabs | small `UIPanel` rows (gold border when active) |
| `Button` / `ButtonHover` / `Accent` | `UIPanel` / hover (73,94,171) / hover + gold border |
| `InputBg` / `InputFocusBg` | search-box `UIPanel` / hover colour |
| `Success` (toggle on) | green `UIPanel` |
| `SectionBg` | darker inner `UIPanel` |
| `TooltipBg` | `Utils.DrawInvBG` (vanilla tooltip box) |
| `ScrollTrack` / `ScrollThumb` / slider thumbs | `Scrollbar` / `ScrollbarInner` |
| thin `Accent` / `Border` / `Divider` lines | gold / black / dark navy |

Colours it doesn't recognise are drawn by Core as before. Text already used vanilla's outlined Andy font.

Developer preview: create an empty `preview.flag` in the deployed mod folder and a window showing every widget
opens on the title screen.
