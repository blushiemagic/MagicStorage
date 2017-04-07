using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Components
{
	public class TECreativeStorageUnit : TEAbstractStorageUnit
	{
		public override bool IsFull
		{
			get
			{
				return true;
			}
		}

		public override bool ValidTile(Tile tile)
		{
			return tile.type == mod.TileType("CreativeStorageUnit") && tile.frameX == 0 && tile.frameY == 0;
		}

		public override bool HasSpaceInStackFor(Item check, bool locked = false)
		{
			return false;
		}

		public override bool HasItem(Item check, bool locked = false)
		{
			return !Inactive;
		}

		public override IEnumerable<Item> GetItems()
		{
			if (Inactive)
			{
				yield break;
			}
			for (int k = 1; k < ItemID.Sets.Deprecated.Length; k++)
			{
				if (!ItemID.Sets.Deprecated[k])
				{
					Item item = new Item();
					item.SetDefaults(k);
					item.stack = item.maxStack;
					yield return item;
				}
			}
		}

		public override void DepositItem(Item toDeposit, bool locked = false)
		{
		}

		public override Item TryWithdraw(Item lookFor, bool locked = false)
		{
			if (Inactive)
			{
				return new Item();
			}
			return lookFor.Clone();
		}
	}
}