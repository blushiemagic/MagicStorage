using MagicStorage.Items;
using MagicStorage.Stations;
using SerousCommonLib.API.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems {
	internal class MagicRecipes : ModSystem {
		public override void AddRecipes()
		{
			Recipe.Create(ItemID.CookedMarshmallow)
				.AddIngredient(ItemID.Marshmallow)
				.AddCondition(MagicStorageMod.HasCampfire)
				.Register();
		}

		//Regexes for filtering vanilla item types
		public static readonly Regex chestItemRegex = new(@"\b(?!Fake_)(.*Chest)\b", RegexOptions.Compiled);
		public static readonly Regex workBenchItemRegex = new(@"\b(.*WorkBench)\b", RegexOptions.Compiled);
		public static readonly Regex sinkItemRegex = new(@"\b(.*Sink)(?:Does)?\b", RegexOptions.Compiled);
		public static readonly Regex tableItemRegex = new(@"\b(.*Table)(?:WithCloth)?\b", RegexOptions.Compiled);
		public static readonly Regex bookcaseItemRegex = new(@"\b(.*Bookcase)\b", RegexOptions.Compiled);
		public static readonly Regex campfireItemRegex = new(@"\b(.*Campfire)\b", RegexOptions.Compiled);

		public override void AddRecipeGroups()
		{
			ModLoadingProgressHelper.SetLoadingSubProgressText("MagicStorage.MagicRecipes::AddRecipeGroups");

			IEnumerable<int> vanillaItems = Enumerable.Range(0, ItemID.Count);

			int[] GetItems(int iconicItem, Regex regex, params int[] ignore) {
				List<int> ids = vanillaItems.Where(id => regex.IsMatch(ItemID.Search.GetName(id))).ToList();

				ids.Remove(iconicItem);
				ids.Insert(0, iconicItem);

				foreach (int id in ignore)
					ids.Remove(id);

				return ids.ToArray();
			}

			string any = Language.GetTextValue("LegacyMisc.37");

			int[] items = GetItems(ItemID.Chest, chestItemRegex);
			RecipeGroup group = new(() => $"{any} Chest", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyChest", group);
			RegisterGroupClone(group, nameof(ItemID.Chest));

			items = new int[] { ItemID.SnowBlock, ItemID.IceBlock, ItemID.PurpleIceBlock, ItemID.PinkIceBlock, ItemID.RedIceBlock };
			group = new RecipeGroup(() => $"{any} {Language.GetTextValue("Mods.MagicStorage.SnowBiomeBlock")}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnySnowBiomeBlock", group);
			RegisterGroupClone(group, nameof(ItemID.SnowBlock));

			items = new[] { ItemID.Diamond, ModContent.ItemType<ShadowDiamond>() };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Diamond)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyDiamond", group);
			RegisterGroupClone(group, nameof(ItemID.Diamond));

			items = GetItems(ItemID.WorkBench, workBenchItemRegex,
				ItemID.HeavyWorkBench);
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.WorkBench)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyWorkBench", group);
			RegisterGroupClone(group, nameof(ItemID.WorkBench));

			items = new int[] { ItemID.IronAnvil, ItemID.LeadAnvil };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.IronAnvil)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyPreHmAnvil", group);
			RegisterGroupClone(group, nameof(ItemID.IronAnvil));

			items = new int[] { ItemID.Bottle, ItemID.PinkVase, ItemID.Mug, ItemID.DynastyCup, ItemID.WineGlass, ItemID.HoneyCup, ItemID.SteampunkCup };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Bottle)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyBottle", group);
			RegisterGroupClone(group, nameof(ItemID.Bottle));

			items = GetItems(ItemID.MetalSink, sinkItemRegex);
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.MetalSink)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnySink", group);
			RegisterGroupClone(group, nameof(ItemID.MetalSink));

			items = GetItems(ItemID.WoodenTable, tableItemRegex,
				ItemID.BewitchingTable, ItemID.AlchemyTable);
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.WoodenTable)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyTable", group);
			RegisterGroupClone(group, nameof(ItemID.WoodenTable));

			items = new int[] { ItemID.CookingPot, ItemID.Cauldron };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.CookingPot)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyCookingPot", group);
			RegisterGroupClone(group, nameof(ItemID.CookingPot));

			items = new int[] { ItemID.MythrilAnvil, ItemID.OrichalcumAnvil };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.MythrilAnvil)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyHmAnvil", group);
			RegisterGroupClone(group, nameof(ItemID.MythrilAnvil));

			items = new int[] { ItemID.AdamantiteForge, ItemID.TitaniumForge };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.AdamantiteForge)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyHmFurnace", group);
			RegisterGroupClone(group, nameof(ItemID.AdamantiteForge));

			items = GetItems(ItemID.Bookcase, bookcaseItemRegex);
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Bookcase)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyBookcase", group);
			RegisterGroupClone(group, nameof(ItemID.Bookcase));

			items = new int[]
			{
				ItemID.Tombstone,
				ItemID.GraveMarker,
				ItemID.CrossGraveMarker,
				ItemID.Headstone,
				ItemID.Gravestone,
				ItemID.Obelisk,
				ItemID.RichGravestone1,
				ItemID.RichGravestone2,
				ItemID.RichGravestone3,
				ItemID.RichGravestone4,
				ItemID.RichGravestone5
			};
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Tombstone)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyTombstone", group);
			RegisterGroupClone(group, nameof(ItemID.Tombstone));

			items = GetItems(ItemID.Campfire, campfireItemRegex);
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Campfire)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyCampfire", group);
			RegisterGroupClone(group, nameof(ItemID.Campfire));

			items = new[] { ModContent.ItemType<DemonAltar>(), ModContent.ItemType<CrimsonAltar>() };

			//Support the Demon/Crimson Altar items from Fargo's Mutants Mod
			if (ModLoader.TryGetMod("Fargowiltas", out Mod Fargowiltas)) {
				Array.Resize(ref items, items.Length + 2);
				items[^2] = Fargowiltas.Find<ModItem>("DemonAltar").Type;
				items[^1] = Fargowiltas.Find<ModItem>("CrimsonAltar").Type;
			}

			//Support the Corrupt/Crimson Altar items from LuiAFK Reborn
			if (ModLoader.TryGetMod("miningcracks_take_on_luiafk", out Mod LuiAFK)) {
				Array.Resize(ref items, items.Length + 2);
				items[^2] = LuiAFK.Find<ModItem>("CorruptionAltar").Type;
				items[^1] = LuiAFK.Find<ModItem>("CrimsonAltar").Type;
			}

			group = new RecipeGroup(() => $"{any} {Language.GetTextValue("MapObject.DemonAltar")}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyDemonAltar", group);

			items = new int[] { ItemID.SilverBar, ItemID.TungstenBar };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.SilverBar)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnySilverBar", group);
			RegisterGroupClone(group, nameof(ItemID.SilverBar));

			items = new int[] { ItemID.MythrilBar, ItemID.OrichalcumBar };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.MythrilBar)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyMythrilBar", group);
			RegisterGroupClone(group, nameof(ItemID.MythrilBar));

			items = new int[] { ItemID.DemoniteBar, ItemID.CrimtaneBar };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.DemoniteBar)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyDemoniteBar", group);
			RegisterGroupClone(group, nameof(ItemID.DemoniteBar));

			ModLoadingProgressHelper.SetLoadingSubProgressText("");
		}

		private static void RegisterGroupClone(RecipeGroup original, string groupName) {
			// If the group already exists, union the sets and overwrite the reference
			// Otherwise, make a new group that's a copy of the original group
			if (RecipeGroup.recipeGroupIDs.TryGetValue(groupName, out int groupID)) {
				RecipeGroup group = RecipeGroup.recipeGroups[groupID];
				original.ValidItems.UnionWith(group.ValidItems);
				group.ValidItems = original.ValidItems;
			} else {
				RecipeGroup group = new RecipeGroup(original.GetText, new int[1]);
				group.ValidItems = original.ValidItems;
				RecipeGroup.RegisterGroup(groupName, group);
			}
		}
	}
}
