using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace MagicStorageExtra
{
	public class MagicStorageConfig : ModConfig
	{

		[Label("Display new items/recieps")]
		[Tooltip("Toggles whether new items in the storage will glow to indicate they're new")]
		[DefaultValue(true)]
		public bool _glowNewItems;

		[Label("Whether the below filter should be used")]
		[Tooltip("Enable to use the filter")]
		[DefaultValue(true)]
		public bool _useConfigFilter;

		[Label("Default to showing all recipes")]
		[Tooltip("Enable to default to all recipes, disable to default to available recipes")]
		[DefaultValue(true)]
		public bool _showAllRecipes;

		[JsonIgnore]
		public static bool glowNewItems => ModContent.GetInstance<MagicStorageConfig>()._glowNewItems;
		[JsonIgnore]
		public static bool useConfigFilter => ModContent.GetInstance<MagicStorageConfig>()._useConfigFilter;
		[JsonIgnore]
		public static bool showAllRecipes => ModContent.GetInstance<MagicStorageConfig>()._showAllRecipes;

		public override ConfigScope Mode => ConfigScope.ClientSide;
	}
}
