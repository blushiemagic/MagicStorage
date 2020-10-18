using System.Collections;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Components
{
	public class TECreativeStorageUnit : TEAbstractStorageUnit
	{
		public override bool IsFull => true;

		public override bool ValidTile(Tile tile) {
			return tile.type == mod.TileType("CreativeStorageUnit") && tile.frameX == 0 && tile.frameY == 0;
		}

		public override bool HasSpaceInStackFor(Item check, bool locked = false) {
			return false;
		}

		public override bool HasItem(Item check, bool locked = false, bool ignorePrefix = false) {
			return !Inactive;
		}

		public override IEnumerable<Item> GetItems() {
			return new CreativeEnumerable(Inactive);
		}

		public override void DepositItem(Item toDeposit, bool locked = false) {
		}

		public override Item TryWithdraw(Item lookFor, bool locked = false, bool keepOneIfFavorite = false) {
			if (Inactive)
				return new Item();
			return lookFor.Clone();
		}
	}

	internal class CreativeEnumerable : IEnumerable<Item>
	{
		private readonly bool inactive;

		internal CreativeEnumerable(bool inactive) {
			this.inactive = inactive;
		}

		public IEnumerator<Item> GetEnumerator() {
			return new CreativeEnumerator(inactive);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	internal class CreativeEnumerator : IEnumerator<Item>
	{
		private int id;
		private readonly bool inactive;

		internal CreativeEnumerator(bool inactive) {
			this.inactive = inactive;
		}

		public Item Current {
			get {
				Item item = new Item();
				item.SetDefaults(id, true);
				item.stack = item.maxStack;
				return item;
			}
		}

		object IEnumerator.Current => Current;

		public bool MoveNext() {
			if (inactive)
				return false;
			do {
				id++;
			} while (id < ItemID.Sets.Deprecated.Length && ItemID.Sets.Deprecated[id]);
			return id < ItemID.Sets.Deprecated.Length;
		}

		public void Reset() {
			id = 0;
		}

		public void Dispose() {
		}
	}
}
