using MagicStorage.CrossMod.Storage;
using Terraria.ModLoader;

namespace MagicStorage.Examples {
	// This file showcases how to modify existing storage unit tiers
	// In this example, the upgrade paths for the Demonite/Crimtane tiers and the Hellstone tier are being modified

	[Autoload(false)]  // Make sure to remove this line when copying this code!
	internal class ExampleStorageTierModifier : StorageTierModifier {
		public override void ModifyUpgradeConnections(StorageUnitTier tier) {
			// Use this method to modify existing upgrade paths between tiers
			// In this example, the upgrade paths for the Demonite/Crimtane tiers and the Hellstone tier
			//   will be severed and the tier from ExampleStorageUnitTier.cs will be inserted between them
			// Since the Demonite/Crimtane tiers are already upgradable to the example tier and the example
			//   tier is upgradable to the Hellstone tier, we just need to remove the existing paths
			StorageUnitTier.Demonite.RemoveConnections(StorageUnitTier.Hellstone);
			StorageUnitTier.Crimtane.RemoveConnections(StorageUnitTier.Hellstone);
		}
	}
}
