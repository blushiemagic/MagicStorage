using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Components {
	public class TEEnvironmentAccess : TEStoragePoint {
		internal List<EnvironmentModule> modules = new();

		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<EnvironmentAccess>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

		//Copies of the hooks in EnvironmentModule
		public IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => modules.SelectMany(m => m.GetAdditionalItems(sandbox) ?? Array.Empty<Item>());

		public void ModifyCraftingZones(EnvironmentSandbox sandbox, ref CraftingInformation information) {
			foreach (EnvironmentModule module in modules)
				module.ModifyCraftingZones(sandbox, ref information);
		}

		public void OnConsumeItemForRecipe(EnvironmentSandbox sandbox, Item item) {
			foreach (EnvironmentModule module in modules)
				module.OnConsumeItemForRecipe(sandbox, item);
		}

		public void ResetPlayer(EnvironmentSandbox sandbox) {
			foreach (EnvironmentModule module in modules)
				module.ResetPlayer(sandbox);
		}
	}
}
