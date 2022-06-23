using MagicStorage.Edits;
using MagicStorage.Items;
using MagicStorage.Stations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage {
	public class MagicStorage : Mod {
		public static MagicStorage Instance => ModContent.GetInstance<MagicStorage>();

		// Integration with ModHelpers
		public static string GithubUserName => "blushiemagic";
		public static string GithubProjectName => "MagicStorage";

		// TODO: text prompt to input exact amount of items wanted (hint: make prompt update to max possible, should a user input more, and to 0 should a user input a negative number/invalid string)

		public override void Load()
		{
			InterfaceHelper.Initialize();
      
			// AddTranslations() removed, now use hjsons in Localization/
			// AddTranslations();
		}

		public override void Unload()
		{
			StorageGUI.Unload();
			CraftingGUI.Unload();
		}

		// AddTranslations() removed, now use hjsons in Localization/
		// private void AddTranslations() {
		// }

		public override void AddRecipes()
		{
#if TML_2022_05
			CreateRecipe(ItemID.CookedMarshmallow)
#else
			Recipe.Create(ItemID.CookedMarshmallow)
#endif
				.AddIngredient(ItemID.Marshmallow)
				.AddCondition(new Recipe.Condition(NetworkText.FromKey("Mods.MagicStorage.CookedMarshmallowCondition"), recipe => CraftingGUI.Campfire))
				.Register();
		}

		public override void PostAddRecipes()
		{
			//Make a copy of every recipe that requires Ecto Mist, but let it be crafted at the appropriate combined station(s) as well
			for (int i = 0; i < Recipe.numRecipes; i++)
			{
				Recipe recipe = Main.recipe[i];

				if (recipe.Disabled)
					continue;

				if (recipe.HasCondition(Recipe.Condition.InGraveyardBiome))
				{
#if TML_2022_05
					Recipe copy = CloneRecipe(recipe);
#else
					Recipe copy = recipe.Clone();
#endif

					copy.requiredTile.Clear(); // Should this be cleared?
					copy.AddTile<CombinedStations4Tile>();

					copy.RemoveCondition(Recipe.Condition.InGraveyardBiome);

					copy.Register();
				}
			}
		}

		public override void AddRecipeGroups()
		{
			string any = Language.GetTextValue("LegacyMisc.37");

			int[] items =
			{
				ItemID.Chest,
				ItemID.GoldChest,
				ItemID.ShadowChest,
				ItemID.EbonwoodChest,
				ItemID.RichMahoganyChest,
				ItemID.PearlwoodChest,
				ItemID.IvyChest,
				ItemID.IceChest,
				ItemID.LivingWoodChest,
				ItemID.SkywareChest,
				ItemID.ShadewoodChest,
				ItemID.WebCoveredChest,
				ItemID.LihzahrdChest,
				ItemID.WaterChest,
				ItemID.JungleChest,
				ItemID.CorruptionChest,
				ItemID.CrimsonChest,
				ItemID.HallowedChest,
				ItemID.FrozenChest,
				ItemID.DynastyChest,
				ItemID.HoneyChest,
				ItemID.SteampunkChest,
				ItemID.PalmWoodChest,
				ItemID.MushroomChest,
				ItemID.BorealWoodChest,
				ItemID.SlimeChest,
				ItemID.GreenDungeonChest,
				ItemID.PinkDungeonChest,
				ItemID.BlueDungeonChest,
				ItemID.BoneChest,
				ItemID.CactusChest,
				ItemID.FleshChest,
				ItemID.ObsidianChest,
				ItemID.PumpkinChest,
				ItemID.SpookyChest,
				ItemID.GlassChest,
				ItemID.MartianChest,
				ItemID.GraniteChest,
				ItemID.MeteoriteChest,
				ItemID.MarbleChest
			};
			RecipeGroup group = new(() => $"{any} Chest", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyChest", group);

			items = new int[] { ItemID.SnowBlock, ItemID.IceBlock, ItemID.PurpleIceBlock, ItemID.PinkIceBlock, ItemID.RedIceBlock };
			group = new RecipeGroup(() => $"{any} {Language.GetTextValue("Mods.MagicStorage.SnowBiomeBlock")}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnySnowBiomeBlock", group);

			items = new[] { ItemID.Diamond, ModContent.ItemType<ShadowDiamond>() };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Diamond)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyDiamond", group);

			items = new int[]
			{
				ItemID.WorkBench,
				ItemID.BambooWorkbench,
				ItemID.BlueDungeonWorkBench,
				ItemID.BoneWorkBench,
				ItemID.BorealWoodWorkBench,
				ItemID.CactusWorkBench,
				ItemID.CrystalWorkbench,
				ItemID.DynastyWorkBench,
				ItemID.EbonwoodWorkBench,
				ItemID.FleshWorkBench,
				ItemID.FrozenWorkBench,
				ItemID.GlassWorkBench,
				ItemID.GoldenWorkbench,
				ItemID.GothicWorkBench,
				ItemID.GraniteWorkBench,
				ItemID.GreenDungeonWorkBench,
				ItemID.HoneyWorkBench,
				ItemID.LesionWorkbench,
				ItemID.LihzahrdWorkBench,
				ItemID.LivingWoodWorkBench,
				ItemID.MarbleWorkBench,
				ItemID.MartianWorkBench,
				ItemID.MeteoriteWorkBench,
				ItemID.NebulaWorkbench,
				ItemID.ObsidianWorkBench,
				ItemID.PalmWoodWorkBench,
				ItemID.PearlwoodWorkBench,
				ItemID.PinkDungeonWorkBench,
				ItemID.PumpkinWorkBench,
				ItemID.RichMahoganyWorkBench,
				ItemID.SandstoneWorkbench,
				ItemID.ShadewoodWorkBench,
				ItemID.SkywareWorkbench,
				ItemID.SlimeWorkBench,
				ItemID.SolarWorkbench,
				ItemID.SpiderWorkbench,
				ItemID.SpookyWorkBench,
				ItemID.StardustWorkbench,
				ItemID.SteampunkWorkBench,
				ItemID.VortexWorkbench
			};
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.WorkBench)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyWorkBench", group);

			items = new int[] { ItemID.IronAnvil, ItemID.LeadAnvil };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.IronAnvil)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyPreHmAnvil", group);

			items = new int[] { ItemID.Bottle, ItemID.PinkVase, ItemID.Mug, ItemID.DynastyCup, ItemID.WineGlass, ItemID.HoneyCup, ItemID.SteampunkCup };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Bottle)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyBottle", group);

			items = new int[]
			{
				ItemID.BambooSink,
				ItemID.BlueDungeonSink,
				ItemID.BoneSink,
				ItemID.BorealWoodSink,
				ItemID.CactusSink,
				ItemID.CrystalSink,
				ItemID.DynastySink,
				ItemID.EbonwoodSink,
				ItemID.FleshSink,
				ItemID.FrozenSink,
				ItemID.GlassSink,
				ItemID.GoldenSink,
				ItemID.GraniteSink,
				ItemID.GreenDungeonSink,
				ItemID.HoneySink,
				ItemID.LesionSink,
				ItemID.LihzahrdSink,
				ItemID.LivingWoodSink,
				ItemID.MarbleSink,
				ItemID.MartianSink,
				ItemID.MetalSink,
				ItemID.MeteoriteSink,
				ItemID.MushroomSink,
				ItemID.NebulaSink,
				ItemID.ObsidianSink,
				ItemID.PalmWoodSink,
				ItemID.PearlwoodSink,
				ItemID.PinkDungeonSink,
				ItemID.PumpkinSink,
				ItemID.RichMahoganySink,
				ItemID.SandstoneSink,
				ItemID.ShadewoodSink,
				ItemID.SkywareSink,
				ItemID.SlimeSink,
				ItemID.SolarSink,
				ItemID.SpiderSinkSpiderSinkDoesWhateverASpiderSinkDoes,
				ItemID.SpookySink,
				ItemID.StardustSink,
				ItemID.SteampunkSink,
				ItemID.VortexSink,
				ItemID.WoodenSink
			};
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.MetalSink)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnySink", group);

			items = new int[]
			{
				ItemID.BambooTable,
				ItemID.BanquetTable,
				ItemID.BlueDungeonTable,
				ItemID.BoneTable,
				ItemID.BorealWoodTable,
				ItemID.CactusTable,
				ItemID.CrystalTable,
				ItemID.DynastyTable,
				ItemID.EbonwoodTable,
				ItemID.FleshTable,
				ItemID.FrozenTable,
				ItemID.GlassTable,
				ItemID.GoldenTable,
				ItemID.GothicTable,
				ItemID.GraniteTable,
				ItemID.GreenDungeonTable,
				ItemID.HoneyTable,
				ItemID.LesionTable,
				ItemID.LihzahrdTable,
				ItemID.LivingWoodTable,
				ItemID.MarbleTable,
				ItemID.MartianTable,
				ItemID.MeteoriteTable,
				ItemID.MushroomTable,
				ItemID.NebulaTable,
				ItemID.ObsidianTable,
				ItemID.PalmWoodTable,
				ItemID.PearlwoodTable,
				ItemID.PicnicTable,
				ItemID.PicnicTableWithCloth,
				ItemID.PineTable,
				ItemID.PinkDungeonTable,
				ItemID.PumpkinTable,
				ItemID.RichMahoganyTable,
				ItemID.SandstoneTable,
				ItemID.ShadewoodTable,
				ItemID.SkywareTable,
				ItemID.SlimeTable,
				ItemID.SolarTable,
				ItemID.SpiderTable,
				ItemID.SpookyTable,
				ItemID.StardustTable,
				ItemID.SteampunkTable,
				ItemID.VortexTable,
				ItemID.WoodenTable
			};
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.WoodenTable)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyTable", group);

			items = new int[] { ItemID.CookingPot, ItemID.Cauldron };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.CookingPot)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyCookingPot", group);

			items = new int[] { ItemID.MythrilAnvil, ItemID.OrichalcumAnvil };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.MythrilAnvil)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyHmAnvil", group);

			items = new int[] { ItemID.AdamantiteForge, ItemID.TitaniumForge };
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.AdamantiteForge)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyHmFurnace", group);

			items = new int[]
			{
				ItemID.Bookcase,
				ItemID.BambooBookcase,
				ItemID.BlueDungeonBookcase,
				ItemID.BoneBookcase,
				ItemID.BorealWoodBookcase,
				ItemID.CactusBookcase,
				ItemID.CrystalBookCase,
				ItemID.DynastyBookcase,
				ItemID.EbonwoodBookcase,
				ItemID.FleshBookcase,
				ItemID.FrozenBookcase,
				ItemID.GlassBookcase,
				ItemID.GoldenBookcase,
				ItemID.GothicBookcase,
				ItemID.GraniteBookcase,
				ItemID.GreenDungeonBookcase,
				ItemID.HoneyBookcase,
				ItemID.LesionBookcase,
				ItemID.LihzahrdBookcase,
				ItemID.MarbleBookcase,
				ItemID.MeteoriteBookcase,
				ItemID.MushroomBookcase,
				ItemID.NebulaBookcase,
				ItemID.ObsidianBookcase,
				ItemID.PalmWoodBookcase,
				ItemID.PearlwoodBookcase,
				ItemID.PinkDungeonBookcase,
				ItemID.PumpkinBookcase,
				ItemID.RichMahoganyBookcase,
				ItemID.SandstoneBookcase,
				ItemID.ShadewoodBookcase,
				ItemID.SkywareBookcase,
				ItemID.SlimeBookcase,
				ItemID.SolarBookcase,
				ItemID.SpiderBookcase,
				ItemID.SpookyBookcase,
				ItemID.StardustBookcase,
				ItemID.SteampunkBookcase,
				ItemID.VortexBookcase
			};
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Bookcase)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyBookcase", group);

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

			items = new int[]
			{
				ItemID.Campfire,
				ItemID.BoneCampfire,
				ItemID.CoralCampfire,
				ItemID.CorruptCampfire,
				ItemID.CrimsonCampfire,
				ItemID.CursedCampfire,
				ItemID.DemonCampfire,
				ItemID.DesertCampfire,
				ItemID.FrozenCampfire,
				ItemID.HallowedCampfire,
				ItemID.IchorCampfire,
				ItemID.JungleCampfire,
				ItemID.RainbowCampfire,
				ItemID.UltraBrightCampfire
			};
			group = new RecipeGroup(() => $"{any} {Lang.GetItemNameValue(ItemID.Campfire)}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyCampfire", group);

			items = new[] { ModContent.ItemType<DemonAltar>(), ModContent.ItemType<CrimsonAltar>() };
			group = new RecipeGroup(() => $"{any} {Language.GetTextValue("MapObject.DemonAltar")}", items);
			RecipeGroup.RegisterGroup("MagicStorage:AnyDemonAltar", group);
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			NetHelper.HandlePacket(reader, whoAmI);
		}

		public override object Call(params object[] args) {
			if (args.Length < 1)
				throw new ArgumentException("Call requires at least one argument");

			string function = "";

			void TryParseAs<T>(int arg, out T value) {
				if (args.Length < arg)
					throw new ArgumentException($"Call \"{function}\" requires at least {arg} arguments");

				if (args[arg] is T v)
					value = v;
				else
					throw new ArgumentException($"Call requires argument #{arg + 1} to be of type {typeof(T).GetSimplifiedGenericTypeName()}");
			}

			TryParseAs(0, out function);

			switch (function) {
				case "Register Sorting":
					TryParseAs(1, out int itemType);
					TryParseAs(2, out Func<Item, Item, bool> canCombine);

					MagicCache.canCombineByType[itemType] = canCombine;
					break;
				default:
					throw new ArgumentException("Call does not support the function \"" + function + "\"");
			}

			return null;
		}
	}
}
