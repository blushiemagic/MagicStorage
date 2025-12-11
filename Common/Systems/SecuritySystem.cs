using MagicStorage.Common.Players;
using MagicStorage.Common.Systems.Auditing;
using MagicStorage.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

#nullable enable
namespace MagicStorage.Common.Systems {
	public class SecuritySystem : ModSystem {
		private class Network {
			public Guid creator = default;
			internal string creatorNameFallback = null!;
			public string name = null!;
			public string? password = null;
			public bool restricted;
			internal int uniqueID;

			public string GetCreator() {
				if (TryFindPlayer(creator, out Player? player)) {
					creatorNameFallback = player.name;
					return player.name;
				} else {
					return creatorNameFallback;
				}
			}

			public void Save(TagCompound tag) {
				tag["id"] = uniqueID;
				tag["creator"] = creator.ToByteArray();
				tag["creatorName"] = creatorNameFallback;
				tag["name"] = name;
				tag["password"] = password is null ? null : StringScrambling.Scramble(password);
				tag["restricted"] = restricted;
			}

			public void Load(TagCompound tag) {
				uniqueID = tag.GetInt("id");
				creator = new Guid(tag.GetByteArray("creator"));
				creatorNameFallback = tag.GetString("creatorName");
				name = tag.GetString("name");
				password = tag.ContainsKey("password") ? StringScrambling.Unscramble(tag.GetByteArray("password")) : null;
				restricted = tag.GetBool("restricted");

				if (password == string.Empty)
					password = null;
			}
		}

		public readonly struct NetworkView {
			public readonly string creator;
			public readonly Guid creatorID;
			public readonly string name;
			public readonly bool restricted;
			public readonly int id;
			private readonly bool _validInstance;

			public bool Valid => _validInstance;

			internal NetworkView(string creator, Guid creatorID, string name, bool restricted, int id) {
				ArgumentNullException.ThrowIfNull(creator);
				ArgumentNullException.ThrowIfNull(name);

				this.creator = creator;
				this.creatorID = creatorID;
				this.name = name;
				this.restricted = restricted;
				this.id = id;
				_validInstance = true;
			}
		}

		private static readonly List<Network> _networks = [];
		private static readonly List<NetworkView> _clientViews = [];
		internal static bool clientListDirty;

		public static int NetworkCount => Main.netMode == NetmodeID.MultiplayerClient ? _clientViews.Count : _networks.Count;

		private static LocalizedText? _clientResult;
		internal static event Action<LocalizedText>? OnClientResultUpdated;
		internal static event Action<NetworkActionResult, NetworkReportCategory>? OnInformResultToClient;

		internal static string searchName = string.Empty;
		internal static int searchPrivateMode = SEARCHMODE_ALL;
		internal static int searchSortMode = SORTMODE_NAME;
		internal static bool sortInReverse = false;

		internal const int SEARCHMODE_ALL = 0;
		internal const int SEARCHMODE_RESTRICTED = 1;
		internal const int SEARCHMODE_PUBLIC = 2;

		internal const int SORTMODE_NAME = 0;
		internal const int SORTMODE_CREATOR = 1;
		internal const int SORTMODE_ID = 2;

		public override void SaveWorldData(TagCompound tag) {
			List<TagCompound> networkTags = new();
			foreach (Network network in _networks) {
				TagCompound networkTag = new();
				network.Save(networkTag);
				networkTags.Add(networkTag);
			}
			tag["networks"] = networkTags;
		}

		public override void LoadWorldData(TagCompound tag) {
			_networks.Clear();
			foreach (TagCompound networkTag in tag.GetList<TagCompound>("networks")) {
				Network network = new();
				network.Load(networkTag);
				_networks.Add(network);
			}
		}

		internal static void OnLocalClientEnterWorld() {
			_clientViews.Clear();
			_clientResult = null;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetHelper.RequestSecurityNetworkList();
				NetHelper.RequestAccessibleNetworksByDefault();
			} else {
				// Singleplayer can access the data directly, so just do that
				// NOTE: This logic runs before automatic Administrator status is handled, but that's only relevant in multiplayer which gets handled anyway
				NetworkActionResult result = ServerDefaultAccessibleNetworks(Main.myPlayer, out var networks);

				if (result.IsSuccess()) {
					SecurityPlayer securityPlayer = Main.LocalPlayer.GetModPlayer<SecurityPlayer>();

					foreach (var network in networks) {
						securityPlayer.JoinNetwork(network.id);
						securityPlayer.RememberPassword(network.id, network.password);
					}
				}
			}
		}

		private static int ReserveUniqueID() {
			int id;
			
			do {
				id = Main.rand.Next();
			} while (TryGetNetwork(id, out _));

			return id;
		}

		private static bool TryFindPlayer(Guid guid, [NotNullWhen(true)] out Player? player) {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player plr = Main.player[i];
				if (plr.active && SecurityPlayer.GetID(i) == guid) {
					player = plr;
					return true;
				}
			}

			player = null;
			return false;
		}

		private static NetworkActionResult ValidatePlayer(int player, out Player plr) {
			if (player < 0 || player >= Main.maxPlayers) {
				plr = null!;
				return NetworkActionResult.InvalidPlayer;
			}

			plr = Main.player[player];
			if (plr is null || !plr.active) {
				plr = null!;
				return NetworkActionResult.InvalidPlayer;
			}

			return NetworkActionResult.Success;
		}

		public static bool CanPlayerAccessImmediately(Player player, int networkID)
			=> !Main.gameMenu && (networkID < 0 || player.GetModPlayer<OperatorPlayer>().hasOp || (player.GetModPlayer<SecurityPlayer>().HasJoinedNetwork(networkID) && NetworkExists(networkID)));

		public static void PrintStorageInaccessible() => Main.NewText(Language.GetText("Mods.MagicStorage.Security.EntityNotAccessible"));

		private static bool TryGetNetwork(int id, out int networkIndex) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Client only has access to the local views
				for (int i = 0; i < _clientViews.Count; i++) {
					if (_clientViews[i].id == id) {
						networkIndex = i;
						return true;
					}
				}
			}

			for (int i = 0; i < _networks.Count; i++) {
				Network net = _networks[i];
				if (net.uniqueID == id) {
					networkIndex = i;
					return true;
				}
			}

			networkIndex = -1;
			return false;
		}

		public static NetworkView GetNetwork(int id) {
			if (id < 0)
				return default;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				foreach (NetworkView view in _clientViews) {
					if (view.id == id)
						return view;
				}
			} else {
				foreach (Network network in _networks) {
					if (network.uniqueID == id)
						return new NetworkView(network.GetCreator(), network.creator, network.name, network.restricted, network.uniqueID);
				}
			}

			return default;
		}

		public static IEnumerable<NetworkView> GetNetworks() {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				foreach (NetworkView view in _clientViews)
					yield return view;

				yield break;
			}

			foreach (Network network in _networks)
				yield return new NetworkView(network.GetCreator(), network.creator, network.name, network.restricted, network.uniqueID);
		}

		public static IEnumerable<NetworkView> EnumerateNetworksWithSettings() {
			IEnumerable<NetworkView> networks = EnumerateFilteredNetworks();
			
			return searchSortMode switch {
				SORTMODE_NAME => sortInReverse ? networks.OrderByDescending(n => n.name) : networks.OrderBy(n => n.name),
				SORTMODE_CREATOR => sortInReverse ? networks.OrderByDescending(n => n.creator) : networks.OrderBy(n => n.creator),
				SORTMODE_ID => sortInReverse ? networks.OrderByDescending(n => n.id) : networks.OrderBy(n => n.id),
				_ => networks
			};
		}

		private static IEnumerable<NetworkView> EnumerateFilteredNetworks() {
			foreach (NetworkView view in GetNetworks()) {
				if (NetworkPassesFilters(view))
					yield return view;
			}
		}

		private static bool NetworkPassesFilters(NetworkView view) {
			if (searchPrivateMode == SEARCHMODE_RESTRICTED && !view.restricted)
				return false;

			if (searchPrivateMode == SEARCHMODE_PUBLIC && view.restricted)
				return false;

			if (!string.IsNullOrWhiteSpace(searchName) && searchName[0] == '@')
				return view.creator.Contains(searchName[1..], StringComparison.OrdinalIgnoreCase);

			return view.name.Contains(searchName, StringComparison.OrdinalIgnoreCase);
		}

		public static NetworkActionResult TryGetPassword(int id, out string? password) {
			if (!TryGetNetwork(id, out int networkIndex)) {
				password = null;
				return NetworkActionResult.NetworkNotFound;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Client only remembers the password if they have accessed the network before
				SecurityPlayer securityPlayer = Main.LocalPlayer.GetModPlayer<SecurityPlayer>();

				if (securityPlayer.HasJoinedNetwork(id)) {
					var network = _clientViews[networkIndex];

					if (!network.restricted) {
						// Public networks have no password
						password = null;
						return NetworkActionResult.Success;
					} else if (securityPlayer.TryGetPassword(id, out string rememberedPassword)) {
						// The client has previously accessed the network and provided the correct password
						// NOTE: If the password for the network is changed, all players have their authorization status revoked
						password = rememberedPassword;
						return NetworkActionResult.Success;
					}
				}

				// Ensure that whatever password was previously remembered is forgotten
				securityPlayer.ForgetPassword(id);

				password = null;
				return NetworkActionResult.UnauthorizedAccess;
			}

			password = _networks[networkIndex].password;
			return NetworkActionResult.Success;
		}

		public static bool NetworkExists(int id) {
			if (id < 0)
				return false;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				foreach (NetworkView view in _clientViews) {
					if (view.id == id)
						return true;
				}

				return false;
			}

			foreach (Network network in _networks) {
				if (network.uniqueID == id)
					return true;
			}

			return false;
		}

		internal static Guid GetCreator(int networkID) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				foreach (NetworkView view in _clientViews) {
					if (view.id == networkID)
						return view.creatorID;
				}
			} else {
				foreach (Network network in _networks) {
					if (network.uniqueID == networkID)
						return network.creator;
				}
			}

			return Guid.Empty;
		}

		internal static void SyncClientNetworkViews(BinaryWriter writer) {
			writer.Write(_networks.Count);
			foreach (var view in GetNetworks()) {
				writer.Write(view.creator);
				writer.Write(view.creatorID);
				writer.Write(view.name);
				writer.Write(view.restricted);
				writer.Write(view.id);
			}
		}

		internal static void ReceiveClientNetworkViews(BinaryReader reader) {
			_clientViews.Clear();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				string creator = reader.ReadString();
				Guid creatorID = reader.ReadGuid();
				string name = reader.ReadString();
				bool restricted = reader.ReadBoolean();
				int id = reader.ReadInt32();
				_clientViews.Add(new NetworkView(creator, creatorID, name, restricted, id));
			}

			clientListDirty = true;
		}

		internal static void ReportNetworkResult(NetworkActionResult result) => ReportNetworkResult(result, default);

		internal static void ReportNetworkResult(NetworkActionResult result, NetworkReportCategory category) {
			_clientResult = GetResponse(result, category);
			OnClientResultUpdated?.Invoke(_clientResult);
			OnInformResultToClient?.Invoke(result, category);
		}

		public static LocalizedText GetEmptyResponse() => Language.GetText("Mods.MagicStorage.Security.UI.NoResponse");

		public static LocalizedText GetResponse(NetworkActionResult result) => GetResponse(result, default);

		public static LocalizedText GetResponse(NetworkActionResult result, NetworkReportCategory category) {
			string key = result switch {
				NetworkActionResult.Success
				or NetworkActionResult.OperatorForcedSuccess => $"Mods.MagicStorage.Security.{category}.Success",
				NetworkActionResult.UnauthorizedNetmodeContext => "Mods.MagicStorage.Security.UnauthorizedNetmodeContext."
					+ Main.netMode switch {
						NetmodeID.SinglePlayer => "Singleplayer",
						NetmodeID.MultiplayerClient => "MultiplayerClient",
						NetmodeID.Server => "MultiplayerServer",
						_ => "Unknown"
					},
				NetworkActionResult.NeedsServerApproval => "Mods.MagicStorage.Security.UI.ServerWait",
				NetworkActionResult.NetworkNotFound
				or NetworkActionResult.EmptyPassword
				or NetworkActionResult.EmptyName
				or NetworkActionResult.InvalidPassword
				or NetworkActionResult.InvalidPlayer
				or NetworkActionResult.UnauthorizedAccess
				or NetworkActionResult.UnauthorizedModification => $"Mods.MagicStorage.Security.{result}",
				_ => throw new ArgumentException($"Invalid NetworkActionResult: {result}", nameof(result))
			};

			return Language.GetText(key);
		}

		internal static void ResetClientResult() {
			_clientResult = GetEmptyResponse();
			OnClientResultUpdated?.Invoke(_clientResult);
		}

		public static NetworkActionResult CreateNetwork(string name, string password, bool restricted) {
			if (string.IsNullOrWhiteSpace(name))
				return NetworkActionResult.EmptyName;

			if (restricted && string.IsNullOrWhiteSpace(password))
				return NetworkActionResult.EmptyPassword;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Request server to create network
				Main.LocalPlayer.GetModPlayer<SecurityPlayer>().RememberPassword(SecurityPlayer.TEMPORARY_PASSWORD, password);
				NetHelper.RequestSecurityNetworkCreation(name, password, restricted);
				return NetworkActionResult.NeedsServerApproval;
			} else if (Main.netMode == NetmodeID.Server) {
				// Servers don't have valid player instances
				return NetworkActionResult.UnauthorizedNetmodeContext;
			}

			int id;
			string? pwd;
			_networks.Add(new Network() {
				creator = SecurityPlayer.GetLocalID(),
				creatorNameFallback = Main.LocalPlayer.name,
				name = name,
				password = pwd = restricted ? password : null,
				restricted = restricted,
				uniqueID = id = ReserveUniqueID()
			});

			var mp = Main.LocalPlayer.GetModPlayer<SecurityPlayer>();
			mp.JoinNetwork(id);
			mp.RememberPassword(id, pwd);

			return NetworkActionResult.Success;
		}

		internal static NetworkActionResult ServerCreateNetwork(int player, string name, string password, bool restricted, out int id) {
			if (Main.netMode != NetmodeID.Server) {
				id = -1;
				return NetworkActionResult.UnauthorizedNetmodeContext;
			}

			var result = ValidatePlayer(player, out Player plr);
			if (!result.IsSuccess()) {
				id = -1;
				return result;
			}

			if (string.IsNullOrWhiteSpace(name)) {
				id = -1;
				return NetworkActionResult.EmptyName;
			}

			if (restricted && string.IsNullOrWhiteSpace(password)) {
				id = -1;
				return NetworkActionResult.EmptyPassword;
			}

			_networks.Add(new Network() {
				creator = SecurityPlayer.GetID(player),
				creatorNameFallback = plr.name,
				name = name,
				password = restricted ? password : string.Empty,
				restricted = restricted,
				uniqueID = id = ReserveUniqueID()
			});

			return NetworkActionResult.Success;
		}

		public static NetworkActionResult RemoveNetwork(int id, string password) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Request server to remove network
				NetHelper.RequestSecurityNetworkRemoval(id, password);
				return NetworkActionResult.NeedsServerApproval;
			} else if (Main.netMode == NetmodeID.Server) {
				// Servers don't have valid player instances
				return NetworkActionResult.UnauthorizedNetmodeContext;
			}

			return CheckNetworkRemoval(Main.LocalPlayer, id, password);
		}

		internal static NetworkActionResult ServerRemoveNetwork(int player, int id, string password) {
			if (Main.netMode != NetmodeID.Server)
				return NetworkActionResult.UnauthorizedNetmodeContext;

			var result = ValidatePlayer(player, out Player plr);
			if (!result.IsSuccess())
				return result;

			result = CheckNetworkRemoval(plr, id, password);
			if (result.IsSuccess())
				AuditSystem.ReportSecurityNetworkDeletion(player, id);

			return result;
		}

		private static NetworkActionResult CheckNetworkRemoval(Player player, int id, string password) {
			if (!TryGetNetwork(id, out int networkIndex))
				return NetworkActionResult.NetworkNotFound;

			Network network = _networks[networkIndex];

			// If the network is private, the player must provide the password to remove it
			// However, if the player is a Server Operator, this restriction is lifted
			NetworkActionResult success = NetworkActionResult.Success;
			if (player.GetModPlayer<OperatorPlayer>().hasOp)
				success = NetworkActionResult.OperatorForcedSuccess;
			else if (network.restricted && network.password != password)
				return NetworkActionResult.UnauthorizedModification;

			_networks.RemoveAt(networkIndex);

			// Inform any components on this network that it no longer exists
			foreach (TEStorageComponent component in TileEntity.ByPosition.Values.OfType<TEStorageComponent>()) {
				if (component.assignedNetwork == network.uniqueID) {
					component.assignedNetwork = -1;
					NetHelper.SyncStorageComponentNetwork(component);
				}
			}

			return success;
		}

		public static NetworkActionResult JoinNetwork(int id, string password) {
			SecurityPlayer securityPlayer = Main.LocalPlayer.GetModPlayer<SecurityPlayer>();
			if (securityPlayer.HasJoinedNetwork(id))
				return NetworkActionResult.Success;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Request server to join network
				securityPlayer.RememberPassword(id, password);
				NetHelper.RequestSecurityNetworkJoin(id, password);
				return NetworkActionResult.NeedsServerApproval;
			} else if (Main.netMode == NetmodeID.Server) {
				// Servers don't have valid player instances
				return NetworkActionResult.UnauthorizedNetmodeContext;
			}

			// Context here is only singleplayer
			return CheckNetworkJoin(Main.LocalPlayer, id, password);
		}

		internal static NetworkActionResult ServerJoinNetwork(int player, int id, string password) {
			if (Main.netMode != NetmodeID.Server)
				return NetworkActionResult.UnauthorizedNetmodeContext;

			var result = ValidatePlayer(player, out Player plr);
			if (!result.IsSuccess())
				return result;

			result = CheckNetworkJoin(plr, id, password);
			if (result.IsSuccess())
				AuditSystem.ReportSecurityNetworkJoin(player, id);

			return result;
		}

		private static NetworkActionResult CheckNetworkJoin(Player player, int id, string password) {
			if (!TryGetNetwork(id, out int networkIndex))
				return NetworkActionResult.NetworkNotFound;

			// If the player is a Server Operator, they can access any network
			// However, joining a network involves saving the password, so that bypass is not allowed here
			NetworkActionResult success = NetworkActionResult.Success;
			/*
			if (player.GetModPlayer<OperatorPlayer>().hasOp) {
				success = NetworkActionResult.OperatorForcedSuccess;
				goto Success;
			}
			*/

			Network network = _networks[networkIndex];

			// The owner of the network can always access it
			if (network.creator == player.GetModPlayer<SecurityPlayer>().UniqueID)
				goto Success;

			if (network.restricted && network.password != password)
				return NetworkActionResult.UnauthorizedAccess;

		Success:
		//	player.GetModPlayer<SecurityPlayer>().JoinNetwork(id);
			return success;
		}

		public static NetworkActionResult AccessNetwork(int id) {
			SecurityPlayer securityPlayer = Main.LocalPlayer.GetModPlayer<SecurityPlayer>();
			bool op = Main.LocalPlayer.GetModPlayer<OperatorPlayer>().hasOp;
			if (op || securityPlayer.HasJoinedNetwork(id))
				return op ? NetworkActionResult.OperatorForcedSuccess : NetworkActionResult.Success;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Request access to the network
				NetHelper.RequestSecurityNetworkAccess(id);
				return NetworkActionResult.NeedsServerApproval;
			} else if (Main.netMode == NetmodeID.Server) {
				// Servers don't have valid player instances
				return NetworkActionResult.UnauthorizedNetmodeContext;
			}

			return CheckNetworkAccess(Main.LocalPlayer, id);
		}

		internal static NetworkActionResult ServerAccessNetwork(int player, int id) {
			if (Main.netMode != NetmodeID.Server)
				return NetworkActionResult.UnauthorizedNetmodeContext;

			var result = ValidatePlayer(player, out Player plr);
			if (!result.IsSuccess())
				return result;

			return CheckNetworkAccess(plr, id);
		}

		private static NetworkActionResult CheckNetworkAccess(Player player, int id) {
			if (!TryGetNetwork(id, out int networkIndex))
				return NetworkActionResult.NetworkNotFound;

			// If the player is a Server Operator, they can access any network
			if (player.GetModPlayer<OperatorPlayer>().hasOp)
				return NetworkActionResult.OperatorForcedSuccess;

			Network network = _networks[networkIndex];

			// The owner of the network can always access it
			if (network.creator == player.GetModPlayer<SecurityPlayer>().UniqueID)
				return NetworkActionResult.Success;

			// The player might not have access to the network.  Should the network have a password, prevent access only if the player hasn't joined it.
			if (network.restricted && !player.GetModPlayer<SecurityPlayer>().HasJoinedNetwork(id))
				return NetworkActionResult.UnauthorizedAccess;

			return NetworkActionResult.Success;
		}

		public static NetworkActionResult ModifyNetwork(int id, string? updatedName, string? updatedPassword, bool? updatedRestricted) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Request server to modify network
				NetHelper.RequestSecurityNetworkChange(id, updatedName, updatedPassword, updatedRestricted);
				return NetworkActionResult.NeedsServerApproval;
			} else if (Main.netMode == NetmodeID.Server) {
				// Servers don't have valid player instances
				return NetworkActionResult.UnauthorizedNetmodeContext;
			}

			return CheckNetworkModification(Main.LocalPlayer, SecurityPlayer.GetLocalID(), id, updatedName, updatedPassword, updatedRestricted, out _, out _);
		}

		internal static NetworkActionResult ServerModifyNetwork(int player, int id, string? updatedName, string? updatedPassword, bool? updatedRestricted, out bool passwordChanged, out bool privacyChanged) {
			passwordChanged = false;
			privacyChanged = false;

			if (Main.netMode != NetmodeID.Server)
				return NetworkActionResult.UnauthorizedNetmodeContext;

			var result = ValidatePlayer(player, out Player plr);
			if (!result.IsSuccess())
				return result;

			if (!TryGetPassword(id, out string? oldPassword).IsSuccess())
				return NetworkActionResult.NetworkNotFound;

			bool oldRestricted = GetNetwork(id).restricted;

			result = CheckNetworkModification(plr, SecurityPlayer.GetID(player), id, updatedName, updatedPassword, updatedRestricted, out passwordChanged, out privacyChanged);
			if (result.IsSuccess())
				AuditSystem.ReportSecurityNetworkModification(plr, id, oldPassword, oldRestricted, updatedPassword ?? oldPassword, updatedRestricted ?? oldRestricted);

			return result;
		}

		private static NetworkActionResult CheckNetworkModification(Player requestingPlayer, Guid requestingPlayerID, int id, string? updatedName, string? updatedPassword, bool? updatedRestricted, out bool passwordChanged, out bool privacyChanged) {
			passwordChanged = false;
			privacyChanged = false;

			if (!TryGetNetwork(id, out int networkIndex))
				return NetworkActionResult.NetworkNotFound;

			Network network = _networks[networkIndex];

			// Only the creator of the network can modify it
			// However, if the player is a Server Operator, this restriction is lifted
			NetworkActionResult success = NetworkActionResult.Success;
			if (requestingPlayer.GetModPlayer<OperatorPlayer>().hasOp)
				success = NetworkActionResult.OperatorForcedSuccess;
			else if (network.creator != requestingPlayerID)
				return NetworkActionResult.UnauthorizedModification;

			// Ensure that the network's state is valid after the modifications
			if (updatedPassword is not null && network.restricted && string.IsNullOrWhiteSpace(updatedPassword))
				return NetworkActionResult.EmptyPassword;

			if (updatedRestricted is bool r && r && string.IsNullOrWhiteSpace(updatedPassword))
				return NetworkActionResult.EmptyPassword;

			// Perform the modifications
			if (updatedName is not null)
				network.name = updatedName;

			string? oldPassword = network.password;
			bool oldRestricted = network.restricted;

			if (updatedPassword is not null)
				network.password = network.restricted ? updatedPassword : null;

			if (updatedRestricted is bool restricted) {
				if (!restricted)
					network.password = null!;  // Public networks have no password

				network.restricted = restricted;
			}

			passwordChanged = network.password != oldPassword;
			privacyChanged = network.restricted != oldRestricted;

			return success;
		}

		public static NetworkActionResult AssignNetwork(TEStorageHeart heart, int id) {
			// Player needs access to both the current network (if the heart is set to one) and the network to move to
			SecurityPlayer securityPlayer = Main.LocalPlayer.GetModPlayer<SecurityPlayer>();
			if (!CanPlayerAccessImmediately(Main.LocalPlayer, id) || !CanPlayerAccessImmediately(Main.LocalPlayer, heart.assignedNetwork))
				return NetworkActionResult.UnauthorizedAccess;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Request the assignment to the server
				NetHelper.RequestStorageHeartNetworkAssignment(Main.LocalPlayer, heart, id);
				return NetworkActionResult.NeedsServerApproval;
			} else if (Main.netMode == NetmodeID.Server) {
				// Servers don't have valid player instances
				return NetworkActionResult.UnauthorizedNetmodeContext;
			}

			return ChangeNetworkAssignments(Main.LocalPlayer, heart, id);
		}

		internal static NetworkActionResult ServerAssignNetwork(int player, Point16 heartPosition, int id) {
			if (Main.netMode != NetmodeID.Server)
				return NetworkActionResult.UnauthorizedNetmodeContext;

			var result = ValidatePlayer(player, out Player plr);
			if (!result.IsSuccess())
				return result;

			if (!TileEntity.ByPosition.TryGetValue(heartPosition, out TileEntity? te) || te is not TEStorageHeart heart)
				return NetworkActionResult.EntityNotFound;

			AuditSystem.ReportSecurityNetworkAssignment(plr, heart, id);

			return ChangeNetworkAssignments(plr, heart, id);
		}

		private static NetworkActionResult ChangeNetworkAssignments(Player player, TEStorageHeart heart, int id) {
			heart.assignedNetwork = id;
			NetHelper.SyncStorageComponentNetwork(heart);

			foreach (TEStorageComponent component in heart.ComponentManager.GetAllComponentEntities()) {
				component.assignedNetwork = id;
				NetHelper.SyncStorageComponentNetwork(component);
			}

			return player.GetModPlayer<OperatorPlayer>().hasOp ? NetworkActionResult.OperatorForcedSuccess : NetworkActionResult.Success;
		}

		internal static void HandleNetworkAccessibilityOnCreation(NetworkActionResult result, int creator, Player player, int networkID) {
			if (result is NetworkActionResult.NeedsServerApproval) {
				// Waiting for the server to respond
				return;
			}

			var securityPlayer = player.GetModPlayer<SecurityPlayer>();

			if (!result.IsSuccess()) {
				// Remove access as a precaution
				securityPlayer.RemoveNetworkAccess(networkID);
				return;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				if (creator == Main.myPlayer) {
					securityPlayer.JoinNetwork(networkID);

					// Transfer the temporary password to the actual password
					if (!securityPlayer.TryGetPassword(SecurityPlayer.TEMPORARY_PASSWORD, out string password))
						throw new InvalidOperationException("Temporary client password not found, player likely attempted to create networks too quickly");

					securityPlayer.RememberPassword(networkID, password);
					securityPlayer.ForgetPassword(SecurityPlayer.TEMPORARY_PASSWORD);
				} else
					securityPlayer.RemoveNetworkAccess(networkID);  // Ensure that other players don't somehow have access to a stale network
			}
		}

		internal static void HandleNetworkAccessibilityOnJoin(NetworkActionResult result, Player player, int networkID, string? password = null) {
			if (result is NetworkActionResult.NeedsServerApproval) {
				// Waiting for the server to respond
				return;
			}

			var securityPlayer = player.GetModPlayer<SecurityPlayer>();

			if (!result.IsSuccess()) {
				// If the player is not authorized to access the network or something else went wrong, remove access
				securityPlayer.RemoveNetworkAccess(networkID);
				return;
			}

			if (!TryGetNetwork(networkID, out int index)) {
				// Network doesn't exist, so remove access as a precaution
				securityPlayer.RemoveNetworkAccess(networkID);
				return;
			}

			bool restricted;
			if (Main.netMode == NetmodeID.MultiplayerClient)
				restricted = _clientViews[index].restricted;
			else
				restricted = _networks[index].restricted;

			if (restricted) {
				if (password is null)
					throw new ArgumentNullException(nameof(password), "Password cannot be null for restricted networks");  // Invalid callee parameters

				securityPlayer.RememberPassword(networkID, password);
			} else {
				if (password is not null)
					throw new ArgumentException("Password must be null for public networks", nameof(password));  // Invalid callee parameters

				securityPlayer.RememberPassword(networkID, null);
			}

			securityPlayer.JoinNetwork(networkID);
		}

		internal static void HandleNetworkAccessibilityOnRemoval(NetworkActionResult result, Player player, int networkID) {
			if (result is NetworkActionResult.NeedsServerApproval) {
				// Waiting for the server to respond
				return;
			}

			if (!result.IsSuccess())
				return;

			player.GetModPlayer<SecurityPlayer>().RemoveNetworkAccess(networkID);
		}

		internal static void HandleNetworkAccessibilityOnAccess(NetworkActionResult result, Player player, int networkID) {
			if (result is NetworkActionResult.NeedsServerApproval) {
				// Waiting for the server to respond
				return;
			}

			if (result.IsSuccess())
				return;

			player.GetModPlayer<SecurityPlayer>().RemoveNetworkAccess(networkID);
		}

		internal static void HandleNetworkAccessibilityOnModification(NetworkActionResult result, Player player, NetworkView view, bool outdatedAuthorization, bool isOwner) {
			if (result is NetworkActionResult.NeedsServerApproval) {
				// Waiting for the server to respond
				return;
			}

			if (result.IsSuccess())
				return;

			var securityPlayer = player.GetModPlayer<SecurityPlayer>();

			if (!view.restricted) {
				// Public networks are always accessible
				securityPlayer.JoinNetwork(view.id);

			} else if (!isOwner && !player.GetModPlayer<OperatorPlayer>().hasOp) {
				// Non-Operators should have their access revoked
				securityPlayer.RemoveNetworkAccess(view.id);
			}
		}

		internal readonly struct SimpleNetworkView(int id, string? password) {
			public readonly int id = id;
			public readonly string? password = password;
		}

		internal static NetworkActionResult ServerDefaultAccessibleNetworks(int player, out SimpleNetworkView[] networks) {
			// Too lazy to rewrite this to have a SinglePlayer context like the other methods...
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				networks = [];
				return NetworkActionResult.UnauthorizedNetmodeContext;
			}

			var result = ValidatePlayer(player, out Player plr);
			if (!result.IsSuccess()) {
				networks = [];
				return result;
			}

			Guid id = plr.GetModPlayer<SecurityPlayer>().UniqueID;
			List<SimpleNetworkView> accessible = [];

			if (plr.GetModPlayer<OperatorPlayer>().IsAdministrator) {
				// Administrators have access to all networks
				accessible.AddRange(_networks.Select(static n => new SimpleNetworkView(n.uniqueID, n.password)));
			} else {
				foreach (Network network in _networks) {
					if (!network.restricted || network.creator == id)
						accessible.Add(new(network.uniqueID, network.password));
				}
			}

			networks = [ .. accessible ];
			return NetworkActionResult.Success;
		}

		// NOTE: let this default to 0 since that would refer to the local player in singleplayer
		private static int accessingPlayer;

		public ref struct AccessContext : IDisposable {
			private int oldPlayer;

			public readonly int Player { get; }

			internal AccessContext(int playerID) {
				Player = playerID;
			}

			public AccessContext Use() {
				oldPlayer = accessingPlayer;
				accessingPlayer = Player;
				return this;
			}

			public void Dispose() {
				if (oldPlayer < -1)
					return;

				accessingPlayer = oldPlayer;

				oldPlayer = -2;
			}
		}

		public static AccessContext CreateAccessContext() => new AccessContext(Main.myPlayer).Use();

		public static AccessContext CreateAccessContext(int playerID) => new AccessContext(playerID).Use();

		public static bool AccessibleFromContext(int networkID) => accessingPlayer < 0 || CanPlayerAccessImmediately(Main.player[accessingPlayer], networkID);

		public static bool TryGetCurrentAccessContext(out AccessContext context) {
			if (accessingPlayer < 0) {
				context = default;
				return false;
			}

			context = new(accessingPlayer);
			return true;
		}

		public static bool HasActiveAccessContext() => accessingPlayer >= 0;
	}

	public enum NetworkActionResult {
		Success,
		NetworkNotFound,
		NeedsServerApproval,
		EmptyPassword,
		EmptyName,
		InvalidPassword,
		InvalidPlayer,
		UnauthorizedAccess,
		UnauthorizedModification,
		UnauthorizedNetmodeContext,
		EntityNotFound,
		OperatorForcedSuccess
	}

	public enum NetworkReportCategory {
		Unknown,
		Creation,
		Join,
		Removal,
		Modification,
		Access,
		NetworkChange
	}
}
