using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorageTM.Crafting;
using MagicStorageTM.Storage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace MagicStorageTM.UI
{
    /// <summary>The crafting page (MagicStorage's CraftingUIState): recipes from storage, stations from the interface.</summary>
    internal static class CraftingView
    {
        private static readonly TextInput _search = new TextInput("Search recipes...", 60);
        private static readonly ScrollView _recipeScroll = new ScrollView();
        private static ItemFilter _filter = ItemFilter.All;
        private static bool _availableOnly = true;
        private static bool _useInventory;

        private static CraftEnvironment _env;
        private static Dictionary<int, long> _counts = new Dictionary<int, long>();
        private static readonly List<Recipe> _shown = new List<Recipe>();
        private static readonly HashSet<Recipe> _available = new HashSet<Recipe>();
        private static int _builtVersion = -1, _rebuildTimer;
        private static string _builtSearch;
        private static ItemFilter _builtFilter;
        private static bool _builtAvailableOnly, _builtUseInventory;

        private static Recipe _selected;
        private static int _amount = 1;

        public static void OnOpened()
        {
            _builtVersion = -1;
            _selected = null;
            _amount = 1;
            _recipeScroll.ResetScroll();
        }

        private static void Rebuild()
        {
            Player player = Main.LocalPlayer;
            _env = CraftingEngine.BuildEnvironment(MagicUI.Access, player);
            _counts = CraftingEngine.AvailableCounts(MagicUI.Heart, player, _useInventory);
            _available.Clear();
            _shown.Clear();
            string q = (_search.Text ?? "").Trim();
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe r = Main.recipe[i];
                if (r == null || r.createItem == null || r.createItem.IsAir) continue;
                bool ok = CraftingEngine.MaxCrafts(r, _counts) >= 1 && CraftingEngine.MeetsEnvironment(r, _env, player);
                if (ok) _available.Add(r);
                if (_availableOnly && !ok) continue;
                if (!UIHelpers.Matches(r.createItem, _filter)) continue;
                if (q.Length > 0 && r.createItem.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _shown.Add(r);
            }
            _builtVersion = StorageWorld.Version;
            _builtSearch = _search.Text;
            _builtFilter = _filter;
            _builtAvailableOnly = _availableOnly;
            _builtUseInventory = _useInventory;
        }

        public static void Draw(int x, int y, int w, int h)
        {
            _search.Update();
            bool settingsChanged = _builtSearch != _search.Text || _builtFilter != _filter || _builtAvailableOnly != _availableOnly || _builtUseInventory != _useInventory;
            // Inventory contents change outside our control; refresh periodically when they matter.
            bool periodic = ++_rebuildTimer >= 30;
            if (periodic) _rebuildTimer = 0;
            if (_builtVersion != StorageWorld.Version || settingsChanged || (periodic && (_useInventory || _builtVersion < 0)))
                Rebuild();

            Player player = Main.LocalPlayer;
            int leftW = 8 * UIHelpers.Slot + 14;

            // ── toolbar: search, the original's two recipe-mode icons, then the filter icons ──
            _search.Draw(x, y, 190, 26);
            int iconY = y - 2;
            if (Icons.Button(x + 198, iconY, Icons.RecipeAvailable, _availableOnly, "Show Available Recipes")) _availableOnly = true;
            if (Icons.Button(x + 198 + Icons.Stride, iconY, Icons.RecipeAll, !_availableOnly, "Show All Recipes")) _availableOnly = false;
            int filterX = x + 198 + 2 * Icons.Stride + 10;
            for (int i = 0; i < Icons.Filters.Length; i++)
            {
                ItemFilter f = Icons.Filters[i];
                if (Icons.Button(filterX + i * Icons.Stride, iconY, Icons.For(f), _filter == f, Icons.Tip(f))) _filter = f;
            }

            // ── recipe grid (left) ──
            int gridY = y + 34;
            int stationsH = 3 * 32 + 22;
            int gridH = h - (gridY - y) - stationsH - 8;
            int cols = 8;
            int rows = (_shown.Count + cols - 1) / cols;
            _recipeScroll.Begin(x, gridY, leftW, gridH, rows * UIHelpers.Slot);
            Recipe hovered = null;
            int firstRow = Math.Max(0, _recipeScroll.ScrollOffset / UIHelpers.Slot);
            int lastRow = Math.Min(rows - 1, (_recipeScroll.ScrollOffset + gridH) / UIHelpers.Slot);
            bool inGrid = WidgetInput.IsMouseOver(x, gridY, leftW, gridH);
            for (int r = firstRow; r <= lastRow; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int i = r * cols + c;
                    if (i >= _shown.Count) break;
                    Recipe rec = _shown[i];
                    int sx = _recipeScroll.ContentX + c * UIHelpers.Slot;
                    int sy = _recipeScroll.ContentY + r * UIHelpers.Slot;
                    bool avail = _available.Contains(rec);
                    if (UIHelpers.DrawSlot(sx, sy, rec.createItem.type, rec.createItem.stack, rec == _selected, dim: !avail) && inGrid)
                        hovered = rec;
                }
            }
            _recipeScroll.End();
            if (hovered != null)
            {
                ItemTooltip.Set(hovered.createItem.type, 0, hovered.createItem.stack);
                if (WidgetInput.MouseLeftClick)
                {
                    WidgetInput.ConsumeClick();
                    _selected = hovered;
                    _amount = 1;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
            if (_shown.Count == 0)
            {
                // Two short lines so the message stays inside the recipe column.
                UIRenderer.DrawText(_availableOnly ? "Nothing can be crafted with" : "No recipes match.", x + 4, gridY + 6, UIColors.TextDim);
                if (_availableOnly) UIRenderer.DrawText("the stored items and stations.", x + 4, gridY + 26, UIColors.TextDim);
            }

            // ── station slots (bottom-left) ──
            DrawStations(x, y + h - stationsH, leftW, player);

            // ── selected recipe (right), with the "use inventory" toggle on top ──
            int detailsX = x + leftW + 10, detailsW = w - leftW - 10;
            if (UIHelpers.SmallButton(detailsX, gridY, detailsW, _useInventory ? "Use inventory: On" : "Use inventory: Off", _useInventory))
                _useInventory = !_useInventory;
            DrawDetails(detailsX, gridY + 30, detailsW, h - (gridY + 30 - y), player);
        }

        private static void DrawStations(int x, int y, int w, Player player)
        {
            UIRenderer.DrawText("Crafting stations (place items here):", x, y, UIColors.TextDim);
            if (!StorageWorld.CraftingAccesses.TryGetValue(MagicUI.Access, out var data))
                data = StorageWorld.GetCraftingAccess(MagicUI.Access);
            const int size = 32, perRow = 15;
            int top = y + 18;
            for (int i = 0; i < CraftingAccessData.MaxStations; i++)
            {
                int sx = x + (i % perRow) * (size - 2);
                int sy = top + (i / perRow) * size;
                if (sx + size > x + w + 30) continue;
                Item item = i < data.Stations.Count ? data.Stations[i] : null;
                bool hover = WidgetInput.IsMouseOver(sx, sy, size - 4, size - 4);
                UIRenderer.DrawRect(sx, sy, size - 4, size - 4, hover ? UIColors.ItemHoverBg : UIColors.ItemBg);
                if (item != null && !item.IsAir) UIHelpers.DrawIcon(item.type, sx + 3, sy + 3, size - 10);
                if (!hover) continue;
                if (item != null && !item.IsAir) ItemTooltip.Set(item.type, item.prefix, item.stack, "Click to take back");
                if (!WidgetInput.MouseLeftClick) continue;
                WidgetInput.ConsumeClick();
                if (!Main.mouseItem.IsAir)
                {
                    if (!CraftingEngine.IsValidStation(Main.mouseItem))
                    {
                        Content.Interactions.Notify("Only crafting stations (and buckets of liquid) can go here.");
                        continue;
                    }
                    if (data.Stations.Count >= CraftingAccessData.MaxStations) continue;
                    if (data.Stations.Any(s => s.type == Main.mouseItem.type)) { Content.Interactions.Notify("That station is already present."); continue; }
                    Item one = Main.mouseItem.Clone();
                    one.stack = 1;
                    one.favorited = false;
                    lock (StorageWorld.Sync) data.Stations.Add(one);
                    Main.mouseItem.stack--;
                    if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();
                    StorageWorld.MarkDirty();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                else if (item != null && !item.IsAir)
                {
                    lock (StorageWorld.Sync) data.Stations.RemoveAt(i);
                    Main.mouseItem = item;
                    StorageWorld.MarkDirty();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
            }
        }

        private static void DrawDetails(int x, int y, int w, int h, Player player)
        {
            UIRenderer.DrawRect(x - 4, y, w + 4, h, UIColors.SectionBg);
            if (_selected == null)
            {
                UIRenderer.DrawText("Select a recipe.", x + 6, y + 8, UIColors.TextDim);
                return;
            }
            Recipe r = _selected;
            int cy = y + 6;
            if (UIHelpers.DrawSlot(x + 2, cy, r.createItem.type, r.createItem.stack))
                ItemTooltip.Set(r.createItem.type, 0, r.createItem.stack);
            UIRenderer.DrawText(TextUtil.Truncate(r.createItem.Name, w - 60), x + 52, cy + 4, UIColors.GetRarityColor(r.createItem.rare));
            long max = CraftingEngine.MaxCrafts(r, _counts);
            UIRenderer.DrawTextSmall($"Can craft: {Math.Min(max, 9999)}", x + 52, cy + 24, max > 0 ? UIColors.Success : UIColors.Error);
            cy += 50;

            UIRenderer.DrawText("Ingredients:", x + 2, cy, UIColors.TextDim);
            cy += 20;
            foreach (var entry in CraftingEngine.Ingredients(r))
            {
                int icon = entry.IsRecipeGroup ? entry.RecipeGroup.GetPlaceholderItemType() : entry.itemIdOrRecipeGroup;
                string name = entry.IsRecipeGroup ? entry.RecipeGroup.GetText() : Lang.GetItemNameValue(icon);
                long have = CraftingEngine.CountFor(entry, _counts);
                UIHelpers.DrawIcon(icon, x + 4, cy, 22);
                bool over = WidgetInput.IsMouseOver(x + 2, cy, w - 4, 24);
                if (over)
                {
                    if (entry.IsRecipeGroup) ItemTooltip.SetWithName(icon, name);
                    else ItemTooltip.Set(icon);
                }
                UIRenderer.DrawText(TextUtil.Truncate($"{entry.stack}x {name}", w - 110), x + 32, cy + 3, UIColors.Text);
                string haveText = UIHelpers.FormatCount(have);
                UIRenderer.DrawText(haveText, x + w - 8 - TextUtil.MeasureWidth(haveText), cy + 3, have >= entry.stack ? UIColors.Success : UIColors.Error);
                cy += 26;
                if (cy > y + h - 150) break;
            }

            var missing = new List<string>();
            bool envOk = CraftingEngine.MeetsEnvironment(r, _env, player, missing);
            cy += 4;
            if (r.requiredTile >= 0)
            {
                string station = SafeTileName(r.requiredTile);
                bool hasStation = r.requiredTile < _env.AdjTiles.Length && _env.AdjTiles[r.requiredTile];
                UIRenderer.DrawText(TextUtil.Truncate("Station: " + station, w - 8), x + 2, cy, hasStation ? UIColors.Success : UIColors.Error);
                cy += 20;
            }
            foreach (string m in missing.Where(m => r.requiredTile < 0 || m != SafeTileName(r.requiredTile)))
            {
                UIRenderer.DrawText(TextUtil.Truncate("Requires: " + m, w - 8), x + 2, cy, UIColors.Error);
                cy += 20;
            }

            // ── amount + craft ──
            int by = y + h - 64;
            if (UIHelpers.SmallButton(x + 2, by, 28, "-")) _amount = Math.Max(1, _amount - (WidgetInput.IsShiftHeld ? 10 : 1));
            string amt = _amount.ToString();
            UIRenderer.DrawText(amt, x + 52 - TextUtil.MeasureWidth(amt) / 2, by + 4, UIColors.Text);
            if (UIHelpers.SmallButton(x + 74, by, 28, "+")) _amount = Math.Min(9999, _amount + (WidgetInput.IsShiftHeld ? 10 : 1));
            if (UIHelpers.SmallButton(x + 108, by, 60, "Max")) _amount = (int)Math.Max(1, Math.Min(9999, max));

            bool canCraft = envOk && max >= 1;
            if (Button.Draw(x + 2, by + 30, w - 8, 28, canCraft ? $"Craft {_amount}" : "Cannot craft",
                    canCraft ? UIColors.Accent : UIColors.Button, UIColors.ButtonHover, canCraft ? UIColors.AccentText : UIColors.TextDim)
                && canCraft)
            {
                int done = CraftingEngine.Craft(r, _amount, MagicUI.Heart, _env, player, _useInventory);
                if (done > 0) SoundEngine.PlaySound(SoundID.Grab);
                _builtVersion = -1;
            }
        }

        private static string SafeTileName(int tile)
        {
            try
            {
                if (tile >= TileID.Count)
                    return Tiles.CustomTiles.Get(tile)?.Name ?? "Unknown";
                return Recipe.GetRequiredTileName(tile);
            }
            catch { return "Tile " + tile; }
        }
    }
}
