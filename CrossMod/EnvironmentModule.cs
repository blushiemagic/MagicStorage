using MagicStorage.Common.Systems;
using MagicStorage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage {
	/// <summary>
	/// A module of information for use in a Storage Configuration Interface. Only one instance is assumed to be active at once.
	/// </summary>
	public abstract class EnvironmentModule : ModType, ILocalizedModType {
		/// <summary>
		/// The loader-assigned ID of this environment module.
		/// </summary>
		public int Type { get; private set; }

		/// <inheritdoc/>
		public string LocalizationCategory => "EnvironmentModule";

		/// <summary>
		/// The localized display name shown in the Environment Access UI.
		/// </summary>
		public LocalizedText DisplayName => this.GetLocalization(nameof(DisplayName), PrettyPrintName);

		/// <summary>
		/// The localized tooltip shown when this module is disabled or unavailable.
		/// </summary>
		public LocalizedText DisabledTooltip => this.GetLocalization(nameof(DisabledTooltip), GetDisabledTooltipDefault);

		/// <inheritdoc/>
		protected sealed override void Register() {
			ModTypeLookup<EnvironmentModule>.Register(this);
			Type = EnvironmentModuleLoader.Add(this);

			MagicStorageMod.Instance.Logger.Debug($"EnvironmentModule \"{FullName}\" added by mod \"{Mod.Name}\"");
		}

		/// <inheritdoc/>
		public sealed override void SetupContent() {
			SetStaticDefaults();
		}

		/// <summary>
		/// Gets the fallback disabled tooltip text for environment modules.
		/// </summary>
		public static string GetDisabledTooltipDefault() => Language.GetTextValue("Mods.MagicStorage.EnvironmentGUI.EntryDisabledDefault");

		/// <summary>
		/// Allows you to specify what additional items are used in the Crafting GUI
		/// </summary>
		public virtual IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => null;

		/// <summary>
		/// Allows you to specify what ingredients are considered "infinite" and, thus, aren't consumed when crafting in the Crafting GUI.
		/// </summary>
		public virtual IEnumerable<int> GetInfiniteItems(EnvironmentSandbox sandbox) => null;

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
		/// <param name="sandbox">The crafting environment sandbox that is consuming the item.</param>
		/// <param name="item">The original item instance retrieved from <see cref="GetAdditionalItems(EnvironmentSandbox)"/> or the storage system</param>
		/// <param name="stack">How many items were consumed</param>
		[Obsolete("Use OnconsumeItemsForRecipe instead", true)]
		public virtual void OnConsumeItemForRecipe(EnvironmentSandbox sandbox, Item item, int stack) { }

		/// <summary>
		/// Allows you to specify what happens when items are consumed for a recipe
		/// </summary>
		/// <param name="sandbox">The crafting environment sandbox that is consuming the items.</param>
		/// <param name="recipe">The recipe used</param>
		/// <param name="items">The items consumed for the recipe</param>
		public virtual void OnConsumeItemsForRecipe(EnvironmentSandbox sandbox, Recipe recipe, List<Item> items) { }

		/// <summary>
		/// Allows you to modify how much of an item is consumed when it is used in a recipe
		/// </summary>
		/// <param name="sandbox">The crafting environment sandbox used for the recipe.</param>
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

	/// <summary>
	/// Context passed to environment modules while Magic Storage simulates crafting conditions.
	/// </summary>
	public readonly struct EnvironmentSandbox {
		/// <summary>
		/// The player whose crafting environment is being simulated.
		/// </summary>
		public readonly Player player;
		/// <summary>
		/// The storage heart currently providing crafting access, or <see langword="null"/> when no network is active.
		/// </summary>
		public readonly TEStorageHeart heart;

		internal EnvironmentSandbox(Player player, TEStorageHeart heart) {
			this.player = player;
			this.heart = heart;
		}

		/// <summary>
		/// Returns whether the active storage network contains a creative storage unit.
		/// </summary>
		public bool HeartHasCreativeUnit() => heart is not null && heart.GetStorageUnits().OfType<TECreativeStorageUnit>().Any();

		/// <summary>
		/// Loads the item type IDs that should be treated as infinite while crafting in this sandbox.
		/// </summary>
		public HashSet<int> LoadInfiniteItems() {
			var infiniteItems = InfiniteItemsForCrafting.GetInfiniteItems();
			
			if (heart is not null) {
				foreach (var module in heart.GetModules()) {
					var items = module.GetInfiniteItems(this);

					if (items is not null && items.Any())
						infiniteItems.UnionWith(items);
				}
			}

			return infiniteItems;
		}
	}

	/// <summary>
	/// Snapshot of crafting station, liquid, and biome flags used by recipe availability checks.
	/// </summary>
	public struct CraftingInformation {
		/// <summary>
		/// Whether campfire crafting conditions are active.
		/// </summary>
		public bool campfire;
		/// <summary>
		/// Whether snow biome crafting conditions are active.
		/// </summary>
		public bool snow;
		/// <summary>
		/// Whether graveyard biome crafting conditions are active.
		/// </summary>
		public bool graveyard;
		/// <summary>
		/// Whether water crafting conditions are active.
		/// </summary>
		public bool water;
		/// <summary>
		/// Whether lava crafting conditions are active.
		/// </summary>
		public bool lava;
		/// <summary>
		/// Whether honey crafting conditions are active.
		/// </summary>
		public bool honey;
		/// <summary>
		/// Whether alchemy table crafting conditions are active.
		/// </summary>
		public bool alchemyTable;
		/// <summary>
		/// Whether shimmer crafting conditions are active.
		/// </summary>
		public bool shimmer;
		/// <summary>
		/// The active adjacent tile flags indexed by tile type.
		/// </summary>
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

		/// <summary>
		/// Creates a copy of this crafting information, including a cloned adjacent tile array.
		/// </summary>
		public CraftingInformation Clone() {
			return new CraftingInformation(campfire, snow, graveyard, water, lava, honey, alchemyTable, shimmer, (bool[])adjTiles.Clone());
		}
	}
}
