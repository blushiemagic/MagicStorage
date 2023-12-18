using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public abstract class TEAbstractStorageUnit : TEStorageComponent
	{
		private Point16 center;

		public bool Inactive { get; set; }

		public abstract bool IsFull { get; }

		public override Point16 StorageCenter
		{
			get => center;
			set => center = value;
		}

		public abstract bool HasSpaceInStackFor(Item check);

		public abstract bool HasItem(Item check, bool ignorePrefix = false);

		public abstract IEnumerable<Item> GetItems();

		public abstract void DepositItem(Item toDeposit);

		public abstract Item TryWithdraw(Item lookFor, bool locked = false, bool keepOneIfFavorite = false);

		public override void SaveData(TagCompound tag)
		{
			tag.Set("Inactive", Inactive);
			TagCompound tagCenter = new();
			tagCenter.Set("X", center.X);
			tagCenter.Set("Y", center.Y);
			tag.Set("Center", tagCenter);
		}

		public override void LoadData(TagCompound tag)
		{
			Inactive = tag.GetBool("Inactive");
			TagCompound tagCenter = tag.GetCompound("Center");
			center = new Point16(tagCenter.GetShort("X"), tagCenter.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(Inactive);
			writer.Write(center.X);
			writer.Write(center.Y);
		}

		public override void NetReceive(BinaryReader reader)
		{
			Inactive = reader.ReadBoolean();
			center = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
