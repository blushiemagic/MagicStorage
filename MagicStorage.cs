﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

		public override void Unload()
		{
			Instance = null;
			bluemagicMod = null;
			legendMod = null;
		}

		private void AddTranslations()
		{
			ModTranslation text = CreateTranslation("SetTo");
			text.SetDefault("Set to: X={0}, Y={1}");
			text.AddTranslation(GameCulture.Polish, "Ustawione na: X={0}, Y={1}");
			text.AddTranslation(GameCulture.French, "Mis à: X={0}, Y={1}");
			text.AddTranslation(GameCulture.Spanish, "Ajustado a: X={0}, Y={1}");

			AddTranslation(text);

			text = CreateTranslation("SnowBiomeBlock");
			text.SetDefault("Snow Biome Block");
			text.AddTranslation(GameCulture.French, "Bloc de biome de neige");
			text.AddTranslation(GameCulture.Spanish, "Bloque de Biomas de la Nieve");
			AddTranslation(text);

			text = CreateTranslation("DepositAll");
			text.SetDefault("Transfer All");
			text.AddTranslation(GameCulture.Russian, "Переместить всё");
			text.AddTranslation(GameCulture.French, "Déposer tout");
			text.AddTranslation(GameCulture.Spanish, "Depositar todo");
			AddTranslation(text);

			text = CreateTranslation("Search");
			text.SetDefault("Search");
			text.AddTranslation(GameCulture.Russian, "Поиск");
			text.AddTranslation(GameCulture.French, "Rechercher");
			text.AddTranslation(GameCulture.Spanish, "Buscar");
			AddTranslation(text);

			text = CreateTranslation("SearchName");
			text.SetDefault("Search Name");
			text.AddTranslation(GameCulture.Russian, "Поиск по имени");
			text.AddTranslation(GameCulture.French, "Recherche par nom");
			text.AddTranslation(GameCulture.Spanish, "búsqueda por nombre");
			AddTranslation(text);

			text = CreateTranslation("SearchMod");
			text.SetDefault("Search Mod");
			text.AddTranslation(GameCulture.Russian, "Поиск по моду");
			text.AddTranslation(GameCulture.French, "Recherche par mod");
			text.AddTranslation(GameCulture.Spanish, "búsqueda por mod");
			AddTranslation(text);

			text = CreateTranslation("SortDefault");
			text.SetDefault("Default Sorting");
			text.AddTranslation(GameCulture.Russian, "Стандартная сортировка");
			text.AddTranslation(GameCulture.French, "Tri Standard");
			text.AddTranslation(GameCulture.Spanish, "Clasificación por defecto");
			AddTranslation(text);

			text = CreateTranslation("SortID");
			text.SetDefault("Sort by ID");
			text.AddTranslation(GameCulture.Russian, "Сортировка по ID");
			text.AddTranslation(GameCulture.French, "Trier par ID");
			text.AddTranslation(GameCulture.Spanish, "Ordenar por ID");
			AddTranslation(text);

			text = CreateTranslation("SortName");
			text.SetDefault("Sort by Name");
			text.AddTranslation(GameCulture.Russian, "Сортировка по имени");
			text.AddTranslation(GameCulture.French, "Trier par nom");
			text.AddTranslation(GameCulture.Spanish, "Ordenar por nombre");
			AddTranslation(text);
            
		    text = CreateTranslation("SortStack");
		    text.SetDefault("Sort by Stacks");
		    text.AddTranslation(GameCulture.Russian, "Сортировка по стакам");
		    text.AddTranslation(GameCulture.French, "Trier par piles");
		    text.AddTranslation(GameCulture.Spanish, "Ordenar por pilas");
		    AddTranslation(text);

			text = CreateTranslation("SortValue");
			text.SetDefault("Sort by Price");
			AddTranslation(text);

			text = CreateTranslation("FilterAll");
			text.SetDefault("Filter All");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Всё)");
			text.AddTranslation(GameCulture.French, "Filtrer tout");
			text.AddTranslation(GameCulture.Spanish, "Filtrar todo");
			AddTranslation(text);

			text = CreateTranslation("FilterWeapons");
			text.SetDefault("Filter Weapons");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Оружия)");
			text.AddTranslation(GameCulture.French, "Filtrer par armes");
			text.AddTranslation(GameCulture.Spanish, "Filtrar por armas");
			AddTranslation(text);

			text = CreateTranslation("FilterTools");
			text.SetDefault("Filter Tools");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Инструменты)");
			text.AddTranslation(GameCulture.French, "Filtrer par outils");
			text.AddTranslation(GameCulture.Spanish, "Filtrar por herramientas");
			AddTranslation(text);

			text = CreateTranslation("FilterEquips");
			text.SetDefault("Filter Equipment");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Снаряжения)");
			text.AddTranslation(GameCulture.French, "Filtrer par Équipement");
			text.AddTranslation(GameCulture.Spanish, "Filtrar por equipamiento");
			AddTranslation(text);

			text = CreateTranslation("FilterWeaponsMelee");
			text.SetDefault("Filter Melee Weapons");
			AddTranslation(text);

			text = CreateTranslation("FilterWeaponsRanged");
			text.SetDefault("Filter Ranged Weapons");
			AddTranslation(text);

			text = CreateTranslation("FilterWeaponsMagic");
			text.SetDefault("Filter Magic Weapons");
			AddTranslation(text);

			text = CreateTranslation("FilterWeaponsSummon");
			text.SetDefault("Filter Summons");
			AddTranslation(text);

			text = CreateTranslation("FilterWeaponsThrown");
			text.SetDefault("Filter Throwing Weapons");
			AddTranslation(text);

			text = CreateTranslation("FilterAmmo");
			text.SetDefault("Filter Ammo");
			AddTranslation(text);

			text = CreateTranslation("FilterArmor");
			text.SetDefault("Filter Armor");
			AddTranslation(text);

			text = CreateTranslation("FilterVanity");
			text.SetDefault("Filter Vanity Items");
			AddTranslation(text);


            text = CreateTranslation("FilterPotions");
			text.SetDefault("Filter Potions");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Зелья)");
			text.AddTranslation(GameCulture.French, "Filtrer par potions");
			text.AddTranslation(GameCulture.Spanish, "Filtrar por poción");
			AddTranslation(text);

			text = CreateTranslation("FilterTiles");
			text.SetDefault("Filter Placeables");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Размещаемое)");
			text.AddTranslation(GameCulture.French, "Filtrer par placeable");
			text.AddTranslation(GameCulture.Spanish, "Filtrar por metido");
			AddTranslation(text);

			text = CreateTranslation("FilterMisc");
			text.SetDefault("Filter Misc");
			text.AddTranslation(GameCulture.Russian, "Фильтр (Разное)");
			text.AddTranslation(GameCulture.French, "Filtrer par miscellanées");
			text.AddTranslation(GameCulture.Spanish, "Filtrar por otros");
			AddTranslation(text);

			text = CreateTranslation("FilterRecent");
			text.SetDefault("Filter New Recently Added Items");
			AddTranslation(text);

			text = CreateTranslation("CraftingStations");
			text.SetDefault("Crafting Stations");
			text.AddTranslation(GameCulture.Russian, "Станции создания");
			text.AddTranslation(GameCulture.French, "Stations d'artisanat");
			text.AddTranslation(GameCulture.Spanish, "Estaciones de elaboración");
			AddTranslation(text);

			text = CreateTranslation("Recipes");
			text.SetDefault("Recipes");
			text.AddTranslation(GameCulture.Russian, "Рецепты");
			text.AddTranslation(GameCulture.French, "Recettes");
			text.AddTranslation(GameCulture.Spanish, "Recetas");
			AddTranslation(text);

			text = CreateTranslation("SelectedRecipe");
			text.SetDefault("Selected Recipe");
			text.AddTranslation(GameCulture.French, "Recette sélectionnée");
			text.AddTranslation(GameCulture.Spanish, "Receta seleccionada");
			AddTranslation(text);

			text = CreateTranslation("Ingredients");
			text.SetDefault("Ingredients");
			text.AddTranslation(GameCulture.French, "Ingrédients");
			text.AddTranslation(GameCulture.Spanish, "Ingredientes");
			AddTranslation(text);

			text = CreateTranslation("StoredItems");
			text.SetDefault("Stored Ingredients");
			text.AddTranslation(GameCulture.French, "Ingrédients Stockés");
			text.AddTranslation(GameCulture.Spanish, "Ingredientes almacenados");
			AddTranslation(text);

			text = CreateTranslation("RecipeAvailable");
			text.SetDefault("Show new recipes (right click to remove \"new\" flag)");
			AddTranslation(text);

			text = CreateTranslation("RecipeAll");
			text.SetDefault("Show all known recipes");
			AddTranslation(text);

			text = CreateTranslation("RecipeBlacklist");
			text.SetDefault("Show hidden recipes (ctrl+click on recipe to (un)hide)");
			AddTranslation(text);

			text = CreateTranslation("SortDps");
			text.SetDefault("Sort by DPS");
			AddTranslation(text);
            
			text = CreateTranslation("ShowOnlyFavorited");
			text.SetDefault("Only Favorited");
			AddTranslation(text);

			text = CreateTranslation("DepositTooltip");
		    text.SetDefault("Quick Stack - click, Deposit All - ctrl+click, Restock - right click");
			AddTranslation(text);
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
			if (!Main.instance.IsActive)
			{
				return;
			}
			StorageGUI.Update(null);
			CraftingGUI.Update(null);
		}
	}
}


