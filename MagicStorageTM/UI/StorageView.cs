using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorageTM.Storage;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace MagicStorageTM.UI
{
    /// <summary>The storage page: search, sort, filter, deposit/withdraw (MagicStorage's StorageUIState).</summary>
    internal static class StorageView
    {
        private static readonly TextInput _search = new TextInput("Search...", 60);
        private static readonly ScrollView _scroll = new ScrollView();
        private static ItemFilter _filter = ItemFilter.All;
        private static ItemSort _sort = ItemSort.Default;

        private static List<StoredEntry> _entries = new List<StoredEntry>();
        private static int _builtVersion = -1;
        private static string _builtSearch;
        private static ItemFilter _builtFilter;
        private static ItemSort _builtSort;
        private static int _used, _total;

        // right-click hold-to-withdraw
        private static int _holdTicks;
        private static int _holdType = -1;

        public static void OnOpened()
        {
            _builtVersion = -1;
            _scroll.ResetScroll();
        }

        public static void OnClosed()
        {
            _search.Unfocus();
        }

        private static void Rebuild()
        {
            var heart = MagicUI.Heart;
            StorageOps.GetCapacity(heart, out _used, out _total);
            string q = (_search.Text ?? "").Trim();
            IEnumerable<StoredEntry> list = StorageOps.Aggregate(heart).Where(e => UIHelpers.Matches(e.Sample, _filter));
            if (q.Length > 0)
                list = list.Where(e => e.Sample.AffixName().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            switch (_sort)
            {
                case ItemSort.Name: list = list.OrderBy(e => e.Sample.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Prefix); break;
                case ItemSort.ID: list = list.OrderBy(e => e.Type).ThenBy(e => e.Prefix); break;
                case ItemSort.Value: list = list.OrderByDescending(e => e.Sample.value).ThenBy(e => e.Type); break;
                case ItemSort.Quantity: list = list.OrderByDescending(e => e.Count).ThenBy(e => e.Type); break;
                default: list = list.OrderBy(e => UIHelpers.Category(e.Sample)).ThenBy(e => e.Type).ThenBy(e => e.Prefix); break;
            }
            _entries = list.ToList();
            _builtVersion = StorageWorld.Version;
            _builtSearch = _search.Text;
            _builtFilter = _filter;
            _builtSort = _sort;
        }

        public static void Draw(int x, int y, int w, int h)
        {
            _search.Update();
            if (_builtVersion != StorageWorld.Version || _builtSearch != _search.Text || _builtFilter != _filter || _builtSort != _sort)
                Rebuild();

            Player player = Main.LocalPlayer;

            // ── toolbar (original layout: search bar, then a sort-icon row, then a filter-icon row) ──
            _search.Draw(x, y, 200, 26);
            if (UIHelpers.SmallButton(x + 206, y + 1, 116, "Deposit All")) DepositAll(player, quickStackOnly: false);
            if (UIHelpers.SmallButton(x + 328, y + 1, 116, "Quick Stack")) DepositAll(player, quickStackOnly: true);

            int sortY = y + 32;
            for (int i = 0; i < Icons.Sorts.Length; i++)
            {
                ItemSort s = Icons.Sorts[i];
                if (Icons.Button(x + i * Icons.Stride, sortY, Icons.For(s), _sort == s, Icons.Tip(s))) _sort = s;
            }
            string cap = $"Slots: {_used}/{_total}";
            Color4 capColor = _total > 0 && _used >= _total ? UIColors.Error : UIColors.TextDim;
            UIRenderer.DrawText(cap, x + w - TextUtil.MeasureWidth(cap) - 4, sortY + 7, capColor);

            int filterY = sortY + Icons.Stride;
            for (int i = 0; i < Icons.Filters.Length; i++)
            {
                ItemFilter f = Icons.Filters[i];
                if (Icons.Button(x + i * Icons.Stride, filterY, Icons.For(f), _filter == f, Icons.Tip(f))) _filter = f;
            }

            // ── grid ──
            int gridY = filterY + Icons.Stride + 4;
            int gridH = h - (gridY - y) - 18;
            int cols = Math.Max(1, (w - 14) / UIHelpers.Slot);
            int rows = (_entries.Count + cols - 1) / cols;
            _scroll.Begin(x, gridY, w, gridH, rows * UIHelpers.Slot);
            int hovered = -1;
            int firstRow = Math.Max(0, _scroll.ScrollOffset / UIHelpers.Slot);
            int lastRow = Math.Min(rows - 1, (_scroll.ScrollOffset + gridH) / UIHelpers.Slot);
            for (int r = firstRow; r <= lastRow; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int i = r * cols + c;
                    if (i >= _entries.Count) break;
                    int sx = _scroll.ContentX + c * UIHelpers.Slot;
                    int sy = _scroll.ContentY + r * UIHelpers.Slot;
                    var e = _entries[i];
                    bool inView = WidgetInput.IsMouseOver(x, gridY, w, gridH);
                    if (UIHelpers.DrawSlot(sx, sy, e.Type, e.Count, favorite: e.Favorited) && inView) hovered = i;
                }
            }
            _scroll.End();

            bool overGrid = WidgetInput.IsMouseOver(x, gridY, w, gridH);
            if (hovered >= 0)
            {
                var e = _entries[hovered];
                ItemTooltip.Set(e.Type, e.Prefix, 1, $"Stored: {e.Count}");
                HandleSlotInput(e, player);
            }
            else if (overGrid && WidgetInput.MouseLeftClick && !Main.mouseItem.IsAir)
            {
                // Click empty space with an item: deposit it.
                DepositMouse();
                WidgetInput.ConsumeClick();
            }
            if (!WidgetInput.MouseRight) { _holdTicks = 0; _holdType = -1; }

            UIRenderer.DrawTextSmall("Left: take stack  Right: take one  Shift: to inventory  Shift-click inventory: deposit",
                x, y + h - 14, UIColors.TextHint);
        }

        private static void HandleSlotInput(StoredEntry e, Player player)
        {
            if (WidgetInput.MouseLeftClick)
            {
                WidgetInput.ConsumeClick();
                if (!Main.mouseItem.IsAir)
                {
                    DepositMouse();
                    return;
                }
                int amount = Math.Max(1, e.Sample.maxStack);
                Item taken = StorageOps.Withdraw(MagicUI.Heart, e.Type, e.Prefix, amount);
                if (taken.IsAir) return;
                if (WidgetInput.IsShiftHeld)
                {
                    Item left = player.GetItem(taken, GetItemSettings.QuickTransferFromSlot);
                    if (!left.IsAir) StorageOps.Deposit(MagicUI.Heart, left);
                }
                else
                {
                    Main.mouseItem = taken;
                }
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }

            bool rightPressed = WidgetInput.MouseRightClick;
            if (rightPressed) { _holdTicks = 0; _holdType = e.Type; WidgetInput.ConsumeRightClick(); }
            bool repeat = WidgetInput.MouseRight && _holdType == e.Type && ++_holdTicks > 15 && _holdTicks % 3 == 0;
            if (rightPressed || repeat)
            {
                bool canStack = Main.mouseItem.IsAir
                    || (Main.mouseItem.type == e.Type && Main.mouseItem.prefix == e.Prefix && Main.mouseItem.stack < Main.mouseItem.maxStack);
                if (!canStack) return;
                Item one = StorageOps.Withdraw(MagicUI.Heart, e.Type, e.Prefix, 1);
                if (one.IsAir) return;
                if (Main.mouseItem.IsAir) Main.mouseItem = one;
                else Main.mouseItem.stack += one.stack;
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        private static void DepositMouse()
        {
            if (Main.mouseItem.IsAir) return;
            int before = Main.mouseItem.stack;
            StorageOps.Deposit(MagicUI.Heart, Main.mouseItem);
            if (Main.mouseItem.IsAir || Main.mouseItem.stack != before)
                SoundEngine.PlaySound(SoundID.Grab);
            else
                Interactions_Full();
        }

        private static void Interactions_Full() => Content.Interactions.Notify("The storage system is full.");

        /// <summary>Deposit every non-favorited main-inventory item (slots 10-49, plus coins/ammo for quick stack).</summary>
        public static void DepositAll(Player player, bool quickStackOnly)
        {
            HashSet<int> present = null;
            if (quickStackOnly)
                present = new HashSet<int>(StorageOps.Aggregate(MagicUI.Heart).Select(e => e.Type));
            bool any = false;
            int last = quickStackOnly ? 57 : 49;
            for (int i = 10; i <= last; i++)
            {
                Item item = player.inventory[i];
                if (item == null || item.IsAir || item.favorited) continue;
                if (present != null && !present.Contains(item.type)) continue;
                if (StorageOps.Deposit(MagicUI.Heart, item)) any = true;
            }
            if (any) SoundEngine.PlaySound(SoundID.Grab);
        }
    }
}
