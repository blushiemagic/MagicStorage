using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RecursiveCraft;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static RecursiveCraft.RecursiveCraft;

namespace MagicStorageExtra
{
	public class RecursiveCraftIntegration
	{
		// Here we store a reference to the RecursiveCraft Mod instance. We can use it for many things. 
		// You can call all the Mod methods on it just like we do with our own Mod instance: RecursiveCraftMod.ItemType("ShadowDiamond")
		private static Mod RecursiveCraftMod;

		private static RecursiveCraftIntegrationMembers members;

		// Here we define a bool property to quickly check if RecursiveCraft is loaded. 
		public static bool Enabled => RecursiveCraftMod != null;

		public static void Load() {
			RecursiveCraftMod = ModLoader.GetMod("RecursiveCraft");
			if (Enabled)
				//RecursiveCraftIntegrationMembers.tile = null; // Will also crash. Here even though Enabled will be false, the Type of tile will still need to be resolved when this method runs.
				//members = new RecursiveCraftIntegrationMembers(); // Even thought the Type StorageAccess is hidden behind RecursiveCraftIntegrationMembers, this line will also cause RecursiveCraftIntegrationMembers and consequently StorageAccess to need to be resolved.
				Initialize(); // Move that logic into another method to prevent this.
		}

		// Be aware of inlining. Inlining can happen at the whim of the runtime. Without this Attribute, this mod happens to crash the 2nd time it is loaded on Linux/Mac. (The first call isn't inlined just by chance.) This can cause headaches. 
		// To avoid TypeInitializationException (or ReflectionTypeLoadException) problems, we need to specify NoInlining on methods like this to prevent inlining (methods containing or accessing Types in the Weakly referenced assembly). 
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void Initialize() {
			// This method will only be called when Enable is true, preventing TypeInitializationException
			//RecursiveCraftIntegrationMembers.compoundRecipes = new List<CompoundRecipe>();
			members = new RecursiveCraftIntegrationMembers {
				compoundRecipes = new List<CompoundRecipe>()
			};
		}

		public static void Unload() {
			if (Enabled) // Here we properly unload, making sure to check Enabled before setting RecursiveCraftMod to null.
				Unload_Inner(); // Once again we must separate out this logic.
			RecursiveCraftMod = null; // Make sure to null out any references to allow Garbage Collection to work.
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void Unload_Inner() {
			//RecursiveCraftIntegrationMembers.compoundRecipes = null;
			members = null;
		}

		private static Dictionary<int, int> FlatDict(ICollection<Item> items) {
			var flatItems = new List<Item>();
			foreach (int type in items.Select(item => item.type).Distinct()) {
				Item flatItem = items.Where(item => item.type == type).Aggregate((item1, item2) => {
					Item item = item1.Clone();
					item.stack += item2.stack;
					return item;
				});
				flatItems.Add(flatItem);
			}
			return flatItems.ToDictionary(item => item.type, item => item.stack);
		}

		public static void Test() {
			Player player = Main.player[Main.myPlayer];
			var modPlayer = player.GetModPlayer<StoragePlayer>();
			List<Item> storedItems = modPlayer.GetStorageHeart().GetStoredItems().ToList();

			RecursiveSearch(FlatDict(storedItems));
		}

		// Make sure to extract the .dll from the .tmod and then add them to your .csproj as references.
		// As a convention, I rename the .dll file ModName_v1.2.3.4.dll and place them in Mod Sources/Mods/lib. 
		// I do this for organization and so the .csproj loads properly for others using the GitHub repository. 
		// Remind contributors to download the referenced mod itself if they wish to build the mod.
		public static IEnumerable<(int recipeId, Recipe recipe)> RecursiveRecipes() {
			Player player = Main.player[Main.myPlayer];
			var modPlayer = player.GetModPlayer<StoragePlayer>();
			List<Item> storedItems = modPlayer.GetStorageHeart().GetStoredItems().ToList();

			Dictionary<int, int> storedItemsDict = FlatDict(storedItems);

			bool[] oldAdjTile = player.adjTile;
			bool oldAdjWater = player.adjWater;
			bool oldAdjLava = player.adjLava;
			bool oldAdjHoney = player.adjHoney;
			bool oldAlchemyTable = player.alchemyTable;
			bool oldZoneSnow = player.ZoneSnow;

			player.adjTile = CraftingGUI.adjTiles;
			player.adjWater = CraftingGUI.adjWater;
			player.adjLava = CraftingGUI.adjLava;
			player.adjHoney = CraftingGUI.adjHoney;
			player.alchemyTable = CraftingGUI.alchemyTable;
			player.ZoneSnow = CraftingGUI.zoneSnow;
			lock (BlockRecipes.activeLock) {
				BlockRecipes.active = false;
				RecursiveSearch(storedItemsDict);
				BlockRecipes.active = true;
			}
			player.adjTile = oldAdjTile;
			player.adjWater = oldAdjWater;
			player.adjLava = oldAdjLava;
			player.adjHoney = oldAdjHoney;
			player.alchemyTable = oldAlchemyTable;
			player.ZoneSnow = oldZoneSnow;

			return members.compoundRecipes.Select(recipe => (recipe.recipeId, recipe.currentRecipe));
		}

		private static void RecursiveSearch(Dictionary<int, int> inventory) {
			members.compoundRecipes.Clear();
			for (int i = 0; i < Recipe.maxRecipes && Main.recipe[i].createItem.type != ItemID.None; i++)
				SearchRecipe(inventory, i);
		}

		private static void SearchRecipe(Dictionary<int, int> inventory, int i) {
			Recipe recipe = Main.recipe[i];
			var inventoryToUse = new Dictionary<int, int>(inventory);
			Dictionary<int, int> inventoryOnceUsed = inventoryToUse;
			var craftedItems = new List<int>();
			if (AmountOfDoableRecipe(ref inventoryOnceUsed, recipe.createItem.stack, recipe, craftedItems, 0) == 0)
				return;
			if (inventoryOnceUsed != inventoryToUse) {
				var usedItems = new Dictionary<int, int>();
				foreach (KeyValuePair<int, int> keyValuePair in inventoryOnceUsed) {
					if (!inventoryToUse.TryGetValue(keyValuePair.Key, out int amount))
						amount = 0;
					amount -= keyValuePair.Value;
					if (amount != 0)
						usedItems.Add(keyValuePair.Key, amount);
				}

				members.compoundRecipes.Add(new CompoundRecipe(i, usedItems));
				return;
			}

			members.compoundRecipes.Add(new CompoundRecipe(i, FlatDict(recipe.requiredItem)));
		}

		private class RecursiveCraftIntegrationMembers
		{
			public List<CompoundRecipe> compoundRecipes;
		}
	}
}
