using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.Global {
	[Autoload(CanCreate)]
	internal class DebugRandomSaveData : GlobalItem {
		public int randomData;

		public const bool CanCreate = true;

		public override bool InstancePerEntity => true;

		public override bool AppliesToEntity(Item entity, bool lateInstantiation) => CanCreate && lateInstantiation;

		public override void SaveData(Item item, TagCompound tag) {
			tag["randomData"] = randomData;
		}

		public override void LoadData(Item item, TagCompound tag) {
			randomData = tag.GetInt("randomData");
		}
	}
}
