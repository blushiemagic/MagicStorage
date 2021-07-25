using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace MagicStorage
{
	public class MagicStorageConfig : ModConfig
	{
		[Label("Display new items/recieps")]
		[Tooltip("Toggles whether new items in the storage will glow to indicate they're new")]
		[DefaultValue(true)]
		public bool _glowNewItems;

		[Label("Default recipe filter")]
		[Tooltip("Enable to default to all recipes, disable to default to available recipes")]
		[DefaultValue(true)]
		public bool _showAllRecipes;

		[Label("Use default filter")]
		[Tooltip("Enable to use the filter below, disable to remember last filter selected in game(filter is still used on first open after mod load)")]
		[DefaultValue(true)]
		public bool _useConfigFilter;

		[JsonIgnore]
		public static bool glowNewItems => ModContent.GetInstance<MagicStorageConfig>()._glowNewItems;

		[JsonIgnore]
		public static bool useConfigFilter => ModContent.GetInstance<MagicStorageConfig>()._useConfigFilter;

		[JsonIgnore]
		public static bool showAllRecipes => ModContent.GetInstance<MagicStorageConfig>()._showAllRecipes;

		public override ConfigScope Mode => ConfigScope.ClientSide;
	}
}
