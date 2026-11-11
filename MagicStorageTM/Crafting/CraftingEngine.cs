using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorageTM.Content;
using MagicStorageTM.Storage;
using MagicStorageTM.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Achievements;
using Terraria.ID;

namespace MagicStorageTM.Crafting
{
    /// <summary>Crafting stations and liquids/biomes provided by a Crafting Interface (MagicStorage's CraftingInformation).</summary>
    public sealed class CraftEnvironment
    {
        public bool[] AdjTiles;
        public bool Water, Lava, Honey, Snow, Graveyard, Alchemy;
    }

    /// <summary>Recipe availability and execution against a storage network (replaces CraftingGUI's engine).</summary>
    public static class CraftingEngine
    {
        public static CraftEnvironment BuildEnvironment(Point16 craftingAccess, Player player)
        {
            int size = Math.Max(player.adjTile.Length, Math.Max(CustomTiles.ExtendedCount, TileID.Count));
            var env = new CraftEnvironment { AdjTiles = new bool[size] };
            if (StorageWorld.CraftingAccesses.TryGetValue(craftingAccess, out var data))
                foreach (Item station in data.Stations) AddStation(env, station);
            if (MSTiles.CraftingAccess != null && MSTiles.CraftingAccess.Type < size)
                env.AdjTiles[MSTiles.CraftingAccess.Type] = true;
            // MagicStorage crafts "at" the access; biome requirements still follow the player.
            env.Snow |= player.ZoneSnow;
            env.Graveyard |= player.ZoneGraveyard;
            return env;
        }

        /// <summary>Same rules as MagicStorage's Utility.AddCraftingZones.</summary>
        public static void AddStation(CraftEnvironment env, Item item)
        {
            if (item == null || item.IsAir) return;
            int tile = item.createTile;
            if (tile >= 0 && tile < env.AdjTiles.Length)
            {
                SetAdj(env, tile);
                switch (tile)
                {
                    case TileID.GlassKiln:
                    case TileID.Hellforge:
                        SetAdj(env, TileID.Furnaces); break;
                    case TileID.AdamantiteForge:
                        SetAdj(env, TileID.Furnaces); SetAdj(env, TileID.Hellforge); break;
                    case TileID.MythrilAnvil:
                        SetAdj(env, TileID.Anvils); break;
                    case TileID.BewitchingTable:
                    case TileID.Tables2:
                        SetAdj(env, TileID.Tables); SetAdj(env, TileID.Chairs); break;
                    case TileID.AlchemyTable:
                        SetAdj(env, TileID.Bottles); SetAdj(env, TileID.Tables); env.Alchemy = true; break;
                    case TileID.Tombstones:
                        env.Graveyard = true; break;
                    case TileID.WorkBenches:
                    case TileID.Tables:
                        SetAdj(env, TileID.Chairs); break;
                }
                if (TileID.Sets.CountsAsWaterForCrafting[tile]) env.Water = true;
                if (tile == TileID.Sinks) env.Water = true;
                CustomTileDef def = CustomTiles.Get(tile);
                if (def != null)
                {
                    env.Water |= def.CountsAsWater;
                    env.Lava |= def.CountsAsLava;
                    env.Honey |= def.CountsAsHoney;
                }
            }
            switch (item.type)
            {
                case ItemID.WaterBucket:
                case ItemID.BottomlessBucket:
                    env.Water = true; break;
                case ItemID.LavaBucket:
                case ItemID.BottomlessLavaBucket:
                    env.Lava = true; break;
                case ItemID.HoneyBucket:
                case ItemID.BottomlessHoneyBucket:
                    env.Honey = true; break;
            }
            if (env.Graveyard) SetAdj(env, TileID.Tombstones);
        }

        private static void SetAdj(CraftEnvironment env, int tile, int depth = 0)
        {
            if (tile < 0 || tile >= env.AdjTiles.Length || depth > 8) return;
            env.AdjTiles[tile] = true;
            if (tile == TileID.AlchemyTable) env.Alchemy = true;
            if (tile < Recipe.TileCountsAs.Length && Recipe.TileCountsAs[tile] is List<int> list)
                foreach (int t in list) SetAdj(env, t, depth + 1);
        }

        /// <summary>Is this item accepted in a Crafting Interface station slot?</summary>
        public static bool IsValidStation(Item item)
        {
            if (item == null || item.IsAir) return false;
            if (item.createTile >= 0 && item.createTile < Recipe.TileUsedInRecipes.Length && Recipe.TileUsedInRecipes[item.createTile]) return true;
            if (item.createTile >= 0 && (CustomTiles.Get(item.createTile)?.CountsAsTiles?.Length ?? 0) > 0) return true;
            if (item.createTile == TileID.Tombstones || item.createTile == TileID.Sinks) return true;
            switch (item.type)
            {
                case ItemID.WaterBucket:
                case ItemID.BottomlessBucket:
                case ItemID.LavaBucket:
                case ItemID.BottomlessLavaBucket:
                case ItemID.HoneyBucket:
                case ItemID.BottomlessHoneyBucket:
                    return true;
            }
            return false;
        }

        /// <summary>Run <paramref name="action"/> with the player's crafting flags replaced by the environment's.</summary>
        public static T WithEnvironment<T>(Player player, CraftEnvironment env, Func<T> action)
        {
            bool[] oldAdj = player.adjTile;
            bool w = player.adjWaterSource, l = player.adjLava, h = player.adjHoney, a = player.alchemyTable;
            bool snow = player.ZoneSnow, grave = player.ZoneGraveyard;
            try
            {
                player.adjTile = env.AdjTiles;
                player.adjWaterSource = env.Water;
                player.adjLava = env.Lava;
                player.adjHoney = env.Honey;
                player.alchemyTable = env.Alchemy;
                player.ZoneSnow = env.Snow;
                player.ZoneGraveyard = env.Graveyard;
                return action();
            }
            finally
            {
                player.adjTile = oldAdj;
                player.adjWaterSource = w;
                player.adjLava = l;
                player.adjHoney = h;
                player.alchemyTable = a;
                player.ZoneSnow = snow;
                player.ZoneGraveyard = grave;
            }
        }

        public static bool MeetsEnvironment(Recipe recipe, CraftEnvironment env, Player player, List<string> missing = null)
        {
            if (recipe.crimson && !WorldGen.crimson) { missing?.Add("Crimson world"); return false; }
            if (recipe.corruption && WorldGen.crimson) { missing?.Add("Corruption world"); return false; }
            return WithEnvironment(player, env, () => recipe.PlayerMeetsEnvironmentConditions(player, missing));
        }

        public static IEnumerable<Recipe.RequiredItemEntry> Ingredients(Recipe recipe)
        {
            for (int i = 0; i < Recipe.maxRequirements; i++)
            {
                var entry = recipe.requiredItemQuickLookup[i];
                if (entry.itemIdOrRecipeGroup == 0) yield break;
                yield return entry;
            }
        }

        public static long CountFor(Recipe.RequiredItemEntry entry, Dictionary<int, long> counts)
        {
            if (!entry.IsRecipeGroup)
                return counts.TryGetValue(entry.itemIdOrRecipeGroup, out long c) ? c : 0;
            long total = 0;
            foreach (int t in entry.RecipeGroup.ValidItems)
                if (counts.TryGetValue(t, out long c)) total += c;
            return total;
        }

        /// <summary>How many times the recipe could be crafted with the counted items (ignores stations).</summary>
        public static long MaxCrafts(Recipe recipe, Dictionary<int, long> counts)
        {
            long max = long.MaxValue;
            bool any = false;
            foreach (var entry in Ingredients(recipe))
            {
                any = true;
                if (entry.stack <= 0) continue;
                max = Math.Min(max, CountFor(entry, counts) / entry.stack);
                if (max == 0) return 0;
            }
            return any ? max : 0;
        }

        /// <summary>Items usable for crafting: everything in storage, plus optionally the player's inventory.</summary>
        public static Dictionary<int, long> AvailableCounts(Point16 heart, Player player, bool includeInventory)
        {
            var counts = StorageOps.CountByType(heart);
            if (includeInventory)
            {
                for (int i = 0; i < 58; i++)
                {
                    Item it = player.inventory[i];
                    if (it == null || it.IsAir) continue;
                    counts.TryGetValue(it.type, out long c);
                    counts[it.type] = c + it.stack;
                }
            }
            return counts;
        }

        /// <summary>
        /// Craft up to <paramref name="times"/> times. Ingredients come from storage first, then (if allowed)
        /// the player's non-favorited inventory. Results go to the mouse when possible, otherwise the inventory.
        /// Returns the number of successful crafts.
        /// </summary>
        public static int Craft(Recipe recipe, int times, Point16 heart, CraftEnvironment env, Player player, bool includeInventory)
        {
            int crafted = 0;
            var list = new List<Recipe.RequiredItemEntry>();
            for (int n = 0; n < times; n++)
            {
                if (!MeetsEnvironment(recipe, env, player)) break;
                var counts = AvailableCounts(heart, player, includeInventory);
                if (MaxCrafts(recipe, counts) < 1) break;

                list.Clear();
                WithEnvironment(player, env, () => { recipe.GetIngredientsForOneCraft(player, list); return 0; });

                // Verify again with discounts applied, then consume.
                foreach (var req in list)
                    if (CountFor(req, counts) < req.stack) return crafted;
                foreach (var req in list) ConsumeIngredient(req, heart, player, includeInventory);

                Item result = CreateResult(recipe);
                Grant(recipe, result, player);
                crafted++;
            }
            if (crafted > 0)
            {
                try { CraftingEffects.OnCraft(recipe, false); } catch { }
                StorageWorld.MarkDirty();
            }
            return crafted;
        }

        private static void ConsumeIngredient(Recipe.RequiredItemEntry req, Point16 heart, Player player, bool includeInventory)
        {
            int remaining = req.stack;
            IEnumerable<int> types = req.IsRecipeGroup
                ? req.RecipeGroup.ValidItems.ToList()
                : new List<int> { req.itemIdOrRecipeGroup };
            foreach (int type in types)
            {
                if (remaining <= 0) break;
                remaining -= StorageOps.Consume(heart, type, remaining);
            }
            if (remaining > 0 && includeInventory)
            {
                foreach (int type in types)
                {
                    for (int i = 0; i < 58 && remaining > 0; i++)
                    {
                        Item it = player.inventory[i];
                        if (it == null || it.IsAir || it.type != type || it.favorited) continue;
                        int take = Math.Min(remaining, it.stack);
                        it.stack -= take;
                        remaining -= take;
                        if (it.stack <= 0) it.TurnToAir();
                    }
                }
            }
            if (remaining > 0) Log.Warn($"[Crafting] Ingredient shortfall of {remaining} for {req.itemIdOrRecipeGroup}");
        }

        private static Item CreateResult(Recipe recipe)
        {
            Item item = recipe.createItem.Clone();
            try { item.OnCreated(new RecipeItemCreationContext(recipe)); } catch { }
            if (item.stack <= 1) item.Prefix(-1);
            return item;
        }

        private static void Grant(Recipe recipe, Item result, Player player)
        {
            if (Main.mouseItem.IsAir || (Item.CanStack(Main.mouseItem, result) && Main.mouseItem.stack + result.stack <= Main.mouseItem.maxStack))
            {
                Main.CraftItem_GrantItem(recipe, result, false);
                return;
            }
            Item left = player.GetItem(result, GetItemSettings.ReturnItemShowAsNewNoCoinMerge);
            if (!left.IsAir)
                player.QuickSpawnItem(player.GetItemSource_InventoryOverflow(), left);
            try { AchievementsHelper.NotifyItemCraft(recipe); } catch { }
        }
    }
}
