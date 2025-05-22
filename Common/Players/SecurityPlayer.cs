using MagicStorage.Common.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.Players {
	public class SecurityPlayer : ModPlayer {
		/// <summary>
		/// The unique identifier for this player for identifying authors of networks
		/// </summary>
		public Guid UniqueID { get; private set; }

		public static Guid GetID(int plr) => plr < 0 || plr >= Main.maxPlayers ? Guid.Empty : Main.player[plr].GetModPlayer<SecurityPlayer>().UniqueID;

		public static Guid GetLocalID() => Main.LocalPlayer.GetModPlayer<SecurityPlayer>().UniqueID;

		private readonly HashSet<int> _accessibleNetworks = new();
		private readonly Dictionary<int, string> _knownPasswords = new();

		internal const int TEMPORARY_PASSWORD = -1;

		public bool HasJoinedNetwork(int networkID) => _accessibleNetworks.Contains(networkID);

		public void JoinNetwork(int networkID) => _accessibleNetworks.Add(networkID);

		public void RemoveNetworkAccess(int networkID) {
			_accessibleNetworks.Remove(networkID);
			_knownPasswords.Remove(networkID);
		}

		public bool KnowsPassword(int networkID) => _knownPasswords.ContainsKey(networkID);

		public bool TryGetPassword(int networkID, out string password) => _knownPasswords.TryGetValue(networkID, out password);

		internal void RememberPassword(int networkID, string password) => _knownPasswords[networkID] = password;

		internal void ForgetPassword(int networkID) => _knownPasswords.Remove(networkID);

		internal bool RequestingSecurityUI;

		public override void ResetEffects() {
			RequestingSecurityUI = false;
		}

		public override void OnEnterWorld() {
			_accessibleNetworks.Clear();
			_knownPasswords.Clear();
			SecuritySystem.OnLocalClientEnterWorld();
		}

		public override void SaveData(TagCompound tag) {
			if (UniqueID == Guid.Empty)
				UniqueID = Guid.NewGuid();

			tag["id"] = UniqueID.ToByteArray();
		}

		public override void LoadData(TagCompound tag) {
			if (tag.ContainsKey("id"))
				UniqueID = new Guid(tag.GetByteArray("id"));
			else
				UniqueID = Guid.NewGuid();  // Failsafe in case the id was not saved
		}

		// Netcode methods

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)MessageType.SecurityPlayerSync);
			packet.Write((byte)Player.whoAmI);
			packet.Write(UniqueID.ToByteArray());
			packet.Send(toWho, fromWho);
		}

		internal void ReceiveSync(BinaryReader reader) {
			UniqueID = new Guid(reader.ReadBytes(16));
		}

		public override void CopyClientState(ModPlayer targetCopy) {
			SecurityPlayer mp = (SecurityPlayer)targetCopy;
			mp.UniqueID = UniqueID;
		}

		public override void SendClientChanges(ModPlayer clientPlayer) {
			SecurityPlayer mp = (SecurityPlayer)clientPlayer;
			if (mp.UniqueID != UniqueID)
				SyncPlayer(-1, Main.myPlayer, false);
		}
	}
}
