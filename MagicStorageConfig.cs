using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MagicStorageExtra
{
	public class MagicStorageConfig : ModConfig
	{

		[Label("Display new items/recieps")]
		[Tooltip("Toggles whether new items in the storage will glow to indicate they're new")]
		[DefaultValue(true)]
		public bool glowNewItems;

		public override ConfigScope Mode => ConfigScope.ClientSide;
	}
}
