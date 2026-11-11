using System;
using System.Collections.Generic;
using System.IO;
using MagicStorageTM.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TerrariaModder.Core.UI.Widgets;

namespace MagicStorageTM.UI
{
    /// <summary>
    /// Magic Storage's original sort / filter / recipe icon buttons (UI/NewUIButtonChoice.cs + Assets/*.png):
    /// SortButtonBackground (gold "Active" variant when selected), icon on top, faded until hovered.
    /// </summary>
    internal static class Icons
    {
        // NewUIButtonChoice.ChoiceElement alpha values
        private const float AlphaLow = 0.45f, AlphaHigh = 0.925f;

        public const int Size = 30;
        public const int Stride = Size + 1;

        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();
        private static Asset<Texture2D> _vanillaSort;

        public static readonly ItemSort[] Sorts = { ItemSort.Default, ItemSort.ID, ItemSort.Name, ItemSort.Value, ItemSort.Quantity };

        public static readonly ItemFilter[] Filters =
        {
            ItemFilter.All, ItemFilter.Melee, ItemFilter.Ranged, ItemFilter.Magic, ItemFilter.Summon, ItemFilter.Ammo,
            ItemFilter.Tools, ItemFilter.Fishing, ItemFilter.Armor, ItemFilter.Equipment, ItemFilter.Vanity,
            ItemFilter.Potions, ItemFilter.Placeables, ItemFilter.Materials, ItemFilter.Misc,
        };

        private static Texture2D Get(string name)
        {
            if (_cache.TryGetValue(name, out Texture2D t)) return t;
            t = TextureUtil.LoadPremultiplied(Main.instance.GraphicsDevice,
                Path.Combine(CustomTiles.ModFolder ?? "", "Assets", "UI", name + ".png"));
            _cache[name] = t; // null is cached too, so a missing file is only tried once
            return t;
        }

        public static Texture2D For(ItemSort sort)
        {
            switch (sort)
            {
                case ItemSort.ID: return Get("SortID");
                case ItemSort.Name: return Get("SortName");
                case ItemSort.Value: return Get("SortValue");
                case ItemSort.Quantity: return Get("SortNumber");
                default:
                    // The original uses vanilla's inventory-sort icon for "Default".
                    try
                    {
                        if (_vanillaSort == null)
                            _vanillaSort = Main.Assets.Request<Texture2D>("Images/UI/Sort_0", AssetRequestMode.ImmediateLoad);
                        return _vanillaSort.Value;
                    }
                    catch { return Get("FilterAll"); }
            }
        }

        public static string Tip(ItemSort sort)
        {
            switch (sort)
            {
                case ItemSort.ID: return "Sort by ID";
                case ItemSort.Name: return "Sort by Name";
                case ItemSort.Value: return "Sort by Value";
                case ItemSort.Quantity: return "Sort by Quantity";
                default: return "Default Sorting";
            }
        }

        public static Texture2D For(ItemFilter filter)
        {
            switch (filter)
            {
                case ItemFilter.Melee: return Get("FilterMelee");
                case ItemFilter.Ranged: return Get("FilterRanged");
                case ItemFilter.Magic: return Get("FilterMagic");
                case ItemFilter.Summon: return Get("FilterSummon");
                case ItemFilter.Ammo: return Get("FilterAmmo");
                case ItemFilter.Tools: return Get("FilterPickaxe");
                case ItemFilter.Fishing: return Get("FilterFishing");
                case ItemFilter.Armor: return Get("FilterArmor");
                case ItemFilter.Equipment: return Get("FilterEquips");
                case ItemFilter.Vanity: return Get("FilterVanity");
                case ItemFilter.Potions: return Get("FilterPotion");
                case ItemFilter.Placeables: return Get("FilterTile");
                case ItemFilter.Materials: return Get("FilterMaterials");
                case ItemFilter.Misc: return Get("FilterMisc");
                default: return Get("FilterAll");
            }
        }

        public static string Tip(ItemFilter filter)
        {
            switch (filter)
            {
                case ItemFilter.Melee: return "Filter Melee Weapons";
                case ItemFilter.Ranged: return "Filter Ranged Weapons";
                case ItemFilter.Magic: return "Filter Magic Weapons";
                case ItemFilter.Summon: return "Filter Summon Weapons";
                case ItemFilter.Ammo: return "Filter Ammo";
                case ItemFilter.Tools: return "Filter Tools";
                case ItemFilter.Fishing: return "Filter Fishing Gear";
                case ItemFilter.Armor: return "Filter Armor";
                case ItemFilter.Equipment: return "Filter Equipment";
                case ItemFilter.Vanity: return "Filter Vanity Items and Dyes";
                case ItemFilter.Potions: return "Filter Potions";
                case ItemFilter.Placeables: return "Filter Placeables";
                case ItemFilter.Materials: return "Filter Materials";
                case ItemFilter.Misc: return "Filter Miscellaneous";
                default: return "Filter All";
            }
        }

        public static Texture2D RecipeAvailable => Get("RecipeAvailable");
        public static Texture2D RecipeAll => Get("RecipeAll");

        /// <summary>Draws one icon button. Returns true when clicked.</summary>
        public static bool Button(int x, int y, Texture2D icon, bool selected, string tip)
        {
            bool hover = WidgetInput.IsMouseOver(x, y, Size, Size);
            float alpha = selected ? 1f : hover ? AlphaHigh : AlphaLow;
            SpriteBatch sb = Main.spriteBatch;
            Texture2D bg = Get(selected ? "SortButtonBackgroundActive" : "SortButtonBackground");
            if (sb != null)
            {
                if (bg != null) sb.Draw(bg, new Rectangle(x, y, Size, Size), Color.White * alpha);
                if (icon != null)
                {
                    // Icons are 30px; keep them pixel-exact when they fit, otherwise scale to the button.
                    int iw = Math.Min(icon.Width, Size), ih = Math.Min(icon.Height, Size);
                    var dest = new Rectangle(x + (Size - iw) / 2, y + (Size - ih) / 2, iw, ih);
                    sb.Draw(icon, dest, Color.White * MathHelper.Clamp(alpha * 1.3f, 0f, 1f));
                }
            }
            if (!hover) return false;
            Tooltip.Set(tip);
            if (!WidgetInput.MouseLeftClick) return false;
            WidgetInput.ConsumeClick();
            SoundEngine.PlaySound(SoundID.MenuTick);
            return true;
        }
    }
}
