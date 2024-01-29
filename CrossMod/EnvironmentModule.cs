using MagicStorage.Common.Systems;
using MagicStorage.Components;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage {
	/// <summary>
	/// A module of information for use in a Storage Configuration Interface. Only one instance is assumed to be active at once.
	/// </summary>
	public abstract class EnvironmentModule : ModType, ILocalizedModType {
		public int Type { get; private set; }

		public string LocalizationCategory => "EnvironmentModule";

		public LocalizedText DisplayName => this.GetLocalization(nameof(DisplayName), PrettyPrintName);

		public LocalizedText DisabledTooltip => this.GetLocalization(nameof(DisabledTooltip), GetDisabledTooltipDefault);

		protected sealed override void Register() {
			ModTypeLookup<EnvironmentModule>.Register(this);
			Type = EnvironmentModuleLoader.Add(this);

			MagicStorageMod.Instance.Logger.Debug($"EnvironmentModule \"{FullName}\" added by mod \"{Mod.Name}\"");
		}

		public sealed override void SetupContent() {
			SetStaticDefaults();
		}

		public static string GetDisabledTooltipDefault() => Language.GetTextValue("Mods.MagicStorage.EnvironmentGUI.EntryDisabledDefault");

		/// <summary>
		/// Allows you to specify what additional items are used in the Crafting GUI
		/// </summary>
		public virtual IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => null;

		/// <summary>
		/// Allows you to specify which additional recipes should be refreshed when depositing or withdrawing <paramref name="stationItem"/> from the Station Slots in the Crafting UI.<br/>
		/// Use of the various collections in <see cref="MagicCache"/> or your own cached recipe collections is recommended.
		/// </summary>
		public virtual IEnumerable<Recipe> GetRecipesToRefresh(Item stationItem) => null;

		/// <summary>
		/// Allows you to modify the crafting information for the Crafting GUI<br/>
		/// You could also use this hook to set whether "sandbox.player" is in a modded biome for recipe purposes
		/// </summary>
		public virtual void ModifyCraftingZones(EnvironmentSandbox sandbox, ref CraftingInformation information) { }

		/// <summary>
		/// Allows you to specify what happens when an item is consumed for a recipe
		/// </summary>
		/// <param name="item">The original item instance retrieved from <see cref="GetAdditionalItems(EnvironmentSandbox)"/> or the storage system</param>
		/// <param name="stack">How many items were consumed</param>
		[Obsolete("Use OnconsumeItemsForRecipe instead", true)]
		public virtual void OnConsumeItemForRecipe(EnvironmentSandbox sandbox, Item item, int stack) { }

		/// <summary>
		/// Allows you to specify what happens when items are consumed for a recipe
		/// </summary>
		/// <param name="recipe">The recipe used</param>
		/// <param name="items">The items consumed for the recipe</param>
		public virtual void OnConsumeItemsForRecipe(EnvironmentSandbox sandbox, Recipe recipe, List<Item> items) { }

		/// <summary>
		/// Allows you to modify how much of an item is consumed when it is used in a recipe
		/// </summary>
		/// <param name="recipe">The recipe used</param>
		/// <param name="type">The ID of the required item from the recipe</param>
		/// <param name="stack">The quantity of the item that should be consumed</param>
		public virtual void ConsumeItemForRecipe(EnvironmentSandbox sandbox, Recipe recipe, int type, ref int stack) { }

		/// <summary>
		/// Allows you to reset information in the sandbox's player after processing recipes
		/// </summary>
		public virtual void ResetPlayer(EnvironmentSandbox sandbox) { }

		/// <summary>
		/// Allows you to determine when this module is available for use
		/// </summary>
		public virtual bool IsAvailable() => true;

		/// <summary>
		/// Allows you to run logic before recipes are refreshed
		/// </summary>
		public virtual void PreRefreshRecipes(EnvironmentSandbox sandbox) { }

		/// <summary>
		/// Allows you to run logic after recipes are refreshed
		/// </summary>
		public virtual void PostRefreshRecipes(EnvironmentSandbox sandbox) { }

		/// <summary>
		/// Allows you to run logic before the UIs in Magic Storage are updated
		/// </summary>
		public virtual void PreUpdateUI() { }

		/// <summary>
		/// Allows you to run logic after the UIs in Magic Storage are updated
		/// </summary>
		public virtual void PostUpdateUI() { }
	}

	public readonly struct EnvironmentSandbox {
		public readonly Player player;
		public readonly TEStorageHeart heart;

		internal EnvironmentSandbox(Player player, TEStorageHeart heart) {
			this.player = player;
			this.heart = heart;
		}
	}

	public struct CraftingInformation {
		public bool campfire, snow, graveyard, water, lava, honey, alchemyTable, shimmer;
		public bool[] adjTiles;

		internal CraftingInformation(bool campfire, bool snow, bool graveyard, bool water, bool lava, bool honey, bool alchemyTable, bool shimmer, bool[] adjTiles) {
			this.campfire = campfire;
			this.snow = snow;
			this.graveyard = graveyard;
			this.water = water;
			this.lava = lava;
			this.honey = honey;
			this.alchemyTable = alchemyTable;
			this.shimmer = shimmer;
			this.adjTiles = adjTiles;
		}
	}
}