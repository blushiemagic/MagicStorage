using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

#nullable enable

namespace MagicStorage.Components;

public class TECreativeStorageUnit : TEAbstractStorageUnit
{
	private static Item?[]? Items;

	public override bool IsFull => true;

	public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<CreativeStorageUnit>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

	public override bool HasSpaceInStackFor(Item check) => false;

	public override bool HasItem(Item check, bool ignorePrefix = false) => !Inactive;

	public override IEnumerable<Item> GetItems()
	{
		Items ??= new Item[ItemLoader.ItemCount];

		for (int i = 0; i < Items.Length; i++)
		{
			if (i is 0 || ItemID.Sets.Deprecated[i])
				continue;

			var item = Items[i];

			if (item is null)
				item = Items[i] = new Item(i);
			else if (item.type != i || item.IsAir)
				item.SetDefaults(i);

			item.stack = item.maxStack;
			yield return item;
		}
	}

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
