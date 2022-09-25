using System.ComponentModel;
using System.Diagnostics;
using MagicStorage.Common.Players;
using MagicStorage.UI.States;
using Newtonsoft.Json;
using Terraria;
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
		[DefaultValue(true)]
		public bool quickStackDepositMode;

		[Label("$Mods.MagicStorage.Config.clearSearchText.Label")]
		[Tooltip("$Mods.MagicStorage.Config.clearSearchText.Tooltip")]
		[DefaultValue(true)]
		public bool clearSearchText;

		[Label("$Mods.MagicStorage.Config.extraFilterIcons.Label")]
		[Tooltip("$Mods.MagicStorage.Config.extraFilterIcons.Tooltip")]
		[DefaultValue(true)]
		[ReloadRequired]
		public bool extraFilterIcons;

		[Label("$Mods.MagicStorage.Config.useOldCraftMenu.Label")]
		[Tooltip("$Mods.MagicStorage.Config.useOldCraftMenu.Tooltip")]
		[DefaultValue(false)]
		public bool useOldCraftMenu;

		[Label("$Mods.MagicStorage.Config.allowItemDataDebug.Label")]
		[Tooltip("$Mods.MagicStorage.Config.allowItemDataDebug.Tooltip")]
		[DefaultValue(false)]
		public bool itemDataDebug;  //Previously "allowItemDataDebug"

		[Label("$Mods.MagicStorage.Config.searchBarRefreshOnKey.Label")]
		[Tooltip("$Mods.MagicStorage.Config.searchBarRefreshOnKey.Tooltip")]
		[DefaultValue(true)]
		public bool searchBarRefreshOnKey;

		[Label("$Mods.MagicStorage.Config.uiFavorites.Label")]
		[Tooltip("$Mods.MagicStorage.Config.uiFavorites.Tooltip")]
		[DefaultValue(false)]
		public bool uiFavorites;

		[Label("$Mods.MagicStorage.Config.recipeBlacklist.Label")]
		[Tooltip("$Mods.MagicStorage.Config.recipeBlacklist.Tooltip")]
		[DefaultValue(false)]
		public bool recipeBlacklist;

		[Label("$Mods.MagicStorage.Config.buttonLayout.Label")]
		[Tooltip("$Mods.MagicStorage.Config.buttonLayout.Tooltip")]
		[DefaultValue(ButtonConfigurationMode.Legacy)]
		[DrawTicks]
		public ButtonConfigurationMode buttonLayout;

		[Label("$Mods.MagicStorage.Config.clearHistory.Label")]
		[Tooltip("$Mods.MagicStorage.Config.clearHistory.Tooltip")]
		[DefaultValue(false)]
		public bool clearHistory;

		[Label("$Mods.MagicStorage.Config.canMovePanels.Label")]
		[Tooltip("$Mods.MagicStorage.Config.canMovePanels.Tooltip")]
		[DefaultValue(true)]
		public bool canMovePanels;

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
		public static bool UseOldCraftMenu => Instance.useOldCraftMenu;

		[JsonIgnore]
		public static bool ItemDataDebug => Instance.itemDataDebug;

		[JsonIgnore]
		public static bool SearchBarRefreshOnKey => Instance.searchBarRefreshOnKey;

		[JsonIgnore]
		public static bool CraftingFavoritingEnabled => Instance.uiFavorites;

		[JsonIgnore]
		public static bool RecipeBlacklistEnabled => Instance.recipeBlacklist;

		[JsonIgnore]
		public static ButtonConfigurationMode ButtonUIMode => Instance.buttonLayout;

		[JsonIgnore]
		public static bool ClearRecipeHistory => Instance.clearHistory;

		[JsonIgnore]
		public static bool CanMoveUIPanels => Instance.canMovePanels;

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

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) {
			if (Main.player[whoAmI].GetModPlayer<OperatorPlayer>().hasOp)
				return true;

			message = "Only users with the Server Operator status or higher can modify this config";
			return false;
		}
	}

	#if NETPLAY
	/// <summary>
	/// The config for beta builds.  Make sure to wrap uses of this class with <c>#if NETPLAY</c>
	/// </summary>
	[Label("$Mods.MagicStorage.Config.BetaLabel")]
	public class MagicStorageBetaConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;

		public static MagicStorageBetaConfig Instance => ModContent.GetInstance<MagicStorageBetaConfig>();

		[Label("$Mods.MagicStorage.Config.PrintTextToChat.Label")]
		[Tooltip("$Mods.MagicStorage.Config.PrintTextToChat.Tooltip")]
		[DefaultValue(false)]
		public bool printTextToChat;

		[Label("$Mods.MagicStorage.Config.ShowDebugPylonRangeAreas.Label")]
		[Tooltip("$Mods.MagicStorage.Config.ShowDebugPylonRangeAreas.Tooltip")]
		[DefaultValue(false)]
		public bool showPylonAreas;

		[JsonIgnore]
		public static bool PrintTextToChat => Instance.printTextToChat;

		[JsonIgnore]
		public static bool ShowDebugPylonRangeAreas => Instance.showPylonAreas;
	}
	#endif
}
