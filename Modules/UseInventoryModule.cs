using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Modules {
	internal class UseInventoryModule : EnvironmentModule {
		public override IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => sandbox.player.inventory.Take(50);
	}
}
