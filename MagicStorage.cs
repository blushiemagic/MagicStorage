using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace MagicStorage
{
	public class MagicStorage : Mod
	{
		public static MagicStorage Instance;
		public static Mod bluemagicMod;
		public static Mod legendMod;

		public static readonly Version requiredVersion = new Version(0, 9, 2, 2);

		public override void Load()
		{
			if (ModLoader.version < requiredVersion)
			{
				throw new Exception("Magic storage requires a tModLoader version of at least " + requiredVersion);
			}
			Instance = this;
			InterfaceHelper.Initialize();
			legendMod = ModLoader.GetMod("LegendOfTerraria3");
			bluemagicMod = ModLoader.GetMod("Bluemagic");
			AddTranslations();
		}

		private void AddTranslations()
		{
			ModTranslation text = CreateTranslation("SetTo");
			text.SetDefault("Set to: X={0}, Y={1}");
			text.AddTranslation(GameCulture.Polish, "Ustawione na: X={0}, Y={1}");
			AddTranslation(text);

			text = CreateTranslation("SnowBiomeBlock");
			text.SetDefault("Snow Biome Block");
			AddTranslation(text);

			text = CreateTranslation("DepositAll");
			text.SetDefault("Deposit All");
			text.AddTranslation(GameCulture.Russian, "Переместить всё");
			AddTranslation(text);

			text = CreateTranslation("Search");
			text.SetDefault("Search");
			text.AddTranslation(GameCulture.Russian, "Поиск");
			AddTranslation(text);

			text = CreateTranslation("SearchName");
			text.SetDefault("Search Name");
			text.AddTranslation(GameCulture.Russian, "Поиск по имени");
			AddTranslation(text);

			text = CreateTranslation("SearchMod");
			text.SetDefault("Search Mod");
			text.AddTranslation(GameCulture.Russian, "Поиск по моду");
			AddTranslation(text);

			text = CreateTranslation("SortDefault");
			text.SetDefault("Default Sorting");
			text.AddTranslation(GameCulture.Russian, "Стандартная сортировка");
			AddTranslation(text);

			text = CreateTranslation("SortID");
			text.SetDefault("Sort by ID");
			text.AddTranslation(GameCulture.Russian, "Сортировка по ID");
			AddTranslation(text);

			text = CreateTranslation("SortName");
			text.SetDefault("Sort by Name");
			text.AddTranslation(GameCulture.Russian, "Сортировка по имени");
			AddTranslation(text);

			text = CreateTranslation("SortStack");
			text.SetDefault("Sort by Stacks");
			text.AddTranslation(GameCulture.Russian, "Сортировка по стакам");
			AddTranslation(text);

			text = CreateTranslation("FilterAll");
			text.SetDefault("Filter All");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Всё)");
			AddTranslation(text);

			text = CreateTranslation("FilterWeapons");
			text.SetDefault("Filter Weapons");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Оружия)");
			AddTranslation(text);

			text = CreateTranslation("FilterTools");
			text.SetDefault("Filter Tools");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Инструменты)");
			AddTranslation(text);

			text = CreateTranslation("FilterEquips");
			text.SetDefault("Filter Equipment");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Снаряжения)");
			AddTranslation(text);

			text = CreateTranslation("FilterPotions");
			text.SetDefault("Filter Potions");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Зелья)");
			AddTranslation(text);

			text = CreateTranslation("FilterTiles");
			text.SetDefault("Filter Placeables");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Размещаемое)");
			AddTranslation(text);

			text = CreateTranslation("FilterMisc");
			text.SetDefault("Filter Misc");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Разное)");
			AddTranslation(text);

			text = CreateTranslation("CraftingStations");
			text.SetDefault("Crafting Stations");
			text.AddTranslation(GameCulture.Russian, "Станции создания");
			AddTranslation(text);

			text = CreateTranslation("Recipes");
			text.SetDefault("Recipes");
			text.AddTranslation(GameCulture.Russian, "Рецепты");
			AddTranslation(text);

			text = CreateTranslation("SelectedRecipe");
			text.SetDefault("Selected Recipe");
			AddTranslation(text);

			text = CreateTranslation("Ingredients");
			text.SetDefault("Ingredients");
			AddTranslation(text);

			text = CreateTranslation("StoredItems");
			text.SetDefault("Stored Ingredients");
			AddTranslation(text);

			text = CreateTranslation("RecipeAvailable");
			text.SetDefault("Show available recipes");
			AddTranslation(text);

			text = CreateTranslation("RecipeAll");
			text.SetDefault("Show all recipes");
			AddTranslation(text);
		}

		public override void PostSetupContent()
		{
			
		}

		public override void AddRecipeGroups()
		{
			RecipeGroup group = new RecipeGroup(() => Lang.misc[37] + " Chest",
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
			ItemID.MarbleChest);
			RecipeGroup.RegisterGroup("MagicStorage:AnyChest", group);
			group = new RecipeGroup(() => Lang.misc[37].Value + " " + Language.GetTextValue("Mods.MagicStorage.SnowBiomeBlock"), ItemID.SnowBlock, ItemID.IceBlock, ItemID.PurpleIceBlock, ItemID.PinkIceBlock);
			if (bluemagicMod != null)
			{
				group.ValidItems.Add(bluemagicMod.ItemType("DarkBlueIce"));
			}
			RecipeGroup.RegisterGroup("MagicStorage:AnySnowBiomeBlock", group);
			group = new RecipeGroup(() => Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.Diamond), ItemID.Diamond, ItemType("ShadowDiamond"));
			if (legendMod != null)
			{
				group.ValidItems.Add(legendMod.ItemType("GemChrysoberyl"));
				group.ValidItems.Add(legendMod.ItemType("GemAlexandrite"));
			}
			RecipeGroup.RegisterGroup("MagicStorage:AnyDiamond", group);
			if (legendMod != null)
			{
				group = new RecipeGroup(() => Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.Amethyst), ItemID.Amethyst, legendMod.ItemType("GemOnyx"), legendMod.ItemType("GemSpinel"));
				RecipeGroup.RegisterGroup("MagicStorage:AnyAmethyst", group);
				group = new RecipeGroup(() => Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.Topaz), ItemID.Topaz, legendMod.ItemType("GemGarnet"));
				RecipeGroup.RegisterGroup("MagicStorage:AnyTopaz", group);
				group = new RecipeGroup(() => Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.Sapphire), ItemID.Sapphire, legendMod.ItemType("GemCharoite"));
				RecipeGroup.RegisterGroup("MagicStorage:AnySapphire", group);
				group = new RecipeGroup(() => Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.Emerald), legendMod.ItemType("GemPeridot"));
				RecipeGroup.RegisterGroup("MagicStorage:AnyEmerald", group);
				group = new RecipeGroup(() => Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.Ruby), ItemID.Ruby, legendMod.ItemType("GemOpal"));
				RecipeGroup.RegisterGroup("MagicStorage:AnyRuby", group);
			}
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			NetHelper.HandlePacket(reader, whoAmI);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			InterfaceHelper.ModifyInterfaceLayers(layers);
		}

		public override void PostUpdateInput()
		{
			StorageGUI.Update(null);
			CraftingGUI.Update(null);
		}
	}
}

