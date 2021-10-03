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
		[Label("Display new items/recipes")]
		[Tooltip("Toggles whether new items in the storage will glow to indicate they're new")]
		[DefaultValue(false)]
		public bool glowNewItems;

		[Label("Use default filter")]
		[Tooltip("Enable to use the filter below, disable to remember last filter selected in game(filter is still used on first open after mod load)")]
		[DefaultValue(true)]
		public bool useConfigFilter;

		[Label("Default recipe filter")]
		[Tooltip("Enable to default to all recipes, disable to default to available recipes")]
		[DefaultValue(false)]
		public bool showAllRecipes;

		[Label("Quick stack deposit mode")]
		[Tooltip("Enable to quick stack with control(ctrl) pressed, disable to quick stack with control(ctrl) released")]
		[DefaultValue(false)]
		public bool quickStackDepositMode;

		[Label("Clear search text")]
		[Tooltip("Enable to clear the search text when opening the UI")]
		[DefaultValue(false)]
		public bool clearSearchText;

		public static MagicStorageConfig Instance => ModContent.GetInstance<MagicStorageConfig>();

		[JsonIgnore]
		public static bool GlowNewItems => Instance.glowNewItems;

		[JsonIgnore]
		public static bool UseConfigFilter => Instance.useConfigFilter;

		[JsonIgnore]
		public static bool ShowAllRecipes => Instance.showAllRecipes;

		[JsonIgnore]
		public static bool QuickStackDepositMode => Instance.quickStackDepositMode;

		[JsonIgnore] public static bool ClearSearchText => Instance.clearSearchText;

		public override ConfigScope Mode => ConfigScope.ClientSide;
	}
}
