using System.Collections.Generic;
using MagicStorageTM.Storage;
using Terraria;
using TerrariaModder.Core;
using TerrariaModder.Core.Assets;

namespace MagicStorageTM.Content
{
    public enum PortableKind { Storage, Crafting }

    /// <summary>Every Magic Storage item, registered through Core's custom-item system.</summary>
    public static class MSItems
    {
        public const string ModId = "magic-storage";

        private static readonly Dictionary<string, int> _types = new Dictionary<string, int>();
        private static readonly List<string> _names = new List<string>();

        public static int TypeOf(string name) => name != null && _types.TryGetValue(name, out int t) ? t : 0;
        public static bool Is(Item item, string name) => item != null && !item.IsAir && item.type == TypeOf(name);

        public static void Register(ModContext ctx)
        {
            // ── Network blocks ──
            Placeable(ctx, "storage-component", "Storage Component", "StorageComponent", MSTiles.Component.Type, 0, Prices.Sell(silver: 1),
                "The base item for creating any Magic Storage component");
            Placeable(ctx, "storage-heart", "Storage Heart", "StorageHeart", MSTiles.Heart.Type, 1, Prices.Sell(gold: 1, silver: 35),
                "The core component for any Magic Storage storage system", "Required for a storage system to function");
            Placeable(ctx, "storage-access", "Storage Access", "StorageAccess", MSTiles.StorageAccess.Type, 1, Prices.Sell(silver: 67, copper: 50),
                "Acts as another access point for viewing the stored items in a storage system");
            Placeable(ctx, "crafting-access", "Storage Crafting Interface", "CraftingAccess", MSTiles.CraftingAccess.Type, 1, Prices.Sell(gold: 1, silver: 16, copper: 25),
                "Enables crafting with the items in a storage system");
            Placeable(ctx, "remote-access", "Remote Storage Access", "RemoteAccess", MSTiles.RemoteAccess.Type, 1, Prices.Sell(gold: 1, silver: 72, copper: 50),
                "Allows remote access of a Storage Heart", "Acts as a \"storage center\" for components like the Crafting Interface",
                "Right click it while holding a Locator to link it to a Storage Heart");
            var connector = Placeable(ctx, "storage-connector", "Storage Connector", "StorageConnector", MSTiles.Connector.Type, 0, Prices.Sell(copper: 10),
                "Acts as a bridge for connecting components of Magic Storage components");
            connector.MaxStack = 999;

            foreach (UnitTier tier in UnitTier.All)
            {
                var def = Placeable(ctx, tier.UnitItem, tier.DisplayName, "StorageUnit" + (tier.Key == "Basic" ? "" : tier.Key), MSTiles.Unit.Type,
                    tier.Rarity, tier.Value,
                    "A component for storing items in a Magic Storage storage system", $"Can store up to {tier.Capacity} items");
                def.PlaceStyle = tier.Row;
                def.JourneyResearchCount = 10;

                if (tier.UpgradeItem != null)
                {
                    Register(ctx, tier.UpgradeItem, new ItemDefinition
                    {
                        DisplayName = tier.DisplayName.Replace(" Unit", " Upgrade"),
                        Tooltip = new[] { $"Upgrades Storage Unit to {tier.Capacity} capacity", "Right click a placed Storage Unit while holding this, or combine with a Storage Unit" },
                        Texture = "Assets/Items/Upgrade" + tier.Key + ".png",
                        Width = 12, Height = 12,
                        MaxStack = 99,
                        Rarity = tier.Rarity,
                        Value = UpgradeValue(tier.Key),
                        Material = true,
                        JourneyResearchCount = 10,
                    });
                }
            }

            // ── Tools ──
            Register(ctx, "locator", new ItemDefinition
            {
                DisplayName = "Locator",
                Tooltip = new[] { "Right click a Storage Heart to store its location", "Right click a Remote Storage Access to link it to the stored location" },
                Texture = "Assets/Items/Locator.png",
                Width = 28, Height = 28, MaxStack = 1, Rarity = 1, Value = Prices.Sell(gold: 1),
                ModifyTooltips = lines => AddBindingLine(lines, PortableKind.Storage),
            });
            Register(ctx, "locator-disk", new ItemDefinition
            {
                DisplayName = "Locator Drive",
                Tooltip = new[] { "Right click a Storage Heart to store its location", "Right click a Remote Storage Access to link it to the stored location", "Does not get destroyed upon use" },
                Texture = "Assets/Items/LocatorDisk.png",
                Width = 28, Height = 28, MaxStack = 1, Rarity = 10, Value = Prices.Sell(gold: 5),
                ModifyTooltips = lines => AddBindingLine(lines, PortableKind.Storage),
            });

            Portable(ctx, "portable-access-prehm", "Basic Portable Remote Storage Access", "PortableAccessPreHM", PortableKind.Storage, 3, Prices.Sell(gold: 1, silver: 50), 500, 50,
                "Linked Heart must be at most 100' from any valid pylon to gain extended reach on each pylon", "Player must be at most 1000' away from the linked Heart or pylon to access storage");
            Portable(ctx, "portable-access-hm", "Advanced Portable Remote Storage Access", "PortableAccessHM", PortableKind.Storage, 7, Prices.Sell(gold: 3), 1500, 100,
                "Linked Heart must be at most 200' from any valid pylon to gain extended reach on each pylon", "Player must be at most 3000' away from the linked Heart or pylon to access storage");
            Portable(ctx, "portable-access", "Ultimate Portable Remote Storage Access", "PortableAccess", PortableKind.Storage, 11, Prices.Sell(gold: 10), -1, -1,
                "Has an unlimited access range");
            Portable(ctx, "portable-crafting-access-prehm", "Basic Portable Remote Crafting Access", "PortableCraftingAccessPreHM", PortableKind.Crafting, 3, Prices.Sell(gold: 1, silver: 10), 500, 50,
                "Linked Crafting Interface must be at most 100' from any valid pylon to gain extended reach on each pylon", "Player must be at most 1000' away from the linked Crafting Interface or pylon");
            Portable(ctx, "portable-crafting-access-hm", "Advanced Portable Remote Crafting Access", "PortableCraftingAccessHM", PortableKind.Crafting, 7, Prices.Sell(gold: 3), 1500, 100,
                "Linked Crafting Interface must be at most 200' from any valid pylon to gain extended reach on each pylon", "Player must be at most 3000' away from the linked Crafting Interface or pylon");
            Portable(ctx, "portable-crafting-access", "Ultimate Portable Remote Crafting Access", "PortableCraftingAccess", PortableKind.Crafting, 11, Prices.Sell(gold: 10), -1, -1,
                "Has an unlimited access range");

            // ── Materials ──
            Register(ctx, "shadow-diamond", new ItemDefinition
            {
                DisplayName = "Shadow Diamond",
                Tooltip = new[] { "Traces of light still linger inside", "Dropped the first time each boss is defeated in a world" },
                Texture = "Assets/Items/ShadowDiamond.png",
                Width = 20, Height = 20, MaxStack = 99, Rarity = 1, Value = Prices.Sell(gold: 1), Material = true,
            });
            Register(ctx, "radiant-jewel", new ItemDefinition
            {
                DisplayName = "Radiant Jewel",
                Tooltip = new[] { "'Shines with a dazzling light'" },
                Texture = "Assets/Items/RadiantJewel.png",
                Width = 18, Height = 18, MaxStack = 99, Rarity = 11, Value = Prices.Sell(gold: 10), Material = true,
            });

            // ── Combined crafting stations ──
            Station(ctx, "combined-stations-1", "Combined Stations (Tier 1)", "CombinedStations1Item", MSTiles.Combined1.Type, 2, Prices.Sell(gold: 2), "Combines the functionality of several crafting stations");
            Station(ctx, "combined-stations-2", "Combined Stations (Tier 2)", "CombinedStations2Item", MSTiles.Combined2.Type, 5, Prices.Sell(gold: 10), "Combines the functionality of several crafting stations");
            Station(ctx, "combined-stations-3", "Combined Stations (Tier 3)", "CombinedStations3Item", MSTiles.Combined3.Type, 8, Prices.Sell(gold: 25), "Combines the functionality of several crafting stations");
            Station(ctx, "combined-stations-4", "Combined Stations (Final Tier)", "CombinedStations4Item", MSTiles.Combined4.Type, 10, Prices.Sell(gold: 50), "Combines the functionality of several crafting stations", "Also acts as a water, lava and honey source");
            Station(ctx, "combined-furniture-stations-1", "Combined Furniture Stations (Tier 1)", "CombinedFurnitureStations1Item", MSTiles.Furniture1.Type, 2, Prices.Sell(gold: 5), "Combines the functionality of several crafting stations for furniture");
            Station(ctx, "combined-furniture-stations-2", "Combined Furniture Stations (Tier 2)", "CombinedFurnitureStations2Item", MSTiles.Furniture2.Type, 5, Prices.Sell(gold: 15), "Combines the functionality of several crafting stations for furniture");
        }

        /// <summary>Resolve runtime item types (valid from OnContentReady).</summary>
        public static void Resolve(ModContext ctx)
        {
            foreach (string name in _names)
            {
                if (ctx.TryGetItemType(ModId, name, out int type)) _types[name] = type;
                else Log.Warn($"[Items] Could not resolve item type for {name}");
            }
            Log.Info($"[Items] Resolved {_types.Count}/{_names.Count} item types");
        }

        // ── helpers ──

        private static ItemDefinition Register(ModContext ctx, string name, ItemDefinition def)
        {
            if (!ctx.RegisterItem(name, def)) Log.Error($"[Items] Failed to register {name}");
            _names.Add(name);
            return def;
        }

        private static ItemDefinition Placeable(ModContext ctx, string name, string display, string texture, int tileType, int rarity, int value, params string[] tooltip)
        {
            return Register(ctx, name, new ItemDefinition
            {
                DisplayName = display,
                Tooltip = tooltip,
                Texture = "Assets/Items/" + texture + ".png",
                Width = 26, Height = 26,
                MaxStack = 99,
                UseStyle = 1, UseTime = 10, UseAnimation = 15, AutoReuse = true,
                Consumable = true,
                CreateTile = tileType,
                Rarity = rarity,
                Value = value,
                JourneyResearchCount = 3,
            });
        }

        private static void Station(ModContext ctx, string name, string display, string texture, int tileType, int rarity, int value, params string[] tooltip)
        {
            var def = Placeable(ctx, name, display, texture, tileType, rarity, value, tooltip);
            def.Width = 30;
            def.Height = 30;
            def.JourneyResearchCount = 1;
        }

        private static void Portable(ModContext ctx, string name, string display, string texture, PortableKind kind, int rarity, int value, int playerRangeTiles, int pylonRangeTiles, params string[] extra)
        {
            var tooltip = new List<string>
            {
                kind == PortableKind.Storage ? "Right click a Storage Heart to link it" : "Right click a Storage Crafting Interface to link it",
                kind == PortableKind.Storage ? "Use item to access your storage" : "Use item to access your crafting",
            };
            tooltip.AddRange(extra);
            Register(ctx, name, new ItemDefinition
            {
                DisplayName = display,
                Tooltip = tooltip.ToArray(),
                Texture = "Assets/Items/" + texture + ".png",
                Width = 28, Height = 28,
                MaxStack = 1,
                UseStyle = 1, UseTime = 28, UseAnimation = 28,
                Rarity = rarity,
                Value = value,
                OnUse = player => Interactions.UsePortable(player as Player, kind, playerRangeTiles, pylonRangeTiles),
                ModifyTooltips = lines => AddBindingLine(lines, kind),
            });
        }

        private static int UpgradeValue(string key)
        {
            switch (key)
            {
                case "Demonite":
                case "Crimtane": return Prices.Sell(silver: 32);
                case "Hellstone":
                case "Hallowed": return Prices.Sell(silver: 40);
                case "BlueChlorophyte": return Prices.Sell(gold: 1);
                case "Luminite": return Prices.Sell(gold: 1, silver: 50);
                case "Terra": return Prices.Sell(gold: 10);
                default: return 0;
            }
        }

        private static void AddBindingLine(List<string> lines, PortableKind kind)
        {
            if (Main.gameMenu || Main.LocalPlayer == null) return;
            var b = StorageWorld.GetBindings(Main.LocalPlayer);
            var p = kind == PortableKind.Storage ? b.StorageHeart : b.CraftingAccess;
            lines.Add(p.X >= 0 ? $"Set to: {Interactions.DescribeLocation(p)}" : "Currently not set to any location");
        }
    }
}
