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
using RecursiveCraftMod = RecursiveCraft.RecursiveCraft;

namespace MagicStorage
{
	public sealed class RecursiveCraftIntegration : ModSystem
	{
		public static bool Enabled { get; private set; }

		public override void Load()
		{
			Enabled = ModLoader.HasMod("RecursiveCraft");
			if (Enabled)
				StrongRef_Load(); // Move that logic into another method to prevent this.
		}

		// Be aware of inlining. Inlining can happen at the whim of the runtime. Without this Attribute, this mod happens to crash the 2nd time it is loaded on Linux/Mac. (The first call isn't inlined just by chance.) This can cause headaches.
		// To avoid TypeInitializationException (or ReflectionTypeLoadException) problems, we need to specify NoInlining on methods like this to prevent inlining (methods containing or accessing Types in the Weakly referenced assembly).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StrongRef_Load()
		{
			OnPlayer.QuickSpawnItem_IEntitySource_int_int += OnPlayerQuickSpawnItem_IEntitySource_int_int;
		}

		public override void PostAddRecipes()
		{
			if (Enabled)
				StrongRef_PostAddRecipes();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void StrongRef_PostAddRecipes()
		{
			Members.CompoundRecipe = new CompoundRecipe(Mod);
			Members.ThreadCompoundRecipe = new CompoundRecipe(Mod);
		}

		public override void Unload()
		{
			if (Enabled)            // Here we properly unload, making sure to check Enabled before setting RecursiveCraftMod to null.
				StrongRef_Unload(); // Once again we must separate out this logic.
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StrongRef_Unload()
		{
			Members.CompoundRecipe = null;
			Members.ThreadCompoundRecipe = null;

			OnPlayer.QuickSpawnItem_IEntitySource_int_int -= OnPlayerQuickSpawnItem_IEntitySource_int_int;
		}

		private static int OnPlayerQuickSpawnItem_IEntitySource_int_int(OnPlayer.orig_QuickSpawnItem_IEntitySource_int_int orig,
			Player self, IEntitySource source, int type, int stack)
		{
			if (CraftingGUI.compoundCrafting)
			{
				Item item = new(type, stack);
				CraftingGUI.compoundCraftSurplus.Add(item);
				return -1; // return invalid value since this should never be used
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
			Main.rand ??= new UnifiedRandom((int) DateTime.UtcNow.Ticks);
			Dictionary<int, int> storedItems = GetStoredItems();
			if (storedItems == null)
				return;

			lock (RecursiveCraftMod.RecipeInfoCache)
			{
				RecursiveCraftMod.RecipeInfoCache.Clear();
				RecursiveCraftMod.FindRecipes(storedItems);
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
				if (RecursiveCraftMod.RecipeInfoCache.TryGetValue(recipe, out RecipeInfo recipeInfo) && recipeInfo.RecipeUsed?.Count > 1)
					RecursiveCraftMod.RecipeInfoCache.Add(recipe, recipeInfo);
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
				lock (RecursiveCraftMod.RecipeInfoCache)
				{
					RecursiveCraftMod.RecipeInfoCache.Remove(recipe);
					RecursiveCraftMod.FindRecipes(storedItems);
					SingleSearch(recipe);
				}

			return RecursiveCraftMod.RecipeInfoCache.ContainsKey(recipe);
		}

		public static Recipe ApplyCompoundRecipe(Recipe recipe)
		{
			if (recipe == Members.CompoundRecipe.Compound)
				recipe = Members.CompoundRecipe.OverridenRecipe;
			if (!RecursiveCraftMod.RecipeInfoCache.TryGetValue(recipe, out RecipeInfo recipeInfo))
				return recipe;

			int index = Array.IndexOf(Main.recipe, recipe);
			Members.CompoundRecipe.Apply(index, recipeInfo);
			return Members.CompoundRecipe.Compound;
		}

		public static Recipe ApplyThreadCompoundRecipe(Recipe recipe)
		{
			if (!RecursiveCraftMod.RecipeInfoCache.TryGetValue(recipe, out RecipeInfo recipeInfo))
				return recipe;

			int index = Array.IndexOf(Main.recipe, recipe);
			Members.ThreadCompoundRecipe.Apply(index, recipeInfo);
			return Members.ThreadCompoundRecipe.Compound;
		}

		private static class Members
		{
			public static CompoundRecipe CompoundRecipe;
			public static CompoundRecipe ThreadCompoundRecipe;
		}
	}
}
