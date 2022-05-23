using MagicStorage.Edits;
using MagicStorage.Items;
using MagicStorage.Stations;
using Microsoft.Xna.Framework.Input;
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

		// TODO can these 2 be const?
		public static string GithubUserName => "blushiemagic";
		public static string GithubProjectName => "MagicStorage";

		public static ModKeybind IsItemKnownHotKey { get; private set; }

		public static ImmutableArray<Mod> AllMods { get; private set; }
		public static Dictionary<Mod, int> IndexByMod { get; private set; }

		// TODO: text prompt to input exact amount of items wanted (hint: make prompt update to max possible, should a user input more, and to 0 should a user input a negative number/invalid string)

		public static readonly Version requiredVersion = new Version(0, 12);

		public override void Load()
		{
			if (TModLoaderVersion < requiredVersion)
				throw new Exception("Magic storage requires a tModLoader version of at least " + requiredVersion);

			InterfaceHelper.Initialize();
			AddTranslations();
			IsItemKnownHotKey = KeybindLoader.RegisterKeybind(this, "Is This Item Known?", Keys.Q);

			EditsLoader.Load();

			DirectDetourManager.Load();
		}

		public override void Unload()
		{
			IsItemKnownHotKey = null;
			StorageGUI.Unload();
			CraftingGUI.Unload();

			DirectDetourManager.Unload();
		}

		private void AddTranslations() {
			ModTranslation text = LocalizationLoader.CreateTranslation(this, "SetTo");
			text.SetDefault("Set to: X={0}, Y={1}");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Ustawione na: X={0}, Y={1}");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Mis à: X={0}, Y={1}");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Ajustado a: X={0}, Y={1}");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "已设置为: X={0}, Y={1}");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SnowBiomeBlock");
			text.SetDefault("Snow Biome Block");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Bloc de biome de neige");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Bloque de Biomas de la Nieve");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "雪地环境方块");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "DepositAll");
			text.SetDefault("Deposit All");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Переместить всё");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Déposer tout");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Depositar todo");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "全部存入");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "Search");
			text.SetDefault("Search");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Поиск");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Rechercher");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Buscar");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "搜索");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SearchName");
			text.SetDefault("Search Name");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Поиск по имени");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Recherche par nom");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "búsqueda por nombre");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "搜索名称");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "CraftAmount");
			text.SetDefault("Craft amount");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SearchMod");
			text.SetDefault("Search Mod");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Поиск по моду");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Recherche par mod");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "búsqueda por mod");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "搜索模组");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SortDefault");
			text.SetDefault("Default Sorting");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Стандартная сортировка");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Tri Standard");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Clasificación por defecto");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "默认排序");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SortID");
			text.SetDefault("Sort by ID");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Сортировка по ID");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Trier par ID");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Ordenar por ID");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "按ID排序");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SortName");
			text.SetDefault("Sort by Name");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Сортировка по имени");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Trier par nom");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Ordenar por nombre");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "按名称排序");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SortStack");
			text.SetDefault("Sort by Stacks");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Сортировать по стопкам");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Trier par piles");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Ordenar por pilas");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "按堆栈排序");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SortValue");
			text.SetDefault("Sort by Value");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Сортировать по значению");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Trier par valeur");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Ordenar por valor");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "按值排序");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterAll");
			text.SetDefault("Filter All");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Фильтр (Всё)");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Filtrer tout");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Filtrar todo");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "筛选全部");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterWeapons");
			text.SetDefault("Filter Weapons");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Фильтр (Оружия)");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Filtrer par armes");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Filtrar por armas");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "筛选武器");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterTools");
			text.SetDefault("Filter Tools");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Фильтр (Инструменты)");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Filtrer par outils");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Filtrar por herramientas");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "筛选工具");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterEquips");
			text.SetDefault("Filter Equipment");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Фильтр (Снаряжения)");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Filtrer par Équipement");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Filtrar por equipamiento");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "筛选装备");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterWeaponsMelee");
			text.SetDefault("Filter Melee Weapons");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterWeaponsRanged");
			text.SetDefault("Filter Ranged Weapons");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterWeaponsMagic");
			text.SetDefault("Filter Magic Weapons");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterWeaponsSummon");
			text.SetDefault("Filter Summons");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterWeaponsThrown");
			text.SetDefault("Filter Throwing Weapons");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterAmmo");
			text.SetDefault("Filter Ammo");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterArmor");
			text.SetDefault("Filter Armor");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterVanity");
			text.SetDefault("Filter Vanity Items");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterPotions");
			text.SetDefault("Filter Potions");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Фильтр (Зелья)");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Filtrer par potions");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Filtrar por poción");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "筛选药水");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterTiles");
			text.SetDefault("Filter Placeables");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Фильтр (Размещаемое)");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Filtrer par placeable");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Filtrar por metido");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "筛选放置物");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterMisc");
			text.SetDefault("Filter Misc");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Фильтр (Разное)");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Filtrer par miscellanées");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Filtrar por otros");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "筛选杂项");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "FilterRecent");
			text.SetDefault("Filter New Recently Added Items");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "CraftingStations");
			text.SetDefault("Crafting Stations");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Станции создания");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Stations d'artisanat");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Estaciones de elaboración");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "制作站");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "Recipes");
			text.SetDefault("Recipes");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Рецепты");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Recettes");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Recetas");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "合成配方");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SelectedRecipe");
			text.SetDefault("Selected Recipe");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Recette sélectionnée");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Receta seleccionada");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "选择配方");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "Ingredients");
			text.SetDefault("Ingredients");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Ingrédients");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Ingredientes");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "材料");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "StoredItems");
			text.SetDefault("Stored Ingredients");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Ingrédients Stockés");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Ingredientes almacenados");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "存储中的材料");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "RecipeAvailable");
			text.SetDefault("Show available recipes");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Afficher les recettes disponibles");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Mostrar recetas disponibles");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "显示可合成配方");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "RecipeAll");
			text.SetDefault("Show all known recipes");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Afficher toutes les recettes");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Mostrar todas las recetas");
			text.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "显示全部配方");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "RecipeBlacklist");
			text.SetDefault("Show hidden recipes (ctrl+click on recipe to (un)hide)");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "SortDps");
			text.SetDefault("Sort by DPS");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "ShowOnlyFavorited");
			text.SetDefault("Only Favorited");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "DepositTooltip");
			text.SetDefault("Quick Stack - click, Deposit All - ctrl+click, Restock - right click");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "DepositTooltipAlt");
			text.SetDefault("Quick Stack - ctrl+click, Deposit All - click, Restock - right click");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "CraftTooltip");

			//See explanation in CraftingGUI.UpdateCraftButton() for why the test feature was removed
			// text.SetDefault("Left click to Craft, Right click to get item for a test (only for new items)");
			text.SetDefault("Left click to Craft (ctrl to get max)");
			LocalizationLoader.AddTranslation(text);

			text = LocalizationLoader.CreateTranslation(this, "TestItemSuffix");
			text.SetDefault(" !UNTIL RESPAWN!");
			LocalizationLoader.AddTranslation(text);
		}

		public override void AddRecipes(){
			CreateRecipe(ItemID.CookedMarshmallow)
				.AddIngredient(ItemID.Marshmallow)
				.AddCondition(new Recipe.Condition(NetworkText.FromLiteral("Biome Globe in a Crafting Interface"), recipe => CraftingGUI.Campfire))
				.Register();
		}

		public override void PostAddRecipes() {
			//Make a copy of every recipe that requires Ecto Mist, but let it be crafted at the appropriate combined station(s) as well
			for (int i = 0; i < Recipe.maxRecipes; i++)
			{
				Recipe recipe = Main.recipe[i];

				if (recipe.HasCondition(Recipe.Condition.InGraveyardBiome))
				{
					Recipe copy = CreateRecipe(recipe.createItem.type, recipe.createItem.stack);

					foreach (Item item in recipe.requiredItem)
						copy.AddIngredient(item.type, item.stack);

					copy.acceptedGroups = new List<int>(recipe.acceptedGroups);

					copy.requiredTile = new List<int>(recipe.requiredTile) { ModContent.TileType<CombinedStations4Tile>() };

					//Copy all conditions except the graveyard one
					copy.AddCondition(recipe.Conditions.Where(cond => cond != Recipe.Condition.InGraveyardBiome));

					copy.Register();
				}
			}
		}

		public override void PostSetupContent()
		{
			AllMods = ModLoader.Mods.Skip(1)
				.Where(mod => !mod.Name.EndsWith("Library", StringComparison.OrdinalIgnoreCase))
				.Where(mod => mod.GetContent<ModItem>().Any())
				.ToImmutableArray();

			IndexByMod = new();

			for (int i = 0; i < AllMods.Length; i++)
			{
				var mod = AllMods[i];
				IndexByMod[mod] = i;
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
	}
}
