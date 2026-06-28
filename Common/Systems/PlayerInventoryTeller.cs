using MagicStorage.Common.Systems.Debugging;
using MagicStorage.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.Systems {
	public static class PlayerInventoryTeller {
		public static readonly int PiggyBank = 0;
		public static readonly int Safe = 1;
		public static readonly int DefendersForge = 2;
		public static readonly int VoidVault = 3;

		private static readonly List<Func<Player, Item[]>> _loadInventory = [
			static player => player.bank.item,
			static player => player.bank2.item,
			static player => player.bank3.item,
			static player => player.bank4.item,
		];

		public static Item[] LoadBank(Player player, int bank) => _loadInventory[bank](player);

		internal static BitArray PrepareHandleArray(Item[] inventory) {
			BitArray hasItem = new(inventory.Length);

			for (int i = 0; i < inventory.Length; i++) {
				Item item = inventory[i];
				hasItem[i] = item is { IsAir: false, favorited: false };
			}

			return hasItem;
		}

		internal static void HandleInventory(Item[] inventory, BitArray hasItem, TEStorageHeart heart, ref bool depositedAny) {
			// Attempt to deposit each item
			// Any leftovers will be left in the inventory

			for (int i = 0; i < inventory.Length; i++) {
				if (!hasItem[i])
					continue;
				
				Item item = inventory[i];
				int oldStack = item.stack;

				heart.DepositItem(item);

				if (oldStack != item.stack)
					depositedAny = true;

				if (item.IsAir)
					hasItem[i] = false;
			}
		}

		public static void SendDepositToStorageRequest(int bank, TEStorageHeart heart) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			Item[] inventory = LoadBank(Main.LocalPlayer, bank);
			BitArray hasItemToSend = PrepareHandleArray(inventory);

			if (hasItemToSend.GetCardinality() == 0) {
				// Nothing from the inventory could be deposited
				if (DebugControls.Get(DebugControls.Names.DepositItemsFromPlayerBank))
					DebugMessage.Report(true, "No items to deposit from bank {0}", bank);

				return;
			}

			var packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.ClientRequestPlayerBankDeposit);
			packet.Write(heart.Position);
			packet.Write((byte)bank);
			packet.Write((ushort)inventory.Length);

			// Send the BitArray as bytes
			byte[] bitArrayBytes = new byte[(inventory.Length + 7) >>> 3];
			hasItemToSend.CopyTo(bitArrayBytes, 0);
			packet.Write(bitArrayBytes);

			// Send the items
			for (int i = 0; i < inventory.Length; i++) {
				if (hasItemToSend[i]) {
					Item item = inventory[i];
					ItemIO.Send(item, packet, true, false);

					// Temporarily delete the item so that it can't be sent again
					item.TurnToAir();
				}
			}

			packet.Send();

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Get(DebugControls.Names.DepositItemsFromPlayerBank)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent packet {0} to the server", MessageType.ClientRequestPlayerBankDeposit);
				DebugMessage.Indent();
				DebugMessage.Report(true, "Bank: {0}", bank);
				DebugMessage.Report(true, "Heart position: {0}", heart.Position);
				DebugMessage.Report(true, "Sent {0} items", hasItemToSend.GetCardinality());
				DebugMessage.EndReportGroup();
			}
		}

		public static void ServerReceiveDepositToStorageRequest(BinaryReader reader, int sender) {
			Point16 position = reader.ReadPoint16();
			int bank = reader.ReadByte();
			int count = reader.ReadUInt16();

			byte[] bitArrayBytes = reader.ReadBytes((count + 7) >>> 3);
			BitArray hasItem = new BitArray(bitArrayBytes) { Length = count };

			Item[] inventory = new Item[count];

			for (int i = 0; i < count; i++) {
				if (hasItem[i])
					inventory[i] = ItemIO.Receive(reader, true, false);
			}

			if (Main.netMode != NetmodeID.Server)
				return;

			bool depositedAny = false;

			if (!NetHelper.TryGetEntityFromLocation(MessageType.ClientRequestPlayerBankDeposit, position, out TEStorageHeart heart)) {
				NetHelper.PrintBadArgsFallback();
				goto sendResponse;
			}

			HandleInventory(inventory, hasItem, heart, ref depositedAny);

sendResponse:

			var packet = MagicStorageMod.Instance.GetPacket();
			packet.Write((byte)MessageType.PlayerBankDepositResult);
			packet.Write((byte)bank);
			packet.Write(depositedAny);
			packet.Write((ushort)inventory.Length);

			hasItem.CopyTo(bitArrayBytes, 0);
			packet.Write(bitArrayBytes);

			// If an item is still present, it couldn't be fully deposited into storage; send it back
			for (int i = 0; i < inventory.Length; i++) {
				if (hasItem[i])
					ItemIO.Send(inventory[i], packet, true, false);
			}

			packet.Send(sender);

			if (NetHelper.CanDebugOutgoingPackets() && DebugControls.Get(DebugControls.Names.DepositItemsFromPlayerBank)) {
				DebugMessage.BeginReportGroup();
				DebugMessage.Report(true, "Sent packet {0} to client {1}", MessageType.PlayerBankDepositResult, sender);
				DebugMessage.Indent();
				DebugMessage.Report(true, "Bank: {0}", bank);
				DebugMessage.Report(true, "Deposited any: {0}", depositedAny);
				DebugMessage.Report(true, "Sent {0} items back to the client", hasItem.GetCardinality());
				DebugMessage.EndReportGroup();
			}
		}

		public static void ClientReceiveDepositToStorageResponse(BinaryReader reader) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			int bank = reader.ReadByte();
			bool depositedAny = reader.ReadBoolean();
			int count = reader.ReadUInt16();

			byte[] bitArrayBytes = reader.ReadBytes((count + 7) >>> 3);
			BitArray hasItem = new BitArray(bitArrayBytes) { Length = count };

			Item[] inventory = new Item[count];

			for (int i = 0; i < count; i++) {
				if (hasItem[i])
					inventory[i] = ItemIO.Receive(reader, true, false);
			}

			// Put the leftover items back in the inventory

			Item[] targetBank = LoadBank(Main.LocalPlayer, bank);

			int numItems = Math.Min(inventory.Length, targetBank.Length);

			for (int i = 0; i < numItems; i++) {
				if (hasItem[i]) {
					Item fromNet = inventory[i];
					if (fromNet is { IsAir: false })
						targetBank[i] = fromNet;
				}
			}

			if (depositedAny)
				SoundEngine.PlaySound(SoundID.Grab);
		}
	}
}
