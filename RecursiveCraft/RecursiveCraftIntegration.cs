using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RecursiveCraft;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using static RecursiveCraft.RecursiveCraft;
using OnPlayer = On.Terraria.Player;

namespace MagicStorageExtra.RecursiveCraft
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
				Initialize(); // Move that logic into another method to prevent this.
		}

		// Be aware of inlining. Inlining can happen at the whim of the runtime. Without this Attribute, this mod happens to crash the 2nd time it is loaded on Linux/Mac. (The first call isn't inlined just by chance.) This can cause headaches. 
		// To avoid TypeInitializationException (or ReflectionTypeLoadException) problems, we need to specify NoInlining on methods like this to prevent inlining (methods containing or accessing Types in the Weakly referenced assembly). 
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void Initialize() {
			// This method will only be called when Enable is true, preventing TypeInitializationException
			members = new RecursiveCraftIntegrationMembers();
			OnPlayer.QuickSpawnItem_int_int += OnPlayerOnQuickSpawnItem_int_int;
		}

		public static void InitRecipe() {
			members.compoundRecipes = new CompoundRecipe[Recipe.maxRecipes];
			for (int i = 0; i < Recipe.maxRecipes; i++) {
				Mod mod = Main.recipe[i].createItem.modItem?.mod;
				members.compoundRecipes[i] = new CompoundRecipe(mod);
			}
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
			OnPlayer.QuickSpawnItem_int_int -= OnPlayerOnQuickSpawnItem_int_int;
		}

		private static void OnPlayerOnQuickSpawnItem_int_int(OnPlayer.orig_QuickSpawnItem_int_int orig, Player self, int type, int stack) {
			if (CraftingGUI.compoundCrafting) {
				var item = new Item();
				item.SetDefaults(type);
				item.stack = stack;
				CraftingGUI.compoundCraftSurplus.Add(item);
				return;
			}
			orig(self, type, stack);
		}

		private static Dictionary<int, int> FlatDict(ICollection<Item> items) {
			var dictionary = new Dictionary<int, int>();
			foreach (Item item in items)
				if (dictionary.ContainsKey(item.netID))
					dictionary[item.netID] += item.stack;
				else
					dictionary[item.netID] = item.stack;
			return dictionary;
		}

		// Make sure to extract the .dll from the .tmod and then add them to your .csproj as references.
		// As a convention, I rename the .dll file ModName_v1.2.3.4.dll and place them in Mod Sources/Mods/lib. 
		// I do this for organization and so the .csproj loads properly for others using the GitHub repository. 
		// Remind contributors to download the referenced mod itself if they wish to build the mod.
		public static void RecursiveRecipes() {
			if (Main.rand == null)
				Main.rand = new UnifiedRandom((int)DateTime.UtcNow.Ticks);
			Player player = Main.LocalPlayer;
			var modPlayer = player.GetModPlayer<StoragePlayer>();
			List<Item> storedItems = modPlayer.GetStorageHeart().GetStoredItems().ToList();

			Dictionary<int, int> storedItemsDict = FlatDict(storedItems);

			lock (BlockRecipes.activeLock) {
				BlockRecipes.active = false;
				RecursiveSearch(storedItemsDict);
				BlockRecipes.active = true;
			}
		}

		private static void RecursiveSearch(Dictionary<int, int> inventory) {
			CraftingSource craftingSource = new GuiAsCraftingSource();
			for (int n = 0; n < Recipe.maxRecipes && Main.recipe[n].createItem.type != ItemID.None; n++) {
				Recipe recipe = Main.recipe[n];
				if (recipe is CompoundRecipe compoundRecipe) {
					recipe = compoundRecipe.OverridenRecipe;
					Main.recipe[n] = recipe;
				}
				RecipeInfo recipeInfo = FindIngredientsForRecipe(inventory, craftingSource, recipe);
				if (recipeInfo != null && recipeInfo.RecipeUsed.Count > 1) {
					members.compoundRecipes[n].Apply(n, recipeInfo);
					Main.recipe[n] = members.compoundRecipes[n];
				}
			}
		}

		public static bool IsCompound(Recipe recipe) => recipe is CompoundRecipe;

		public static Recipe RefreshRecipe(Recipe selectedRecipe) {
			return selectedRecipe;
		}

		private class RecursiveCraftIntegrationMembers
		{
			public CompoundRecipe[] compoundRecipes;
		}
	}
}
