using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MagicStorage.Components;
using RecursiveCraft;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using OnPlayer = On.Terraria.Player;

namespace MagicStorage
{
	[JITWhenModsEnabled(RecursiveCraftModName)]
	public static class RecursiveCraftIntegration
	{
		// Here we store a reference to the RecursiveCraft Mod instance. We can use it for many things.
		// You can call all the Mod methods on it just like we do with our own Mod instance: RecursiveCraftMod.ItemType("ExampleItem")
		private static Mod RecursiveCraftMod;

		public const string RecursiveCraftModName = "RecursiveCraft";

		// Here we define a bool property to quickly check if RecursiveCraft is loaded.
		public static bool Enabled => RecursiveCraftMod is not null;

		public static void Load()
		{
			ModLoader.TryGetMod(RecursiveCraftModName, out RecursiveCraftMod);
			if (Enabled)
				StrongRef_Load(); // Move that logic into another method to prevent this.
		}

		// Be aware of inlining. Inlining can happen at the whim of the runtime. Without this Attribute, this mod happens to crash the 2nd time it is loaded on Linux/Mac. (The first call isn't inlined just by chance.) This can cause headaches.
		// To avoid TypeInitializationException (or ReflectionTypeLoadException) problems, we need to specify NoInlining on methods like this to prevent inlining (methods containing or accessing Types in the Weakly referenced assembly).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StrongRef_Load()
		{
			// This method will only be called when Enable is true, preventing TypeInitializationException
			Members.RecipeInfoCache = new Dictionary<Recipe, RecipeInfo>();

			OnPlayer.QuickSpawnItem_IEntitySource_int_int += OnPlayerQuickSpawnItem_IEntitySource_int_int;
		}

		public static void PostAddRecipes()
		{
			if (Enabled)
				StrongRef_PostAddRecipes();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StrongRef_PostAddRecipes()
		{
			Members.CompoundRecipe       = new CompoundRecipe(RecursiveCraftMod);
			Members.ThreadCompoundRecipe = new CompoundRecipe(RecursiveCraftMod);
		}

		public static void Unload()
		{
			if (Enabled) // Here we properly unload, making sure to check Enabled before setting RecursiveCraftMod to null.
				StrongRef_Unload(); // Once again we must separate out this logic.

			RecursiveCraftMod = null; // Make sure to null out any references to allow Garbage Collection to work.
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StrongRef_Unload()
		{
			Members.RecipeInfoCache      = null;
			Members.CompoundRecipe       = null;
			Members.ThreadCompoundRecipe = null;

			OnPlayer.QuickSpawnItem_IEntitySource_int_int -= OnPlayerQuickSpawnItem_IEntitySource_int_int;
		}

		private static int OnPlayerQuickSpawnItem_IEntitySource_int_int(OnPlayer.orig_QuickSpawnItem_IEntitySource_int_int orig,
			Player self, IEntitySource source, int type, int stack)
		{
			if (CraftingGUI.compoundCrafting)
			{
				Item item = new();
				item.SetDefaults(type);
				item.stack = stack;
				CraftingGUI.compoundCraftSurplus.Add(item);
				return -1;
			}

			return orig(self, source, type, stack);
		}

		private static Dictionary<int, int> FlatDict(IEnumerable<Item> items)
		{
			Dictionary<int, int> dictionary = new();
			foreach ((int type, int amount) in items.GroupBy(item => item.type, item => item.stack, (type, stacks) => (type, stacks.Sum())))
				dictionary[type] = amount;
			return dictionary;
		}

		// Make sure to extract the .dll from the .tmod and then add them to your .csproj as references.
		// As a convention, I rename the .dll file ModName_v1.2.3.4.dll and place them in Mod Sources/Mods/lib.
		// I do this for organization and so the .csproj loads properly for others using the GitHub repository.
		// Remind contributors to download the referenced mod itself if they wish to build the mod.
		public static void RecursiveRecipes()
		{
			Main.rand ??= new UnifiedRandom((int)DateTime.UtcNow.Ticks);
			Dictionary<int, int> storedItems = GetStoredItems();
			if (storedItems == null)
				return;

			lock (Members.RecipeInfoCache)
			{
				Members.RecipeInfoCache.Clear();
				RecursiveCraft.RecursiveCraft.FindRecipes(storedItems);
				foreach (Recipe r in Main.recipe)
				{
					Recipe recipe = r;
					if (recipe.createItem.type == ItemID.None)
						break;
					if (recipe == Members.CompoundRecipe.Compound)
						recipe = Members.CompoundRecipe.OverridenRecipe;
					SingleSearch(recipe);
				}
			}
		}

		private static Dictionary<int, int> GetStoredItems()
		{
			Player player = Main.LocalPlayer;
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			TEStorageHeart heart = modPlayer.GetStorageHeart();
			if (heart is not null)
				return FlatDict(heart.GetStoredItems());
			return null;
		}

		private static void SingleSearch(Recipe recipe)
		{
			lock (BlockRecipes.ActiveLock)
			{
				BlockRecipes.Active = false;
				if (RecursiveCraft.RecursiveCraft.RecipeInfoCache.TryGetValue(recipe, out RecipeInfo recipeInfo) && recipeInfo.RecipeUsed?.Count > 1)
					Members.RecipeInfoCache.Add(recipe, recipeInfo);
				BlockRecipes.Active = true;
			}
		}

		public static bool IsCompoundRecipe(Recipe recipe) => recipe == Members.CompoundRecipe.Compound;

		public static Recipe GetOverriddenRecipe(Recipe recipe) => recipe == Members.CompoundRecipe.Compound ? Members.CompoundRecipe.OverridenRecipe : recipe;

		public static bool UpdateRecipe(Recipe recipe)
		{
			if (recipe == Members.CompoundRecipe.Compound)
				recipe = Members.CompoundRecipe.OverridenRecipe;

			Dictionary<int, int> storedItems = GetStoredItems();
			if (storedItems is not null)
				lock (Members.RecipeInfoCache)
				{
					Members.RecipeInfoCache.Remove(recipe);
					RecursiveCraft.RecursiveCraft.FindRecipes(storedItems);
					SingleSearch(recipe);
				}

			return Members.RecipeInfoCache.ContainsKey(recipe);
		}

		public static Recipe ApplyCompoundRecipe(Recipe recipe)
		{
			if (recipe == Members.CompoundRecipe.Compound)
				recipe = Members.CompoundRecipe.OverridenRecipe;
			if (!Members.RecipeInfoCache.TryGetValue(recipe, out RecipeInfo recipeInfo))
				return recipe;

			int index = Array.IndexOf(Main.recipe, recipe);
			Members.CompoundRecipe.Apply(index, recipeInfo);
			return Members.CompoundRecipe.Compound;
		}

		public static Recipe ApplyThreadCompoundRecipe(Recipe recipe)
		{
			if (!Members.RecipeInfoCache.TryGetValue(recipe, out RecipeInfo recipeInfo))
				return recipe;

			int index = Array.IndexOf(Main.recipe, recipe);
			Members.ThreadCompoundRecipe.Apply(index, recipeInfo);
			return Members.ThreadCompoundRecipe.Compound;
		}

		[JITWhenModsEnabled(RecursiveCraftModName)]
		private static class Members
		{
			public static Dictionary<Recipe, RecipeInfo> RecipeInfoCache;
			public static CompoundRecipe CompoundRecipe;
			public static CompoundRecipe ThreadCompoundRecipe;
		}
	}
}
