using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using System.Diagnostics;
using System.Reflection;

namespace MagicStorage.Sorting
{
	public static class ItemSorter
	{
		private enum itemType
		{
			Unknown,
			MeleeWeapon,
			RangedWeapon,
			MagicWeapon,
			SummonWeapon,
			ThrownWeapon,
			Weapon,
			Ammo,
			Picksaw,
			Hamaxe,
			Pickaxe,
			Axe,
			Hammer,
			TerraformingTool,
			AmmoTool,
			Armor,
			VanityArmor,
			Accessory,
			Grapple,
			Mount,
			Cart,
			LightPet,
			VanityPet,
			Dye,
			HairDye,
			HealthPotion,
			ManaPotion,
			Elixir,
			BuffPotion,
			BossSpawn,
			Painting,
			Wiring,
			Material,
			Rope,
			Extractible,
			Misc,
			FrameImportantTile,
			CommonTile
		}

		private static Dictionary<itemType, Func<Item, bool>> chcekType = new Dictionary<itemType, Func<Item, bool>>();

		static ItemSorter()
		{
			foreach (int i in Enum.GetValues(typeof(itemType)))
			{
				if (i != (int)itemType.Unknown)
				{
					MethodInfo methodInfo = typeof(ItemSorter).GetMethod(Enum.GetName(typeof(itemType), i), BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Item) }, null);
					Func<Item, bool> delegateMethod = (Func<Item, bool>)Delegate.CreateDelegate(typeof(Func<Item, bool>), methodInfo);
					chcekType.Add((itemType)i, delegateMethod);
				}
			}
		}

		private static bool MeleeWeapon(Item item)
		{
			return item.maxStack == 1 && item.damage > 0 && item.ammo == 0 && item.melee && item.pick < 1 && item.hammer < 1 && item.axe < 1;
		}

		private static bool RangedWeapon(Item item)
		{
			return item.maxStack == 1 && item.damage > 0 && item.ammo == 0 && item.ranged;
		}

		private static bool MagicWeapon(Item item)
		{
			return item.maxStack == 1 && item.damage > 0 && item.ammo == 0 && item.magic;
		}

		private static bool SummonWeapon(Item item)
		{
			return item.maxStack == 1 && item.damage > 0 && item.summon;
		}

		private static bool ThrownWeapon(Item item)
		{
			return item.damage > 0 && (item.ammo == 0 || item.notAmmo) && item.shoot > 0 && item.thrown;
		}

		private static bool Weapon(Item item)
		{
			return item.damage > 0 && item.ammo == 0 && item.pick == 0 && item.axe == 0 && item.hammer == 0;
		}

		private static bool Ammo(Item item)
		{
			return item.ammo > 0 && item.damage > 0;
		}

		private static bool Picksaw(Item item)
		{
			return item.pick > 0 && item.axe > 0;
		}

		private static bool Hamaxe(Item item)
		{
			return item.hammer > 0 && item.axe > 0;
		}

		private static bool Pickaxe(Item item)
		{
			return item.pick > 0;
		}

		private static bool Axe(Item item)
		{
			return item.axe > 0;
		}

		private static bool Hammer(Item item)
		{
			return item.hammer > 0;
		}

		private static bool TerraformingTool(Item item)
		{
			return ItemID.Sets.SortingPriorityTerraforming[item.type] >= 0;
		}

		private static bool AmmoTool(Item item)
		{
			return item.ammo > 0;
		}

		private static bool Armor(Item item)
		{
			return (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) && !item.vanity;
		}

		private static bool VanityArmor(Item item)
		{
			return (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) && item.vanity;
		}

		private static bool Accessory(Item item)
		{
			return item.accessory;
		}

		private static bool Grapple(Item item)
		{
			return Main.projHook[item.shoot];
		}

		private static bool Mount(Item item)
		{
			return item.mountType != -1 && !MountID.Sets.Cart[item.mountType];
		}

		private static bool Cart(Item item)
		{
			return item.mountType != -1 && MountID.Sets.Cart[item.mountType];
		}

		private static bool LightPet(Item item)
		{
			return item.buffType > 0 && Main.lightPet[item.buffType];
		}

		private static bool VanityPet(Item item)
		{
			return item.buffType > 0 && Main.vanityPet[item.buffType];
		}

		private static bool Dye(Item item)
		{
			return item.dye > 0;
		}

		private static bool HairDye(Item item)
		{
			return item.hairDye >= 0;
		}

		private static bool HealthPotion(Item item)
		{
			return item.consumable && item.healLife > 0 && item.healMana < 1;
		}

		private static bool ManaPotion(Item item)
		{
			return item.consumable && item.healLife < 1 && item.healMana > 0;
		}

		private static bool Elixir(Item item)
		{
			return item.consumable && item.healLife > 0 && item.healMana > 0;
		}

		private static bool BuffPotion(Item item)
		{
			return item.consumable && item.buffType > 0;
		}

		private static bool BossSpawn(Item item)
		{
			return ItemID.Sets.SortingPriorityBossSpawns[item.type] >= 0;
		}

		private static bool Painting(Item item)
		{
			return ItemID.Sets.SortingPriorityPainting[item.type] >= 0 || item.paint > 0;
		}

		private static bool Wiring(Item item)
		{
			return ItemID.Sets.SortingPriorityWiring[item.type] >= 0 || item.mech;
		}

		private static bool Material(Item item)
		{
			return ItemID.Sets.SortingPriorityMaterials[item.type] >= 0;
		}

		private static bool Rope(Item item)
		{
			return ItemID.Sets.SortingPriorityRopes[item.type] >= 0;
		}

		private static bool Extractible(Item item)
		{
			return ItemID.Sets.SortingPriorityExtractibles[item.type] >= 0;
		}

		private static bool Misc(Item item)
		{
			return item.createTile < 0 && item.createWall < 1;
		}

		private static bool FrameImportantTile(Item item)
		{
			return item.createTile >= 0 && Main.tileFrameImportant[item.createTile];
		}

		private static bool CommonTile(Item item)
		{
			return item.createTile >= 0 || item.createWall > 0;
		}

		private static itemType getItemType(Item item)
		{
			itemType result = itemType.Unknown;

			foreach (var kv in chcekType)
			{
				if (kv.Value(item))
				{
					result = kv.Key;
					break;
				}
			}

			return result;
		}

		private static Stopwatch watch = Stopwatch.StartNew();
		public static IEnumerable<Item> SortAndFilter(IEnumerable<Item> items, SortMode sortMode, FilterMode filterMode, string modFilter, string nameFilter)
		{
			//long ticks = watch.ElapsedTicks;
			ItemFilter filter;
			switch (filterMode)
			{
				case FilterMode.All:
					filter = new FilterAll();
					break;
				case FilterMode.Weapons:
					filter = new FilterWeapon();
					break;
				case FilterMode.Tools:
					filter = new FilterTool();
					break;
				case FilterMode.Equipment:
					filter = new FilterEquipment();
					break;
				case FilterMode.Potions:
					filter = new FilterPotion();
					break;
				case FilterMode.Placeables:
					filter = new FilterPlaceable();
					break;
				case FilterMode.Misc:
					filter = new FilterMisc();
					break;
				default:
					filter = new FilterAll();
					break;
			}

			List<Item> filteredItems;
			if (sortMode == SortMode.Default)
			{
				Dictionary<itemType, SortedDictionary<long, Item>> dic = new Dictionary<itemType, SortedDictionary<long, Item>>();
				foreach (itemType itemType in chcekType.Keys)
				{
					dic.Add(itemType, new SortedDictionary<long, Item>());
				}
				dic.Add(itemType.Unknown, new SortedDictionary<long, Item>());
				foreach (Item item in items)
				{
					if (!item.IsAir && filter.Passes(item) && FilterName(item, modFilter, nameFilter))
					{
						itemType iType = getItemType(item);
						long hash = 1000 * item.type + item.prefix;
						if (!dic[iType].ContainsKey(hash))
						{
							dic[iType].Add(hash, item.Clone());
						}
						else
						{
							dic[iType][hash].stack += item.stack;
						}
					}
				}

				filteredItems = new List<Item>();
				foreach (var itemGroup in dic.Values)
				{
					foreach (Item item in itemGroup.Values)
					{
						filteredItems.Add(item);
					}
				}
			}
			else
			{
				Dictionary<long, Item> dic = new Dictionary<long, Item>();
				foreach (Item item in items)
				{
					if (!item.IsAir && filter.Passes(item) && FilterName(item, modFilter, nameFilter))
					{
						long hash = 1000 * item.type + item.prefix;
						if (!dic.ContainsKey(hash))
						{
							dic.Add(hash, item.Clone());
						}
						else
						{
							dic[hash].stack += item.stack;
						}
					}
				}

				filteredItems = dic.Values.ToList();
				CompareFunction func;
				switch (sortMode)
				{
					case SortMode.Id:
						func = new CompareID();
						break;
					case SortMode.Name:
						func = new CompareName();
						break;
					case SortMode.Quantity:
						func = new CompareQuantity();
						break;
					default:
						return filteredItems;
				}
				filteredItems.Sort((i1, i2) => func.Compare(i1, i2));
			}
			//Main.NewText($"Item Sorting took: {watch.ElapsedTicks - ticks} ticks, items count: {items.Count()}, filtered count: {filteredItems.Count()}");
			return filteredItems;
		}

		public static IEnumerable<Recipe> GetRecipes(SortMode sortMode, FilterMode filterMode, string modFilter, string nameFilter)
		{
			//long ticks = watch.ElapsedTicks;
			ItemFilter filter;
			switch (filterMode)
			{
				case FilterMode.All:
					filter = new FilterAll();
					break;
				case FilterMode.Weapons:
					filter = new FilterWeapon();
					break;
				case FilterMode.Tools:
					filter = new FilterTool();
					break;
				case FilterMode.Equipment:
					filter = new FilterEquipment();
					break;
				case FilterMode.Potions:
					filter = new FilterPotion();
					break;
				case FilterMode.Placeables:
					filter = new FilterPlaceable();
					break;
				case FilterMode.Misc:
					filter = new FilterMisc();
					break;
				default:
					filter = new FilterAll();
					break;
			}
			List<Recipe> filteredRecipes = new List<Recipe>();

			if (sortMode == SortMode.Default)
			{
				Dictionary<itemType, SortedDictionary<long, Recipe>> dic = new Dictionary<itemType, SortedDictionary<long, Recipe>>();
				foreach (itemType itemType in chcekType.Keys)
				{
					dic.Add(itemType, new SortedDictionary<long, Recipe>());
				}
				dic.Add(itemType.Unknown, new SortedDictionary<long, Recipe>());
				for (int i = 0; i < Main.recipe.Length; ++i)
				{
					Recipe recipe = Main.recipe[i];
					Item item = recipe.createItem;
					if (!item.IsAir && filter.Passes(recipe) && FilterName(item, modFilter, nameFilter))
					{
						itemType iType = getItemType(item);
						long hash = 1000 * item.type + i;
						dic[iType].Add(hash, recipe);
					}
				}

				filteredRecipes = new List<Recipe>();
				foreach (var recipeGroup in dic.Values)
				{
					foreach (Recipe recipe in recipeGroup.Values)
					{
						filteredRecipes.Add(recipe);
					}
				}
			}
			else
			{
				for (int i = 0; i < Main.recipe.Length; ++i)
				{
					Recipe recipe = Main.recipe[i];
					Item item = recipe.createItem;
					if (!item.IsAir && filter.Passes(recipe) && FilterName(item, modFilter, nameFilter))
					{
						filteredRecipes.Add(recipe);
					}
				}

				CompareFunction func;
				switch (sortMode)
				{
					case SortMode.Id:
						func = new CompareID();
						break;
					case SortMode.Name:
						func = new CompareName();
						break;
					case SortMode.Quantity:
						func = new CompareQuantity();
						break;
					default:
						return filteredRecipes;
				}
				filteredRecipes.Sort((i1, i2) => func.Compare(i1, i2));
			}
			//Main.NewText($"Recipe Sorting took: {watch.ElapsedTicks - ticks} ticks, recipe count: {Main.recipe.Length}");
			return filteredRecipes;
		}

		private static bool FilterName(Item item, string modFilter, string filter)
		{
			string modName = "Terraria";
			if (item.modItem != null)
			{
				modName = item.modItem.mod.DisplayName;
			}
			return modName.ToLowerInvariant().IndexOf(modFilter.ToLowerInvariant()) >= 0 && item.Name.ToLowerInvariant().IndexOf(filter.ToLowerInvariant()) >= 0;
		}
	}
}
