using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public class TERemoteAccess : TEStorageCenter
	{
		private bool _loaded;
		private Point16 locator = Point16.NegativeOne;

		internal bool Loaded
		{
			get => locator.X < 0 || locator.Y < 0 || _loaded;
			private set => _loaded = value;
		}

		public override Point16 StorageCenter
		{
			get => locator;
			set => locator = value;
		}

		public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<RemoteAccess>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

		public override TEStorageHeart GetHeart()
		{
			if (locator.X < 0 || locator.Y < 0)
				return null;

			if (ByPosition.TryGetValue(locator, out TileEntity te))
				return te as TEStorageHeart;

			LoadLocation();
			return null;
		}

		private void LoadLocation()
		{
			if (!Loaded)
			{
				Loaded = true;
				NetHelper.ClientRequestSection(locator);
			}
		}

		public bool TryLocate(Point16 toLocate, out string message)
		{
			if (locator.X >= 0 && locator.Y >= 0)
			{
				message = Language.GetTextValue("Mods.MagicStorage.RemoteAccessHasLocator");
				return false;
			}

			if (toLocate.X < 0 || toLocate.Y < 0)
			{
				message = Language.GetTextValue("Mods.MagicStorage.RemoteAccessUnlocated");
				return false;
			}

			message = Language.GetTextValue("Mods.MagicStorage.RemoteAccessSuccess");
			locator = toLocate;
			NetHelper.ClientSendTEUpdate(Position);
			return true;
		}

		public override void SaveData(TagCompound tag)
		{
			base.SaveData(tag);
			TagCompound tagLocator = new();
			tagLocator.Set("X", locator.X);
			tagLocator.Set("Y", locator.Y);
			tag.Set("Locator", tagLocator);
		}

		public override void LoadData(TagCompound tag)
		{
			base.LoadData(tag);
			TagCompound tagLocator = tag.GetCompound("Locator");
			locator = new Point16(tagLocator.GetShort("X"), tagLocator.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer)
		{
			base.NetSend(writer);
			writer.Write(locator.X);
			writer.Write(locator.Y);
		}

		public override void NetReceive(BinaryReader reader)
		{
			base.NetReceive(reader);
			locator = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
