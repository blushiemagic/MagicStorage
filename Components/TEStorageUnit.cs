using Ionic.Zlib;
using MagicStorage.Common.IO;
using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using MagicStorage.CrossMod.Storage;
using MagicStorage.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public class TEStorageUnit : TEAbstractStorageUnit
	{
		internal enum NetOperations : byte
		{
			FullySync,
			Deposit,
			Withdraw,
			WithdrawStack,
			PackItems,
			Flatten,
			RemoveCore,
			InsertCore
		}

		private struct NetOperation
		{
			public NetOperation(NetOperations _netOPeration, Item _item = null, bool _keepOneInFavorite = false)
			{
				netOperation = _netOPeration;
				item = _item;
				keepOneInFavorite = _keepOneInFavorite;
			}

			public NetOperations netOperation { get; }
			public Item item { get; }
			public bool keepOneInFavorite { get; }
		}

		private readonly Queue<NetOperation> netOpQueue = new();
		private HashSet<ItemData> hasItem = new();
		private HashSet<int> hasItemNoPrefix = new();

		//metadata
		private HashSet<ItemData> hasSpaceInStack = new();
		internal List<Item> items = new();  //Exposed to make "selling" items easier
		internal bool receiving;

		public int Capacity
		{
			get
			{
				// I'm crying from just looking at this terrible code -- absoluteAquarian
				/*
				int style = Main.tile[Position.X, Position.Y].TileFrameY / 36;
				if (style == 8)
					return 4;
				if (style > 1)
					style--;
				int capacity = style + 1;
				if (capacity > 4)
					capacity++;
				if (capacity > 6)
					capacity++;
				if (capacity > 8)
					capacity += 7;
				return 40 * capacity;
				*/
				// FIX: v0.7.0.3 - Return zero instead of throwing an error
				return GetCurrentTier()?.Capacity ?? 0;
			}
		}

		public override bool IsFull => items.Count >= Capacity;

		public bool IsEmpty => items.Count == 0;

		public int NumItems => items.Count;

		public StorageUnitTier GetCurrentTier() => StorageUnitTierLoader.FindFromTile(Position.X, Position.Y);

		public void GetFramingState(out StorageUnitFullness fullness, out bool active) {
			fullness = items.Count == 0
				? StorageUnitFullness.Empty
				: items.Count < Capacity
					? StorageUnitFullness.PartiallyFull
					: StorageUnitFullness.Full;

			active = !Inactive;
		}

		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<StorageUnit>() && tile.TileFrameX % 36 == 0 && tile.TileFrameY % 36 == 0;

		public override bool HasSpaceInStackFor(Item check)
		{
			return hasSpaceInStack.Contains(check);
		}

		public bool HasSpaceFor(Item check) => !IsFull || HasSpaceInStackFor(check);

		public override bool HasItem(Item check, bool ignorePrefix = false)
		{
			if (ignorePrefix)
				return hasItemNoPrefix.Contains(check.type);
			return hasItem.Contains(check);
		}

		public override IEnumerable<Item> GetItems() => items;

		public override void DepositItem(Item toDeposit)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return;

			Item original = toDeposit.Clone();
			
			DepositToItemCollection(items, toDeposit, Capacity, out bool hasChange);

			if (hasChange)
			{
				if (Main.netMode == NetmodeID.Server)
					netOpQueue.Enqueue(new NetOperation(NetOperations.Deposit, original));
				PostChangeContents();
			}
		}

		internal static bool DepositToItemCollection(List<Item> items, Item toDeposit, int capacity, out bool hasChange) {
			bool finished = false;
			hasChange = false;

			foreach (Item item in items)
			{
				if (StorageAggregator.CanCombineItems(toDeposit, item) && item.stack < item.maxStack)
				{
					int total = item.stack + toDeposit.stack;
					int newStack = total;
					if (newStack > item.maxStack)
						newStack = item.maxStack;

					Utility.CallOnStackHooks(item, toDeposit, newStack - item.stack);

					item.stack = newStack;

					if (toDeposit.favorited)
						item.favorited = true;
					if (toDeposit.newAndShiny)
						item.newAndShiny = MagicStorageConfig.GlowNewItems;

					hasChange = true;
					toDeposit.stack = total - newStack;
					if (toDeposit.stack <= 0)
					{
						finished = true;
						break;
					}
				}
			}

			if (!finished && items.Count < capacity)
			{
				Item item = toDeposit.Clone();
				items.Add(item);
				toDeposit.SetDefaults(0, true);
				hasChange = true;
				finished = true;
			}

			return finished;
		}

		public override Item TryWithdraw(Item lookFor, bool locked = false, bool keepOneIfFavorite = false)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();

			Item original = lookFor.Clone();

			if (!WithdrawFromItemCollection(items, lookFor, out Item result, keepOneIfFavorite))
				return result;

			if (Main.netMode == NetmodeID.Server)
				netOpQueue.Enqueue(new NetOperation(NetOperations.Withdraw, original, keepOneIfFavorite));
			PostChangeContents();

			return result;
		}

		internal static bool WithdrawFromItemCollection(List<Item> items, Item lookFor, out Item result, bool keepOneIfFavorite = false, bool checkPrefix = true) {
			result = null;
			ConditionalWeakTable<Item, byte[]> cachedItemIO = [];
			for (int k = items.Count - 1; k >= 0; k--)
			{
				Item item = items[k];
				if ((checkPrefix && ItemData.Matches(lookFor, item)) || (!checkPrefix && lookFor.type == item.type))
				{
					int maxToTake = item.stack;
					if (item.stack > 0 && item.favorited && keepOneIfFavorite)
						maxToTake -= 1;
					int withdraw = Math.Min(lookFor.stack, maxToTake);

					if (result is not null) {
						//Item data must be the same
						if (!StorageAggregator.CanCombineItems(result, item, checkPrefix, true, cachedItemIO))
							continue;

						Utility.CallOnStackHooks(result, item, withdraw);

						result.stack += withdraw;
					} else {
						result = item.Clone();
						result.stack = withdraw;
					}

					item.stack -= withdraw;
					if (item.stack <= 0) {
						items.RemoveAt(k);
						item.TurnToAir();
					}

					lookFor.stack -= withdraw;
					
					if (lookFor.stack <= 0)
						goto ReturnFromMethod;
				}
			}

			if (result is null || result.IsAir)
			{
				result = new Item();
				return false;
			}

			ReturnFromMethod:
			return true;
		}

		private int _lastKnownFramingTier = -1;

		public bool UpdateTileFrame()
		{
			// FIX: v0.7.0.4 - Return early instead of throwing an exception
			if (GetCurrentTier() is not StorageUnitTier tier)
				return false;

			Tile tile = Main.tile[Position.X, Position.Y];

			tier.GetState(tile.TileFrameX, tile.TileFrameY, out var previousFullness, out var previousActive);

			StorageUnitFullness currentFullness;
			if (IsEmpty)
				currentFullness = StorageUnitFullness.Empty;
			else if (IsFull)
				currentFullness = StorageUnitFullness.Full;
			else
				currentFullness = StorageUnitFullness.PartiallyFull;

			bool currentActive = !Inactive;

			tier.Frame(currentFullness, currentActive, out int targetFrameX, out int targetFrameY);

			if (previousFullness != currentFullness || previousActive != currentActive || _lastKnownFramingTier != tier.Type) {
				_lastKnownFramingTier = tier.Type;

				int x = Position.X, y = Position.Y;
				tile.TileFrameX = (short)targetFrameX;
				tile.TileFrameY = (short)targetFrameY;

				tile = Main.tile[x + 1, y];
				tile.TileFrameX = (short)(targetFrameX + 18);
				tile.TileFrameY = (short)targetFrameY;

				tile = Main.tile[x, y + 1];
				tile.TileFrameX = (short)targetFrameX;
				tile.TileFrameY = (short)(targetFrameY + 18);

				tile = Main.tile[x + 1, y + 1];
				tile.TileFrameX = (short)(targetFrameX + 18);
				tile.TileFrameY = (short)(targetFrameY + 18);

				return true;
			}

			_lastKnownFramingTier = tier.Type;

			return false;
		}

		public void UpdateTileFrameWithNetSend()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				if (UpdateTileFrame())
					NetMessage.SendTileSquare(-1, Position.X, Position.Y, 2, 2);
			} else
				NetHelper.RequestStorageUnitStyle(Position);
		}

		internal static void SwapItems(TEStorageUnit unit1, TEStorageUnit unit2)
		{
			(unit1.items, unit2.items) = (unit2.items, unit1.items);
			(unit1.hasSpaceInStack, unit2.hasSpaceInStack) = (unit2.hasSpaceInStack, unit1.hasSpaceInStack);
			(unit1.hasItem, unit2.hasItem) = (unit2.hasItem, unit1.hasItem);
			(unit1.hasItemNoPrefix, unit2.hasItemNoPrefix) = (unit2.hasItemNoPrefix, unit1.hasItemNoPrefix);
			if (Main.netMode == NetmodeID.Server)
			{
				unit1.netOpQueue.Clear();
				unit2.netOpQueue.Clear();
				unit1.netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
				unit2.netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
			}

			unit1.PostChangeContents();
			unit2.PostChangeContents();
		}

		internal Item WithdrawStack()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return new Item();

			Item item = items[items.Count - 1];
			items.RemoveAt(items.Count - 1);

			if (Main.netMode == NetmodeID.Server)
				netOpQueue.Enqueue(new NetOperation(NetOperations.WithdrawStack));
			PostChangeContents();

			return item;
		}

		internal Item RemoveItemsAndSpawnCore() {
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving) {
				NetHelper.ClientSendCoreRemoval(Position);
				return null;
			}

			Item spawnedItem = null;
			// FIX: v0.7.0.4 - Do nothing instead of throwing an exception
			if (Main.netMode != NetmodeID.MultiplayerClient && GetCurrentTier() is StorageUnitTier tier) {
				Item core = new Item(tier.CoreItemType);

				((BaseStorageCore)core.ModItem).SetDataFrom(this);

				Vector2 world = Position.ToWorldCoordinates(16, 16);
				Item.NewItem(new EntitySource_TileEntity(this), world, core);

				spawnedItem = core;
			}

			var types = items.Select(static i => i.type).Distinct().ToList();

			items.Clear();
			StorageUnit.SetTypeAndStyle(Position.X, Position.Y, StorageUnitTier.Empty, StorageUnitFullness.Empty, !Inactive);

			if (Main.netMode == NetmodeID.Server)
				netOpQueue.Enqueue(new NetOperation(NetOperations.RemoveCore));
			PostChangeContents();

			if (GetHeart() is TEStorageHeart storageHeart && StoragePlayer.IsClientViewingHeart(storageHeart)) {
				MagicUI.RequestFullRefresh();
				MagicUI.SetNextCollectionsToRefresh(types);
			}

			return spawnedItem;
		}

		internal void InsertCore(BaseStorageCore core) {
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving) {
				NetHelper.ClientSendCoreInsertion(Position, core);
				return;
			}

			items.Clear();

			var coreItems = core.RetrieveItems();
			items.AddRange(coreItems);

			if (Main.netMode == NetmodeID.Server)
				netOpQueue.Enqueue(new NetOperation(NetOperations.InsertCore, core.Item));
			PostChangeContents();

			if (GetHeart() is TEStorageHeart storageHeart && StoragePlayer.IsClientViewingHeart(storageHeart)) {
				MagicUI.RequestFullRefresh();
				MagicUI.SetNextCollectionsToRefresh(coreItems.Select(static i => i.type).Distinct().ToList());
			}
		}

		internal void PackItems() {
			if (Main.netMode == NetmodeID.MultiplayerClient && !receiving)
				return;

			if (items.Count < 2)
				return;

			items = Compact(items, out bool didPack);

			if (didPack)
			{
				if (Main.netMode == NetmodeID.Server)
					netOpQueue.Enqueue(new NetOperation(NetOperations.PackItems));
				PostChangeContents();
			}
		}

		internal bool FlattenFrom(TEStorageUnit source, out List<Item> transferredItems) {
			transferredItems = null;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetHelper.ClientRequestItemTransfer(this, source);
				return false;
			} else if (Main.netMode == NetmodeID.Server)
				return NetHelper.AttemptItemTransferAndSendResult(this, source, out transferredItems, false);

			AttemptItemTransfer(this, source, out transferredItems);

			if (transferredItems.Count == 0)
				return false;

			PostChangeContents();
			source.PostChangeContents();

			return true;
		}

		internal static List<Item> Compact(IEnumerable<Item> items, out bool didPack) {
			List<Item> packed = new();
			didPack = false;

			foreach (Item item in items) {
				foreach (Item pack in packed) {
					if (pack.IsAir || pack.stack >= pack.maxStack)
						continue;

					if (StorageAggregator.CanCombineItems(item, pack)) {
						if (item.stack + pack.stack <= pack.maxStack) {
							Utility.CallOnStackHooks(pack, item, item.stack);

							pack.stack += item.stack;
							item.stack = 0;
						} else {
							Utility.CallOnStackHooks(pack, item, item.maxStack - pack.stack);

							item.stack -= pack.maxStack - pack.stack;
							pack.stack = pack.maxStack;
						}

						didPack = true;
						break;
					}
				}

				if (item.stack > 0)
					packed.Add(item);
			}

			return packed;
		}

		internal static void AttemptItemTransfer(TEStorageUnit destination, TEStorageUnit source, out List<Item> transferredItems) {
			transferredItems = new();

			if (source.IsEmpty) {
				//Nothing to do
				NetHelper.Report(false, $"Source unit (X: {source.Position.X}, Y: {source.Position.Y}) was empty.  Aborting transfer");
				return;
			}

			if (destination.Inactive) {
				//Nothing to do
				NetHelper.Report(false, $"Destination unit (X: {destination.Position.X}, Y: {destination.Position.Y}) was inactive.  Aborting transfer");
				return;
			}

			//Attempt to pack items first
			for (int d = 0; d < destination.NumItems; d++) {
				Item dest = destination.items[d];

				if (dest.IsAir || dest.stack >= dest.maxStack)
					continue;

				for (int s = source.NumItems - 1; s >= 0; s--) {
					Item src = source.items[s];

					if (src.IsAir)
						continue;

					if (StorageAggregator.CanCombineItems(dest, src)) {
						Item transferred = src.Clone();

						if (dest.stack + src.stack <= dest.maxStack) {
							Utility.CallOnStackHooks(dest, src, src.stack);

							dest.stack += src.stack;
							src.stack = 0;
							source.items.RemoveAt(s);
						} else {
							int diff = dest.maxStack - dest.stack;

							Utility.CallOnStackHooks(dest, src, diff);

							transferred.stack = diff;

							src.stack -= diff;
							dest.stack = dest.maxStack;
						}

						transferredItems.Add(transferred);
					}
				}
			}

			if (transferredItems.Count > 0)
				NetHelper.Report(false, $"Packed {transferredItems.Count} items from the source unit (X: {source.Position.X}, Y: {source.Position.Y}) to the destination unit (X: {destination.Position.X}, Y: {destination.Position.Y})");

			//Then simply transfer items until the destination is full or the source is empty
			int nonPackedTransfer = 0;
			while (!destination.IsFull && !source.IsEmpty) {
				Item withdrawn = source.items[^1];
				source.items.RemoveAt(source.items.Count - 1);

				destination.items.Add(withdrawn);

				transferredItems.Add(withdrawn);

				nonPackedTransfer++;
			}

			if (nonPackedTransfer > 0)
				NetHelper.Report(false, $"Transferred {nonPackedTransfer} items from the source unit (X: {source.Position.X}, Y: {source.Position.Y}) to the destination unit (X: {destination.Position.X}, Y: {destination.Position.Y})");
		}

		public override void SaveData(TagCompound tag)
		{
			base.SaveData(tag);
			List<TagCompound> tagItems = items.Select(Utility.SaveItem).Where(t => t.Count > 0).ToList();
			tag.Set("Items", tagItems);
		}

		public override void LoadData(TagCompound tag)
		{
			base.LoadData(tag);
			ClearItemsData();
			foreach (Item item in tag.GetList<TagCompound>("Items").Select(Utility.SafelyLoadItem).Where(static i => !i.IsAir))
			{
				items.Add(item);
				ItemData data = item;
				if (item.stack < item.maxStack)
					hasSpaceInStack.Add(data);
				hasItem.Add(data);
				hasItemNoPrefix.Add(data.Type);
			}
		}

		public void FullySync()
		{
			netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
		}

		const int MAX_REQUESTS = 32;

		public override void NetSend(BinaryWriter trueWriter)
		{
			using MemoryStream buffer = new(65536);
			using BinaryWriter writer = new(buffer);

			base.NetSend(writer);

			// too many updates at this point just fully sync
			if (netOpQueue.Count > MAX_REQUESTS)
			{
				// Preserve operations that aren't just a sync of inventory contents
				// This is especially important for InsertCore, since that would be executed on a Storage Unit with zero Capacity
				var specialOps = netOpQueue.Where(static op => op.netOperation is NetOperations.InsertCore or NetOperations.RemoveCore)
					.Take(MAX_REQUESTS)
					.ToList();

				netOpQueue.Clear();

				if (specialOps.Count > 0)
				{
					foreach (NetOperation op in specialOps)
						netOpQueue.Enqueue(op);
				}
				else
					netOpQueue.Enqueue(new NetOperation(NetOperations.FullySync));
			}

			ValueWriter bitWriter = new ValueWriter(writer);

			int capacityBits = NetCompression.GetBitSize(Capacity);
			bitWriter.Write((ushort)items.Count, capacityBits);
			bitWriter.Write((ushort)netOpQueue.Count, NetCompression.GetBitSize(MAX_REQUESTS));
			while (netOpQueue.Count > 0)
			{
				NetOperation netOp = netOpQueue.Dequeue();
				bitWriter.Write((byte)netOp.netOperation, numBits: 3);
				switch (netOp.netOperation)
				{
					// FIX: v0.7.0.12 - Item data should use the original tag information, not the netcode data
					case NetOperations.FullySync:
						SaveCompression.SaveItems(items, bitWriter, true, true, listCountBitSizeOverride: capacityBits);
						break;
					case NetOperations.Withdraw:
						bitWriter.Write(netOp.keepOneInFavorite);
						SaveCompression.SaveItem(netOp.item, bitWriter, true, true);
						break;
					case NetOperations.WithdrawStack:
						break;
					case NetOperations.Deposit:
						SaveCompression.SaveItem(netOp.item, bitWriter, true, true);
						break;
					case NetOperations.PackItems:
						break;
					case NetOperations.RemoveCore:
						break;
					case NetOperations.InsertCore:
						SaveCompression.SaveItem(netOp.item, bitWriter, false, false);
						break;
					default:
						break;
				}
			}

			/* Forces data to be flushed into the buffer */
			bitWriter.Flush();
			writer.Flush();

			byte[] data = NetCompression.Compress(buffer.GetBuffer(), CompressionLevel.BestCompression);

			/* Sends the buffer through the network */
			trueWriter.Write((ushort)data.Length);
			trueWriter.Write(data);

			NetHelper.Report(true, "Sent tile entity data for TEStorageUnit");
			NetHelper.Report(false, "Bytes sent: " + data.Length);
		}

		public override void NetReceive(BinaryReader trueReader)
		{
			/* Reads the buffer off the network */
			ushort bufferLen = trueReader.ReadUInt16();
			using MemoryStream decompressedStream = new MemoryStream(NetCompression.Decompress(trueReader.ReadBytes(bufferLen), CompressionLevel.BestCompression));
			using BinaryReader reader = new(decompressedStream);

			base.NetReceive(reader);

			ValueReader bitReader = new ValueReader(reader);

			int capacityBits = NetCompression.GetBitSize(Capacity);
			int serverItemsCount = bitReader.ReadUInt16(capacityBits);
			int opCount = bitReader.ReadUInt16(NetCompression.GetBitSize(MAX_REQUESTS));
			if (opCount > 0)
			{
				if (ByPosition.TryGetValue(Position, out TileEntity te) && te is TEStorageUnit otherUnit)
				{
					items = otherUnit.items;
					hasSpaceInStack = otherUnit.hasSpaceInStack;
					hasItem = otherUnit.hasItem;
					hasItemNoPrefix = otherUnit.hasItemNoPrefix;
				}

				int oldCount = items.Count;

				receiving = true;
				bool repairMetaData = true;
				for (int i = 0; i < opCount; i++)
				{
					byte netOp = bitReader.ReadByte(numBits: 3);
					if (Enum.IsDefined(typeof(NetOperations), netOp))
					{
						switch ((NetOperations)netOp)
						{
							// FIX: v0.7.0.12 - Item data should use the original tag information, not the netcode data
							case NetOperations.FullySync:
								repairMetaData = false;
								ClearItemsData();
								List<Item> netItems = SaveCompression.LoadItems(bitReader, true, true, listCountBitSizeOverride: capacityBits);
								for (int j = 0; j < netItems.Count; j++)
								{
									Item item = netItems[j];
									items.Add(item);
									ItemData data = item;
									if (item.stack < item.maxStack)
										hasSpaceInStack.Add(data);
									hasItem.Add(data);
									hasItemNoPrefix.Add(data.Type);
								}
								break;
							case NetOperations.Withdraw:
								bool keepOneIfFavorite = bitReader.ReadBoolean();
								TryWithdraw(SaveCompression.LoadItem(bitReader, true, true), keepOneIfFavorite);
								break;
							case NetOperations.WithdrawStack:
								WithdrawStack();
								break;
							case NetOperations.Deposit:
								DepositItem(SaveCompression.LoadItem(bitReader, true, true));
								break;
							case NetOperations.PackItems:
								PackItems();
								break;
							case NetOperations.RemoveCore:
								RemoveItemsAndSpawnCore();
								break;
							case NetOperations.InsertCore:
								InsertCore((BaseStorageCore)SaveCompression.LoadItem(bitReader, false, false).ModItem);
								break;
							default:
								break;
						}
					}
					else
					{
						if (Main.netMode != NetmodeID.Server)
							Main.NewText($"NetRecive Bad OP: {netOp}", Microsoft.Xna.Framework.Color.Red);
						else
							Utility.WriteLineColoredSafely($"NetRecive Bad OP: {netOp}", ConsoleColor.Red, ConsoleColor.Black);
					}
				}

				if (repairMetaData)
					RepairMetadata();

				if (items.Count != oldCount)
					UpdateTileFrameWithNetSend();

				receiving = false;

				NetHelper.Report(true, "Received tile entity data for TEStorageUnit");
			}
			else if (serverItemsCount != items.Count) // if there is mismatch between the server and the client then send a sync request
			{
				NetHelper.Report(true, $"Item count mismatch detected for TEStorageUnit (Server: {serverItemsCount}, Client: {items.Count}), requesting full sync");

				NetHelper.SyncStorageUnit(Position);
			}
		}

		private void ClearItemsData()
		{
			items.Clear();
			hasSpaceInStack.Clear();
			hasItem.Clear();
			hasItemNoPrefix.Clear();
		}

		private void RepairMetadata()
		{
			hasSpaceInStack.Clear();
			hasItem.Clear();
			hasItemNoPrefix.Clear();
			foreach (Item item in items)
			{
				ItemData data = item;
				if (item.stack < item.maxStack)
					hasSpaceInStack.Add(data);
				hasItem.Add(data);
				hasItemNoPrefix.Add(data.Type);
			}
		}

		public void PostChangeContents()
		{
			RepairMetadata();
			UpdateTileFrameWithNetSend();
			NetHelper.SendTEUpdate(ID, Position);
		}
	}
}
