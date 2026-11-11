using System.Collections.Generic;
using System.Linq;

namespace MagicStorageTM.Storage
{
    /// <summary>Storage Unit tiers. Row = sprite-sheet row in StorageUnit.png (same layout as MagicStorage).</summary>
    public sealed class UnitTier
    {
        public string Key;
        public string DisplayName;
        public int Row;
        public int Capacity;
        public string UnitItem;      // Core item name of the placeable unit
        public string UpgradeItem;   // Core item name of the upgrade that turns a lower tier into this one (null = base)
        public string[] UpgradesFrom = new string[0];
        public int Rarity;
        public int Value;

        public static readonly List<UnitTier> All = new List<UnitTier>
        {
            new UnitTier { Key = "Basic", DisplayName = "Storage Unit", Row = 0, Capacity = 40, UnitItem = "storage-unit", Rarity = 0, Value = Prices.Sell(silver: 6) },
            new UnitTier { Key = "Demonite", DisplayName = "Demonite Storage Unit", Row = 1, Capacity = 80, UnitItem = "storage-unit-demonite", UpgradeItem = "upgrade-demonite", UpgradesFrom = new[] { "Basic" }, Rarity = 1, Value = Prices.Sell(silver: 32) },
            new UnitTier { Key = "Crimtane", DisplayName = "Crimtane Storage Unit", Row = 2, Capacity = 80, UnitItem = "storage-unit-crimtane", UpgradeItem = "upgrade-crimtane", UpgradesFrom = new[] { "Basic" }, Rarity = 1, Value = Prices.Sell(silver: 32) },
            new UnitTier { Key = "Hellstone", DisplayName = "Hellstone Storage Unit", Row = 3, Capacity = 120, UnitItem = "storage-unit-hellstone", UpgradeItem = "upgrade-hellstone", UpgradesFrom = new[] { "Demonite", "Crimtane" }, Rarity = 2, Value = Prices.Sell(silver: 50) },
            new UnitTier { Key = "Hallowed", DisplayName = "Hallowed Storage Unit", Row = 4, Capacity = 160, UnitItem = "storage-unit-hallowed", UpgradeItem = "upgrade-hallowed", UpgradesFrom = new[] { "Hellstone" }, Rarity = 4, Value = Prices.Sell(gold: 1) },
            new UnitTier { Key = "BlueChlorophyte", DisplayName = "Blue Chlorophyte Storage Unit", Row = 5, Capacity = 240, UnitItem = "storage-unit-bluechlorophyte", UpgradeItem = "upgrade-bluechlorophyte", UpgradesFrom = new[] { "Hallowed" }, Rarity = 7, Value = Prices.Sell(gold: 1, silver: 60) },
            new UnitTier { Key = "Luminite", DisplayName = "Luminite Storage Unit", Row = 6, Capacity = 320, UnitItem = "storage-unit-luminite", UpgradeItem = "upgrade-luminite", UpgradesFrom = new[] { "BlueChlorophyte" }, Rarity = 10, Value = Prices.Sell(gold: 2, silver: 50) },
            new UnitTier { Key = "Terra", DisplayName = "Terra Storage Unit", Row = 7, Capacity = 640, UnitItem = "storage-unit-terra", UpgradeItem = "upgrade-terra", UpgradesFrom = new[] { "Luminite" }, Rarity = 11, Value = Prices.Sell(silver: 12) },
        };

        public static UnitTier ByRow(int row) => All.FirstOrDefault(t => t.Row == row);
        public static UnitTier ByKey(string key) => All.FirstOrDefault(t => t.Key == key);

        public bool CanUpgradeTo(UnitTier next) => next != null && next.UpgradesFrom.Contains(Key);

        public IEnumerable<UnitTier> NextTiers => All.Where(t => t.UpgradesFrom.Contains(Key));
    }

    internal static class Prices
    {
        /// <summary>Same as Terraria's Item.sellPrice: returns the buy value whose sell price is the given amount.</summary>
        public static int Sell(int platinum = 0, int gold = 0, int silver = 0, int copper = 0)
            => (copper + silver * 100 + gold * 10000 + platinum * 1000000) * 5;
    }
}
