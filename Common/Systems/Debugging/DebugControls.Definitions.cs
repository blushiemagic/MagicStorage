namespace MagicStorage.Common.Systems.Debugging {
	partial class DebugControls {
		private static Node _controlRoot;

		private static void Initialize() {
			_controlRoot = new Node()
				.Set("audit", new Node()
					.Set("commands", false)
					.Set("component", new Node()
						.Set("crafting", new Node()
							.Set("requests", false)
						)
						.Set("heart", new Node()
							.Set("controls", new Node()
								.Set("sellduplicates", false)
							)
							.Set("deposit", false)
							.Set("security", false)
							.Set("withdraw", false)
						)
						.Set("linking", false)
						.Set("unit", new Node()
							.Set("core", false)
							.Set("state", false)
						)
					)
					.Set("file", false)
				)
				.Set("entity", new Node()
					.Set("npc", new Node()
						.Set("golem", new Node()
							.Set("tips", false)
						)
					)
					.Set("player", new Node()
						.Set("quickstack", false)
					)
					.Set("tile", new Node()
						.Set("aether", new Node()
							.Set("requests", false)
						)
						.Set("centers", new Node()
							.Set("link", false)
							.Set("savedata", false)
						)
						.Set("crafting", new Node()
							.Set("requests", false)
							.Set("stations", false)
						)
						.Set("heart", new Node()
							.Set("history", false)
							.Set("stages", new Node()
								.Set("reset", false)
							)
							.Set("storage", new Node()
								.Set("delete", false)
								.Set("details", new Node()
									.Set("delete", false)
								)
							)
							.Set("usage", false)
						)
						.Set("unit", new Node()
							.Set("transfer", false)
						)
					)
				)
				.Set("net", new Node()
					.Set("badargs", false)
					.Set("entity", new Node()
						.Set("tile", new Node()
							.Set("heart", new Node()
								.Set("clientops", false)
							)
							.Set("unit", new Node()
								.Set("sync", false)
							)
							.Set("updatequeue", false)
							.Set("updates", false)
						)
					)
					.Set("incoming", false)
					.Set("outgoing", false)
					.Set("world", new Node()
						.Set("tile", new Node()
							.Set("section", false)
						)
					)
				)
				.Set("operator", new Node()
					.Set("commands", new Node()
						.Set("msreqop", false)
					)
					.Set("status", false)
				)
				.Set("security", new Node()
					.Set("access", false)
					.Set("assign", false)
					.Set("create", false)
					.Set("delete", false)
					.Set("list", false)
					.Set("modify", false)
				)
				.Set("tile", new Node()
					.Set("centers", new Node()
						.Set("recalculate", false)
						.Set("scan", false)
					)
					.Set("component", new Node()
						.Set("destroy", false)
						.Set("place", false)
					)
					.Set("unit", new Node()
						.Set("state", false)
						.Set("style", false)
					)
				)
				.Set("ui", new Node()
					.Set("refresh", false)
					.Set("storage", new Node()
						.Set("controls", new Node()
							.Set("compactcoins", false)
							.Set("depositfrombank", false)
							.Set("details", new Node()
								.Set("sellduplicates", false)
							)
							.Set("sellduplicates", false)
						)
					)
				)
			;

			// Set up the "any control" trackers

			foreach (var (parent, child) in _controlRoot.EnumerateHierarchy(null))
				child.InitializeTrackers(parent);
		}

		private static void Deinitialize() {
			_controlRoot = null;
		}

		public static class Names {
			#region audits
			public const string Audits = "audit";
			public const string AuditFile = Audits + ".file";
			#endregion  // audits

			#region entity
			public const string EntityLogic = "entity";

			#region entity.player
			public const string PlayerLogic = EntityLogic + ".player";
			public const string QuickStacking = PlayerLogic + ".quickstack";
			#endregion  // player

			#region entity.npc
			public const string NPCLogic = EntityLogic + ".npc";

			#region entity.npc.golem
			public const string AutomatonLogic = NPCLogic + ".golem";
			public const string AutomatonHelpTipUpdate = AutomatonLogic + ".tips";
			#endregion  // entity.npc.golem

			#endregion  // entity.npc

			#region entity.tile
			public const string TileEntityLogic = EntityLogic + ".tile";

			#region entity.tile.aether
			public const string AetherInterfaceLogic = TileEntityLogic + ".aether";
			public const string ShimmeringRequests = AetherInterfaceLogic + ".requests";
			#endregion  // entity.tile.aether

			#region entity.tile.centers
			public const string StorageCenterEntityLogic = TileEntityLogic + ".centers";
			public const string StorageCenterManagerData = StorageCenterEntityLogic + ".savedata";
			public const string StorageCenterComponentLinking = StorageCenterEntityLogic + ".link";
			#endregion

			#region entity.tile.crafting
			public const string CraftingInterfaceLogic = TileEntityLogic + ".crafting";
			public const string CraftingRequests = CraftingInterfaceLogic + ".requests";
			public const string CraftingStationSlots = CraftingInterfaceLogic + ".stations";
			#endregion  // entity.tile.crafting

			#region entity.tile.heart
			public const string StorageHeartLogic = TileEntityLogic + ".heart";
			public const string StorageDepositHistory = StorageHeartLogic + ".history";
			public const string StorageHeartUsage = StorageHeartLogic + ".usage";

			#region entity.tile.heart.stages
			public const string StorageHeartUpdatesLogic = StorageHeartLogic + ".stages";
			public const string StorageHeartResetCompactStage = StorageHeartUpdatesLogic + ".reset";
			#endregion  // entity.tile.heart.stages

			#region entity.tile.heart.storage
			public const string StorageHeartItemsLogic = StorageHeartLogic + ".storage";
			public const string StorageHeartItemDeletion = StorageHeartItemsLogic + ".delete";

			#region entity.tile.heart.storage.details
			public const string StorageHeartItemsLogicDetails = StorageHeartItemsLogic + ".details";
			public const string StorageHeartItemDeletionVerbose = StorageHeartItemsLogicDetails + ".delete";
			#endregion  // entity.tile.heart.storage.details

			#endregion  // entity.tile.heart.storage

			#endregion  // entity.tile.heart

			#region entity.tile.unit
			public const string StorageUnitEntityLogic = TileEntityLogic + ".unit";
			public const string StorageUnitItemTransfer = StorageUnitEntityLogic + ".transfer";
			#endregion  // entity.tile.unit

			#endregion  // entity.tile

			#endregion  // entity

			#region net
			public const string Netcode = "net";
			public const string IncomingNetcodePackets = Netcode + ".incoming";
			public const string OutgoingNetcodePackets = Netcode + ".outgoing";
			public const string InvalidNetcodeValues = Netcode + ".badargs";

			#region net.entity
			public const string EntityNetcode = Netcode + ".entity";

			#region net.entity.tile
			public const string TileEntityNetcode = EntityNetcode + ".tile";
			public const string TileEntityUpdatesNetcode = TileEntityNetcode + ".updates";
			public const string TileEntityUpdateQueueNetcode = TileEntityNetcode + ".updatequeue";

			#region net.entity.tile.unit
			public const string StorageUnitNetcode = TileEntityNetcode + ".unit";
			public const string StorageSyncingNetcode = StorageUnitNetcode + ".sync";
			#endregion  // net.entity.tile.unit

			#region net.entity.tile.heart
			public const string StorageHeartNetcode = TileEntityNetcode + ".heart";
			public const string StorageHeartClientOperations = StorageHeartNetcode + ".clientops";
			#endregion  // net.entity.tile.heart

			#endregion  // net.entity.tile

			#endregion  // net.entity

			#region net.world
			public const string WorldNetcode = Netcode + ".world";

			#region net.world.tile
			public const string TileNetcode = WorldNetcode + ".tile";
			public const string TileSectionNetcode = TileNetcode + ".section";
			#endregion  // net.world.tile

			#endregion  // net.world

			#endregion  // net

			#region operator
			public const string OperatorLogic = "operator";
			public const string OperatorStatus = OperatorLogic + ".status";

			#region operator.commands
			public const string OperatorCommandsLogic = OperatorLogic + ".commands";
			public const string CommandGrantAdministrator = OperatorCommandsLogic + ".msreqop";
			#endregion  // operator.commands

			#endregion  // operator

			#region security
			public const string SecuritySystemLogic = "security";
			public const string SeuritySystemNetworkCreation = SecuritySystemLogic + ".create";
			public const string SecuritySystemNetworkAccess = SecuritySystemLogic + ".access";
			public const string SecuritySystemNetworkModification = SecuritySystemLogic + ".modify";
			public const string SecuritySystemNetworkDeletion = SecuritySystemLogic + ".delete";
			public const string SecuritySystemNetworkList = SecuritySystemLogic + ".list";
			public const string SecuritySystemNetworkAssignment = SecuritySystemLogic + ".assign";
			#endregion  // security

			#region tile
			public const string TileLogic = "tile";

			#region tile.unit
			public const string StorageUnitLogic = TileLogic + ".unit";
			public const string StorageUnitActiveState = StorageUnitLogic + ".state";
			public const string StorageUnitFrame = StorageUnitLogic + ".style";
			#endregion  // tile.unit

			#region tile.centers
			public const string StorageCentersLogic = TileLogic + ".centers";
			public const string StorageNetworkRecalculate = StorageCentersLogic + ".recalculate";
			public const string StorageNetworkScanning = StorageCentersLogic + ".scan";
			#endregion  // tile.centers

			#region tile.component
			public const string StorageComponentLogic = TileLogic + ".component";
			public const string StorageComponentPlacement = StorageComponentLogic + ".place";
			public const string StorageComponentDestruction = StorageComponentLogic + ".destroy";
			#endregion  // tile.component

			#endregion  // tile

			#region ui
			public const string UI = "ui";
			public const string RefreshingUI = UI + ".refresh";

			#region ui.storage
			public const string StorageUI = UI + ".storage";

			#region ui.storage.controls
			public const string StorageUIControlsTab = StorageUI + ".controls";
			public const string SellDuplicatesMenu = StorageUIControlsTab + ".sellduplicates";
			public const string DepositItemsFromPlayerBank = StorageUIControlsTab + ".depositfrombank";
			public const string CompactCoins = StorageUIControlsTab + ".compactcoins";

			#region ui.storage.controls.details
			public const string StorageUIControlsDetails = StorageUIControlsTab + ".details";
			public const string SellDuplicatesMenuVerbose = StorageUIControlsDetails + ".sellduplicates";
			#endregion  // ui.storage.controls.details

			#endregion  // ui.storage.controls

			#endregion  // ui.storage

			#endregion  // ui
		}
	}
}
