using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorageTM.Storage;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace MagicStorageTM.Content
{
    /// <summary>
    /// MagicStorage's recipes and recipe groups, added straight into Main.recipe once vanilla recipe setup
    /// and our tile setup are both done (Core's RecipeDefinition has no recipe-group support).
    /// </summary>
    public static class MSRecipes
    {
        private static bool _done;
        private static RecipeGroup AnyDiamond, AnySilverBar, AnyChest, AnyWorkBench, AnyPreHmAnvil, AnyBottle, AnySink, AnyTable,
            AnyCookingPot, AnyHmAnvil, AnyHmFurnace, AnyBookcase, AnyDemoniteBar, AnyMythrilBar;

        private struct Ing
        {
            public int Type;
            public RecipeGroup Group;
            public int Stack;
        }

        private static Ing I(int type, int stack = 1) => new Ing { Type = type, Stack = stack };
        private static Ing I(string modItem, int stack = 1) => new Ing { Type = MSItems.TypeOf(modItem), Stack = stack };
        private static Ing G(RecipeGroup group, int stack = 1) => new Ing { Group = group, Stack = stack };

        public static void Inject()
        {
            if (_done) return;
            if (Recipe.numRecipes <= 0) { Log.Warn("[Recipes] Vanilla recipes not set up yet"); return; }
            if (MSItems.TypeOf("storage-component") <= 0) { Log.Error("[Recipes] Item types not resolved; recipes skipped"); return; }
            _done = true;
            try
            {
                RegisterGroups();
                int before = Recipe.numRecipes;
                AddAll();
                Recipe.UpdateWhichItemsAreMaterials();
                Recipe.UpdateWhichItemsAreCrafted();
                Log.Info($"[Recipes] Added {Recipe.numRecipes - before} recipes");
            }
            catch (Exception ex)
            {
                Log.Error("[Recipes] Failed to add recipes", ex);
            }
        }

        private static int[] VanillaItemsWhere(int iconic, Func<Item, bool> pred)
        {
            var ids = new List<int>();
            for (int id = 1; id < ItemID.Count; id++)
            {
                if (!ContentSamples.ItemsByType.TryGetValue(id, out Item item) || item == null || item.IsAir) continue;
                if (pred(item)) ids.Add(id);
            }
            ids.Remove(iconic);
            ids.Insert(0, iconic);
            return ids.ToArray();
        }

        private static RecipeGroup Group(string text, params int[] items) => new RecipeGroup(() => "Any " + text, items).Register();

        private static void RegisterGroups()
        {
            AnyDiamond = Group(Lang.GetItemNameValue(ItemID.Diamond), ItemID.Diamond, MSItems.TypeOf("shadow-diamond"));
            AnySilverBar = Group(Lang.GetItemNameValue(ItemID.SilverBar), ItemID.SilverBar, ItemID.TungstenBar);
            AnyChest = Group("Chest", VanillaItemsWhere(ItemID.Chest, i => i.createTile >= 0 && i.createTile < TileID.Count && TileID.Sets.BasicChest[i.createTile]));
            AnyWorkBench = Group(Lang.GetItemNameValue(ItemID.WorkBench), VanillaItemsWhere(ItemID.WorkBench, i => i.createTile == TileID.WorkBenches));
            AnyPreHmAnvil = Group(Lang.GetItemNameValue(ItemID.IronAnvil), ItemID.IronAnvil, ItemID.LeadAnvil);
            AnyBottle = Group(Lang.GetItemNameValue(ItemID.Bottle), ItemID.Bottle, ItemID.PinkVase, ItemID.Mug, ItemID.DynastyCup, ItemID.WineGlass, ItemID.HoneyCup, ItemID.SteampunkCup);
            AnySink = Group(Lang.GetItemNameValue(ItemID.MetalSink), VanillaItemsWhere(ItemID.MetalSink, i => i.createTile == TileID.Sinks));
            AnyTable = Group(Lang.GetItemNameValue(ItemID.WoodenTable), VanillaItemsWhere(ItemID.WoodenTable, i => i.createTile == TileID.Tables || i.createTile == TileID.Tables2));
            AnyCookingPot = Group(Lang.GetItemNameValue(ItemID.CookingPot), ItemID.CookingPot, ItemID.Cauldron);
            AnyHmAnvil = Group(Lang.GetItemNameValue(ItemID.MythrilAnvil), ItemID.MythrilAnvil, ItemID.OrichalcumAnvil);
            AnyHmFurnace = Group(Lang.GetItemNameValue(ItemID.AdamantiteForge), ItemID.AdamantiteForge, ItemID.TitaniumForge);
            AnyBookcase = Group(Lang.GetItemNameValue(ItemID.Bookcase), VanillaItemsWhere(ItemID.Bookcase, i => i.createTile == TileID.Bookcases));
            AnyDemoniteBar = Group(Lang.GetItemNameValue(ItemID.DemoniteBar), ItemID.DemoniteBar, ItemID.CrimtaneBar);
            AnyMythrilBar = Group(Lang.GetItemNameValue(ItemID.MythrilBar), ItemID.MythrilBar, ItemID.OrichalcumBar);
        }

        private static void AddAll()
        {
            RecipeGroup wood = RecipeGroups.Wood, iron = RecipeGroups.IronBar;
            int bench = TileID.WorkBenches, anvil = TileID.Anvils, hmAnvil = TileID.MythrilAnvil, lunar = TileID.LunarCraftingStation;

            Add("storage-component", 1, bench, G(wood, 10), G(iron, 2));
            Add("storage-connector", 16, bench, G(wood, 16), G(iron, 1));
            Add("storage-heart", 1, bench, I("storage-component"), G(AnyDiamond, 2), I(ItemID.Emerald, 3));
            Add("storage-unit", 1, bench, I("storage-component"), G(AnyChest), G(AnySilverBar, 10));
            Add("storage-access", 1, bench, I("storage-component"), G(AnyDiamond), I(ItemID.Topaz, 3));
            Add("crafting-access", 1, bench, I("storage-component"), G(AnyDiamond), I(ItemID.Sapphire, 3));
            Add("remote-access", 1, bench, I("storage-component"), G(AnyDiamond, 3), I(ItemID.Ruby, 3));

            // Upgrades
            Add("upgrade-demonite", 1, anvil, I(ItemID.DemoniteBar, 10), I(ItemID.Amethyst));
            Add("upgrade-crimtane", 1, anvil, I(ItemID.CrimtaneBar, 10), I(ItemID.Amethyst));
            Add("upgrade-hellstone", 1, anvil, I(ItemID.HellstoneBar, 10), I(ItemID.Topaz));
            Add("upgrade-hallowed", 1, hmAnvil, I(ItemID.HallowedBar, 10), I(ItemID.SoulofFright), I(ItemID.SoulofMight), I(ItemID.SoulofSight), I(ItemID.Sapphire));
            Add("upgrade-bluechlorophyte", 1, hmAnvil, I(ItemID.ShroomiteBar, 5), I(ItemID.SpectreBar, 5), I(ItemID.BeetleHusk, 2), I(ItemID.Emerald));
            Add("upgrade-luminite", 1, lunar, I(ItemID.LunarBar, 10), I(ItemID.FragmentSolar, 5), I(ItemID.FragmentVortex, 5), I(ItemID.FragmentNebula, 5), I(ItemID.FragmentStardust, 5), I(ItemID.Ruby));
            Add("upgrade-terra", 1, lunar, I("radiant-jewel"), G(AnyDiamond));

            // Unit + upgrade → next tier unit
            foreach (UnitTier tier in UnitTier.All)
                foreach (string from in tier.UpgradesFrom)
                {
                    UnitTier prev = UnitTier.ByKey(from);
                    if (prev != null && tier.UpgradeItem != null)
                        Add(tier.UnitItem, 1, -1, I(prev.UnitItem), I(tier.UpgradeItem));
                }

            // Tools
            Add("locator", 1, anvil, I(ItemID.MeteoriteBar, 10), I(ItemID.Amber, 2));
            Add("locator-disk", 1, lunar, I(ItemID.MartianConduitPlating, 25), I(ItemID.LunarBar, 2));
            Add("portable-access-prehm", 1, anvil, I("locator"), G(wood, 20), G(iron, 15), G(AnyDemoniteBar, 10), G(AnyDiamond, 3), I(ItemID.Ruby, 3));
            Add("portable-access-hm", 1, hmAnvil, I("portable-access-prehm"), I(ItemID.Pearlwood, 20), G(AnyMythrilBar, 15), I(ItemID.ChlorophyteBar, 10));
            Add("portable-access", 1, lunar, I("portable-access-hm"), I(ItemID.MartianConduitPlating, 15), I(ItemID.LunarBar, 8), I("radiant-jewel"));
            Add("portable-crafting-access-prehm", 1, anvil, I("locator"), G(wood, 20), G(iron, 15), G(AnyDemoniteBar, 10), G(AnyDiamond, 3), I(ItemID.Sapphire, 3));
            Add("portable-crafting-access-hm", 1, hmAnvil, I("portable-crafting-access-prehm"), I(ItemID.Pearlwood, 20), G(AnyMythrilBar, 15), I(ItemID.ChlorophyteBar, 10));
            Add("portable-crafting-access", 1, lunar, I("portable-crafting-access-hm"), I(ItemID.MartianConduitPlating, 20), I(ItemID.LunarBar, 15));

            // Combined stations
            Add("combined-stations-1", 1, -1, G(AnyWorkBench), I(ItemID.Furnace), G(AnyPreHmAnvil), G(AnyBottle), G(AnySink), I(ItemID.Sawmill), I(ItemID.Loom), G(AnyTable));
            Add("combined-stations-2", 1, -1, I("combined-stations-1"), I(ItemID.Hellforge), I(ItemID.AlchemyTable), G(AnyCookingPot), I(ItemID.TinkerersWorkshop), I(ItemID.DyeVat), I(ItemID.HeavyWorkBench), I(ItemID.Keg), I(ItemID.TeaKettle));
            Add("combined-stations-3", 1, -1, I("combined-stations-2"), I(ItemID.ImbuingStation), G(AnyHmAnvil), G(AnyHmFurnace), G(AnyBookcase), I(ItemID.CrystalBall), I(ItemID.BlendOMatic));
            Add("combined-furniture-stations-1", 1, -1, I(ItemID.BoneWelder), I(ItemID.GlassKiln), I(ItemID.HoneyDispenser), I(ItemID.IceMachine), I(ItemID.LivingLoom), I(ItemID.SkyMill), I(ItemID.Solidifier));
            Add("combined-furniture-stations-2", 1, -1, I("combined-furniture-stations-1"), I(ItemID.MeatGrinder), I(ItemID.LesionStation), I(ItemID.FleshCloningVaat), I(ItemID.SteampunkBoiler), I(ItemID.LihzahrdFurnace));
            Add("combined-stations-4", 1, -1, I("combined-stations-3"), I("combined-furniture-stations-2"), I(ItemID.Autohammer), I(ItemID.LunarCraftingStation), I(ItemID.LavaBucket, 10), I(ItemID.HoneyBucket, 10));
        }

        private static void Add(string result, int stack, int station, params Ing[] ingredients)
        {
            int resultType = MSItems.TypeOf(result);
            if (resultType <= 0) { Log.Warn($"[Recipes] Unknown result {result}"); return; }
            if (ingredients.Any(i => i.Group == null && i.Type <= 0)) { Log.Warn($"[Recipes] Unresolved ingredient for {result}"); return; }
            if (Recipe.numRecipes >= Main.recipe.Length)
            {
                int newSize = Main.recipe.Length + 256;
                Array.Resize(ref Main.recipe, newSize);
                for (int k = Recipe.numRecipes; k < newSize; k++) Main.recipe[k] = new Recipe();
                Recipe.maxRecipes = newSize;
            }

            var r = new Recipe();
            r.createItem.SetDefaults(resultType);
            r.createItem.stack = stack;
            for (int k = 0; k < ingredients.Length && k < Recipe.maxRequirements; k++)
            {
                Ing ing = ingredients[k];
                if (ing.Group != null)
                {
                    r.requiredItem[k].SetDefaults(ing.Group.GetPlaceholderItemType());
                    r.requiredItem[k].stack = ing.Stack;
                    r.RequireGroup(ing.Group);
                    r.requiredItemQuickLookup[k] = new Recipe.RequiredItemEntry(ing.Group, ing.Stack);
                }
                else
                {
                    r.requiredItem[k].SetDefaults(ing.Type);
                    r.requiredItem[k].stack = ing.Stack;
                    r.requiredItemQuickLookup[k] = new Recipe.RequiredItemEntry(ing.Type, ing.Stack);
                }
            }
            r.requiredTile = station;
            if (station == TileID.Bottles) r.alchemy = true;
            Main.recipe[Recipe.numRecipes] = r;
            if (station >= 0 && station < Recipe.TileUsedInRecipes.Length) Recipe.TileUsedInRecipes[station] = true;
            Recipe.numRecipes++;
        }
    }
}
