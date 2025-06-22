using MagicStorage.CrossMod.Storage;

namespace MagicStorage.Items {
	public class StorageCore : BaseStorageCore {
		public override StorageUnitTier Tier => StorageUnitTier.Basic;
	}

	public class StorageCoreDemonite : BaseStorageCore {
		public override StorageUnitTier Tier => StorageUnitTier.Demonite;
	}

	public class StorageCoreCrimtane : BaseStorageCore {
		public override StorageUnitTier Tier => StorageUnitTier.Crimtane;
	}

	public class StorageCoreHellstone : BaseStorageCore {
		public override StorageUnitTier Tier => StorageUnitTier.Hellstone;
	}

	public class StorageCoreHallowed : BaseStorageCore {
		public override StorageUnitTier Tier => StorageUnitTier.Hallowed;
	}

	public class StorageCoreBlueChlorophyte : BaseStorageCore {
		public override StorageUnitTier Tier => StorageUnitTier.BlueChlorophyte;
	}

	public class StorageCoreLuminite : BaseStorageCore {
		public override StorageUnitTier Tier => StorageUnitTier.Luminite;
	}

	public class StorageCoreTerra : BaseStorageCore {
		public override StorageUnitTier Tier => StorageUnitTier.Terra;
	}
}
