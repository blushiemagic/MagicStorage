using MagicStorage.Components;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage {
	/// <summary>
	/// A module of information for use in an Environmental Simulator
	/// </summary>
	public abstract class EnvironmentModule : ModType {
		public int Type { get; private set; }

		protected sealed override void Register() {
			ModTypeLookup<EnvironmentModule>.Register(this);
			Type = EnvironmentModuleLoader.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();

		/// <summary>
		/// Allows you to specify what additional items are used in the Storage GUI or Crafting GUI
		/// </summary>
		public virtual IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => null;

		/// <summary>
		/// Allows you to modify the crafting information for the Crafting GUI<br/>
		/// You could also use this hook to set whether "sandbox.player" is in a modded biome for recipe purposes
		/// </summary>
		public virtual void ModifyCraftingZones(EnvironmentSandbox sandbox, ref CraftingInformation information) { }

		/// <summary>
		/// Allows you to specify what happens when an item is consumed for a recipe
		/// </summary>
		/// <param name="item">The original item instance retrieved from <see cref="GetAdditionalItems(EnvironmentSandbox)"/></param>
		public virtual void OnConsumeItemForRecipe(EnvironmentSandbox sandbox, Item item) { }

		/// <summary>
		/// Allows you to reset information in the sandbox's player after processing recipes
		/// </summary>
		public virtual void ResetPlayer(EnvironmentSandbox sandbox) { }
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
		public bool campfire, snow, graveyard, water, lava, honey, alchemyTable;
		public bool[] adjTiles;

		internal CraftingInformation(bool campfire, bool snow, bool graveyard, bool water, bool lava, bool honey, bool alchemyTable, bool[] adjTiles) {
			this.campfire = campfire;
			this.snow = snow;
			this.graveyard = graveyard;
			this.water = water;
			this.lava = lava;
			this.honey = honey;
			this.alchemyTable = alchemyTable;
			this.adjTiles = adjTiles;
		}
	}
}
