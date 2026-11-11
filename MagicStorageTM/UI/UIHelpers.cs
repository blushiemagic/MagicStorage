using System;
using Terraria;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace MagicStorageTM.UI
{
    /// <summary>The original's default filter row (FilteringOptions.cs), minus its mod-specific/journey-only entries.</summary>
    public enum ItemFilter { All, Melee, Ranged, Magic, Summon, Ammo, Tools, Fishing, Armor, Equipment, Vanity, Potions, Placeables, Materials, Misc }
    public enum ItemSort { Default, Name, ID, Value, Quantity }

    /// <summary>Shared drawing/classification helpers (MagicStorage's default filters & sorters, simplified).</summary>
    internal static class UIHelpers
    {
        public const int Slot = 44;

        public static bool Matches(Item item, ItemFilter filter)
        {
            switch (filter)
            {
                case ItemFilter.All: return true;
                case ItemFilter.Melee: return IsWeapon(item) && item.melee;
                case ItemFilter.Ranged: return IsWeapon(item) && item.ranged;
                case ItemFilter.Magic: return IsWeapon(item) && item.magic;
                case ItemFilter.Summon: return IsWeapon(item) && (item.summon || item.sentry);
                case ItemFilter.Ammo: return IsAmmo(item);
                case ItemFilter.Tools: return IsTool(item);
                case ItemFilter.Fishing: return IsFishing(item);
                case ItemFilter.Armor: return IsArmor(item);
                case ItemFilter.Equipment: return IsEquipment(item);
                case ItemFilter.Vanity: return IsVanity(item);
                case ItemFilter.Potions: return IsPotion(item);
                case ItemFilter.Placeables: return IsPlaceable(item);
                case ItemFilter.Materials: return item.material;
                case ItemFilter.Misc:
                    return !IsWeapon(item) && !IsAmmo(item) && !IsTool(item) && !IsFishing(item) && !IsArmor(item)
                        && !IsEquipment(item) && !IsVanity(item) && !IsPotion(item) && !IsPlaceable(item) && !item.material;
            }
            return true;
        }

        private static bool IsAmmo(Item item) => item.ammo > 0 && !item.notAmmo;
        private static bool IsFishing(Item item) => item.fishingPole > 0 || item.bait > 0;
        private static bool IsEquipment(Item item) => !item.vanity && (item.accessory || item.mountType >= 0 || IsPetOrLight(item));
        private static bool IsVanity(Item item) => item.vanity || item.dye > 0;
        private static bool IsPlaceable(Item item) => item.createTile >= 0 || item.createWall > 0;

        public static int Category(Item item)
        {
            if (IsWeapon(item)) return 0;
            if (IsTool(item)) return 1;
            if (IsArmor(item)) return 2;
            if (item.accessory) return 3;
            if (item.vanity) return 4;
            if (IsPotion(item)) return 5;
            if (item.createTile >= 0 || item.createWall > 0) return 7;
            if (item.material) return 8;
            return 6;
        }

        private static bool IsTool(Item item) => item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.tileWand >= 0 && item.createTile < 0;
        private static bool IsWeapon(Item item) => item.damage > 0 && !IsTool(item) && item.ammo == 0 && !item.consumable;
        private static bool IsArmor(Item item) => (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) && !item.vanity;
        private static bool IsPotion(Item item) => item.consumable && (item.healLife > 0 || item.healMana > 0 || item.buffType > 0);
        private static bool IsPetOrLight(Item item) => item.buffType > 0 && !item.consumable && item.shoot > 0 && item.damage <= 0;

        public static string FormatCount(long count)
        {
            if (count < 100000) return count.ToString();
            if (count < 10000000) return (count / 1000) + "k";
            return (count / 1000000) + "m";
        }

        /// <summary>
        /// Draws an item icon in a size×size box. Vanilla items go through ItemSlot.DrawItemIcon so their tint
        /// (Gel, dyes…) and animation frames match the inventory; Core's DrawItem draws untinted.
        /// Core's UI renders into Main.spriteBatch, so both end up in the same batch.
        /// </summary>
        public static void DrawIcon(int itemType, int x, int y, int size, byte alpha = 255)
        {
            if (itemType <= 0) return;
            if (itemType < Terraria.ID.ItemID.Count && Main.spriteBatch != null
                && Terraria.ID.ContentSamples.ItemsByType.TryGetValue(itemType, out Item sample))
            {
                try
                {
                    var center = new Microsoft.Xna.Framework.Vector2(x + size / 2f, y + size / 2f);
                    Terraria.UI.ItemSlot.DrawItemIcon(sample, 0, Main.spriteBatch, center, 1f, size,
                        Microsoft.Xna.Framework.Color.White * (alpha / 255f));
                    return;
                }
                catch { /* fall back to Core's renderer */ }
            }
            UIRenderer.DrawItem(itemType, x, y, size, size, alpha);
        }

        /// <summary>Draws one item slot. Returns true if the mouse is over it.</summary>
        public static bool DrawSlot(int x, int y, int itemType, long count, bool highlighted = false, bool dim = false, bool favorite = false)
        {
            bool hover = WidgetInput.IsMouseOver(x, y, Slot - 2, Slot - 2);
            Color4 bg = highlighted ? UIColors.ItemActiveBg : hover ? UIColors.ItemHoverBg : UIColors.ItemBg;
            // Hover/selection is shown by the background colour (vanilla slots have no outline).
            UIRenderer.DrawRect(x, y, Slot - 2, Slot - 2, bg);
            if (itemType > 0)
            {
                DrawIcon(itemType, x + 6, y + 6, Slot - 14, (byte)(dim ? 110 : 255));
                if (count > 1)
                {
                    string s = FormatCount(count);
                    int w = TextUtil.MeasureWidth(s);
                    UIRenderer.DrawText(s, x + Slot - w - 5, y + Slot - 19, 0, 0, 0);
                    UIRenderer.DrawText(s, x + Slot - w - 6, y + Slot - 20, UIColors.Text);
                }
                if (favorite) UIRenderer.DrawText("*", x + Slot - 12, y + 1, 255, 215, 0);
            }
            return hover;
        }

        public static bool SmallButton(int x, int y, int w, string text, bool active = false)
        {
            return active
                ? Button.Draw(x, y, w, 24, text, UIColors.Accent, UIColors.ButtonHover, UIColors.AccentText)
                : Button.Draw(x, y, w, 24, text);
        }

        public static T Cycle<T>(T value, int delta) where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            int i = Array.IndexOf(values, value);
            i = ((i + delta) % values.Length + values.Length) % values.Length;
            return values[i];
        }
    }
}
