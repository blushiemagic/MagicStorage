using System.ComponentModel;
using MagicStorage.Common.Players;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.UI.States;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace MagicStorage {
	public class MagicStorageConfig : ModConfig
	{
		[Header($"$Mods.MagicStorage.Config.Headers.Storage")]
		[DefaultValue(true)]
		public bool quickStackDepositMode;

		[Header($"$Mods.MagicStorage.Config.Headers.Crafting")]
		[DefaultValue(true)]
		public bool useConfigFilter;

		[DefaultValue(false)]
		public bool showAllRecipes;

		[DefaultValue(false)]
		public bool useOldCraftMenu;

		[DefaultValue(false)]
		public bool recipeBlacklist;

		[DefaultValue(false)]
		public bool clearHistory;

		[DefaultValue(3)]
		[DrawTicks]
		[Range(-1, 10)]
		public int recursionDepth;

		[Header($"$Mods.MagicStorage.Config.Headers.StorageAndCrafting")]
		[DefaultValue(false)]
		public bool glowNewItems;

		[DefaultValue(true)]
		public bool clearSearchText;

		[DefaultValue(true)]
		public bool searchBarRefreshOnKey;

		[DefaultValue(false)]
		public bool uiFavorites;

		[DefaultValue(true)]
		[ReloadRequired]
		public bool extraFilterIcons;

		[DefaultValue(ButtonConfigurationMode.Legacy)]
		[DrawTicks]
		public ButtonConfigurationMode buttonLayout;

		[Header($"$Mods.MagicStorage.Config.Headers.General")]
		[DefaultValue(false)]
		public bool itemDataDebug;  //Previously "allowItemDataDebug"

		[DefaultValue(true)]
		public bool canMovePanels;

		[DefaultValue(true)]
		public bool automatonRemembers;

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

		[JsonIgnore]
		public static int RecipeRecursionDepth => Instance.recursionDepth;
		
		[JsonIgnore]
		public static bool IsRecursionEnabled => RecipeRecursionDepth != 0;

		[JsonIgnore]
		public static bool IsRecursionInfinite => RecipeRecursionDepth == -1;

		[JsonIgnore]
		public static bool DisplayLastSeenAutomatonTip => Instance.automatonRemembers;

		public override ConfigScope Mode => ConfigScope.ClientSide;
	}

	public class MagicStorageServerConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ServerSide;

		public static MagicStorageServerConfig Instance => ModContent.GetInstance<MagicStorageServerConfig>();

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
	[LabelKey("$Mods.MagicStorage.Config.BetaLabel")]
	public class MagicStorageBetaConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;

		public static MagicStorageBetaConfig Instance => ModContent.GetInstance<MagicStorageBetaConfig>();

		[LabelKey("$Mods.MagicStorage.Config.PrintTextToChat.Label")]
		[TooltipKey("$Mods.MagicStorage.Config.PrintTextToChat.Tooltip")]
		[DefaultValue(false)]
		public bool printTextToChat;

		[LabelKey("$Mods.MagicStorage.Config.ShowDebugPylonRangeAreas.Label")]
		[TooltipKey("$Mods.MagicStorage.Config.ShowDebugPylonRangeAreas.Tooltip")]
		[DefaultValue(false)]
		public bool showPylonAreas;

		[JsonIgnore]
		public static bool PrintTextToChat => Instance.printTextToChat;

		[JsonIgnore]
		public static bool ShowDebugPylonRangeAreas => Instance.showPylonAreas;
	}
	#endif
}
