using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace MagicStorage
{
	[Label("$Mods.MagicStorage.Config.Label")]
	public class MagicStorageConfig : ModConfig
	{
		[Label("$Mods.MagicStorage.Config.glowNewItems.Label")]
		[Tooltip("$Mods.MagicStorage.Config.glowNewItems.Tooltip")]
		[DefaultValue(false)]
		public bool glowNewItems;

		[Label("$Mods.MagicStorage.Config.useConfigFilter.Label")]
		[Tooltip("$Mods.MagicStorage.Config.useConfigFilter.Tooltip")]
		[DefaultValue(true)]
		public bool useConfigFilter;

		[Label("$Mods.MagicStorage.Config.showAllRecipes.Label")]
		[Tooltip("$Mods.MagicStorage.Config.showAllRecipes.Tooltip")]
		[DefaultValue(false)]
		public bool showAllRecipes;

		[Label("$Mods.MagicStorage.Config.quickStackDepositMode.Label")]
		[Tooltip("$Mods.MagicStorage.Config.quickStackDepositMode.Tooltip")]
		[DefaultValue(false)]
		public bool quickStackDepositMode;

		[Label("$Mods.MagicStorage.Config.clearSearchText.Label")]
		[Tooltip("$Mods.MagicStorage.Config.clearSearchText.Tooltip")]
		[DefaultValue(false)]
		public bool clearSearchText;

		[Label("$Mods.MagicStorage.Config.extraFilterIcons.Label")]
		[Tooltip("$Mods.MagicStorage.Config.extraFilterIcons.Tooltip")]
		[DefaultValue(true)]
		[ReloadRequired]
		public bool extraFilterIcons;

		[Label("$Mods.MagicStorage.Config.showDps.Label")]
		[Tooltip("$Mods.MagicStorage.Config.showDps.Tooltip")]
		[DefaultValue(true)]
		[ReloadRequired]
		public bool showDps;

		[Label("$Mods.MagicStorage.Config.useOldCraftMenu.Label")]
		[Tooltip("$Mods.MagicStorage.Config.useOldCraftMenu.Tooltip")]
		[DefaultValue(false)]
		public bool useOldCraftMenu;

		[Label("$Mods.MagicStorage.Config.allowItemDataDebug.Label")]
		[Tooltip("$Mods.MagicStorage.Config.allowItemDataDebug.Tooltip")]
		[DefaultValue(true)]
		public bool allowItemDataDebug;

		public static MagicStorageConfig Instance => ModContent.GetInstance<MagicStorageConfig>();

		[JsonIgnore]
		public static bool GlowNewItems => Instance.glowNewItems;

		[JsonIgnore]
		public static bool UseConfigFilter => Instance.useConfigFilter;

		[JsonIgnore]
		public static bool ShowAllRecipes => Instance.showAllRecipes;

		[JsonIgnore]
		public static bool QuickStackDepositMode => Instance.quickStackDepositMode;

		[JsonIgnore]
		public static bool ClearSearchText => Instance.clearSearchText;

		[JsonIgnore]
		public static bool ExtraFilterIcons => Instance.extraFilterIcons;

		[JsonIgnore]
		public static bool ShowDps => Instance.showDps;

		[JsonIgnore]
		public static bool UseOldCraftMenu => Instance.useOldCraftMenu;

		[JsonIgnore]
		public static bool AllowItemDataDebug => Instance.allowItemDataDebug;

		public override ConfigScope Mode => ConfigScope.ClientSide;
	}

	[Label("$Mods.MagicStorage.Config.ServersideLabel")]
	public class MagicStorageServerConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ServerSide;

		public static MagicStorageServerConfig Instance => ModContent.GetInstance<MagicStorageServerConfig>();

		[Label("$Mods.MagicStorage.Config.AllowAutomatonToMoveIn.Label")]
		[Tooltip("$Mods.MagicStorage.Config.AllowAutomatonToMoveIn.Tooltip")]
		[DefaultValue(true)]
		public bool allowAutomatonToMoveIn;

		[JsonIgnore]
		public static bool AllowAutomatonToMoveIn => Instance.allowAutomatonToMoveIn;
	}
}
