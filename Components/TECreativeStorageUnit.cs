using System.Collections;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class TECreativeStorageUnit : TEAbstractStorageUnit
	{
		public override bool IsFull => true;

		public override bool ValidTile(Tile tile) => tile.TileType == ModContent.TileType<CreativeStorageUnit>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

		public override bool HasSpaceInStackFor(Item check) => false;

		public override bool HasItem(Item check, bool ignorePrefix = false) => !Inactive;

		public override IEnumerable<Item> GetItems() => new CreativeEnumerable(Inactive);

		public override void DepositItem(Item toDeposit)
		{
		}

		public override Item TryWithdraw(Item lookFor, bool locked = false, bool keepOneIfFavorite = false)
		{
			if (Inactive)
				return new Item();
			return lookFor.Clone();
		}
	}

	internal class CreativeEnumerable : IEnumerable<Item>
	{
		private readonly bool inactive;

		internal CreativeEnumerable(bool inactive)
		{
			this.inactive = inactive;
		}

		public IEnumerator<Item> GetEnumerator() => new CreativeEnumerator(inactive);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal class CreativeEnumerator : IEnumerator<Item>
	{
		private readonly bool inactive;
		private int id;

		internal CreativeEnumerator(bool inactive)
		{
			this.inactive = inactive;
		}

		public Item Current
		{
			get
			{
				Item item = new();
				item.SetDefaults(id, true);
				item.stack = item.maxStack;
				return item;
			}
		}

		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			if (inactive)
				return false;
			do
			{
				id++;
			} while (id < ItemID.Sets.Deprecated.Length && ItemID.Sets.Deprecated[id]);

			return id < ItemID.Sets.Deprecated.Length;
		}

		public void Reset()
		{
			id = 0;
		}

		public void Dispose()
		{
		}
	}
}
