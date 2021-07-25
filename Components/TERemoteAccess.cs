using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Components
{
	public class TERemoteAccess : TEStorageCenter
	{
		private bool _loaded;
		private Point16 locator = new Point16(-1, -1);

		internal bool Loaded
		{
			get => locator.X < 0 || locator.Y < 0 || _loaded;
			private set => _loaded = value;
		}

		public override bool ValidTile(Tile tile) => tile.type == ModContent.TileType<RemoteAccess>() && tile.frameX == 0 && tile.frameY == 0;

		public override TEStorageHeart GetHeart()
		{
			if (locator.X < 0 || locator.Y < 0)
				return null;
			if (!ByPosition.ContainsKey(locator))
			{
				Load();
				return null;
			}

			return ByPosition[locator] as TEStorageHeart;
		}

		private void Load()
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
				message = "This Access already has a locator, please mine then replace to reset it";
				return false;
			}

			if (toLocate.X < 0 || toLocate.Y < 0)
			{
				message = "The locator has not been set to a destination";
				return false;
			}

			message = "Success!";
			locator = toLocate;
			NetHelper.ClientSendTEUpdate(ID);
			return true;
		}

		public override void Update()
		{
			TEStorageHeart heart = GetHeart();
			if (heart != null && !heart.remoteAccesses.Contains(Position))
				heart.remoteAccesses.Add(Position);
		}

		public override TagCompound Save()
		{
			TagCompound tag = base.Save();
			var tagLocator = new TagCompound();
			tagLocator.Set("X", locator.X);
			tagLocator.Set("Y", locator.Y);
			tag.Set("Locator", tagLocator);
			return tag;
		}

		public override void Load(TagCompound tag)
		{
			base.Load(tag);
			TagCompound tagLocator = tag.GetCompound("Locator");
			locator = new Point16(tagLocator.GetShort("X"), tagLocator.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer, bool lightSend)
		{
			base.NetSend(writer, lightSend);
			writer.Write(locator.X);
			writer.Write(locator.Y);
		}

		public override void NetReceive(BinaryReader reader, bool lightReceive)
		{
			base.NetReceive(reader, lightReceive);
			locator = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
