using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components;

public partial class TEStorageUnit
{
	internal abstract class UnitOperation
	{
		public static readonly FullSyncOperation FullSync = new();
		public static readonly DepositOperation Deposit = new();
		public static readonly WithdrawOperation Withdraw = new();
		public static readonly WithdrawStackOperation WithdrawStack = new();

		private static readonly List<UnitOperation> types = new() { FullSync, Deposit, Withdraw, WithdrawStack };

		private static byte nextId;

		private readonly byte id = nextId++;
		protected Item data;

		public void Send(BinaryWriter writer, TEStorageUnit unit)
		{
			writer.Write(id);
			SendData(writer, unit);
		}

		protected abstract void SendData(BinaryWriter writer, TEStorageUnit unit);

		public static bool Receive(BinaryReader reader, TEStorageUnit unit)
		{
			byte id = reader.ReadByte();
			return id < types.Count && types[id].ReceiveData(reader, unit);
		}

		protected abstract bool ReceiveData(BinaryReader reader, TEStorageUnit unit);
	}

	internal abstract class UnitOperation<T> : UnitOperation
		where T : UnitOperation<T>
	{
		public T Create() => (T) MemberwiseClone();

		public T Create(Item item)
		{
			T clone = Create();
			clone.data = item;
			return clone;
		}
	}

	internal class FullSyncOperation : UnitOperation<FullSyncOperation>
	{
		protected override void SendData(BinaryWriter writer, TEStorageUnit unit)
		{
			writer.Write(unit.items.Count);
			foreach (Item item in unit.items)
				ItemIO.Send(item, writer, true, true);
		}

		protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit)
		{
			unit.ClearItemsData();
			int count = reader.ReadInt32();
			for (int k = 0; k < count; k++)
			{
				Item item = ItemIO.Receive(reader, true, true);
				unit.items.Add(item);
				ItemData data = new(item);
				if (item.stack < item.maxStack)
					unit.hasSpaceInStack.Add(data);
				unit.hasItem.Add(data);
				unit.hasItemNoPrefix.Add(data.Type);
			}

			return false;
		}
	}

	internal class DepositOperation : UnitOperation<DepositOperation>
	{
		protected override void SendData(BinaryWriter writer, TEStorageUnit unit)
		{
			ItemIO.Send(data, writer, true, true);
		}

		protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit)
		{
			unit.DepositItem(ItemIO.Receive(reader, true, true));
			return true;
		}
	}

	internal class WithdrawOperation : UnitOperation<WithdrawOperation>
	{
		public bool KeepOneIfFavorite { get; set; }

		protected override void SendData(BinaryWriter writer, TEStorageUnit unit)
		{
			writer.Write(KeepOneIfFavorite);
			ItemIO.Send(data, writer, true, true);
		}

		protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit)
		{
			bool keepOneIfFavorite = reader.ReadBoolean();
			unit.TryWithdraw(ItemIO.Receive(reader, true, true), keepOneIfFavorite: keepOneIfFavorite);
			return true;
		}
	}

	internal class WithdrawStackOperation : UnitOperation<WithdrawStackOperation>
	{
		protected override void SendData(BinaryWriter writer, TEStorageUnit unit)
		{
		}

		protected override bool ReceiveData(BinaryReader reader, TEStorageUnit unit)
		{
			unit.WithdrawStack();
			return true;
		}
	}
}
