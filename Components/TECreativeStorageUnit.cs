using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

#nullable enable

namespace MagicStorage.Components;

public class TECreativeStorageUnit : TEAbstractStorageUnit
{
	private static Item AirItem = null!;
	private static Item[] Items = null!;

	public override void Load(Mod mod)
	{
		base.Load(mod);

		AirItem = new();
		Items = new Item[ItemLoader.ItemCount];
		for (int i = 0; i < Items.Length; i++)
		{
			var item = Items[i] = new Item(i);
			item.stack = item.maxStack;
		}
	}

	public override void Unload()
	{
		base.Unload();

		AirItem = null!;
		Items = null!;
	}

	public override bool IsFull => true;

	public override bool ValidTile(in Tile tile) => tile.TileType == ModContent.TileType<CreativeStorageUnit>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;

	public override bool HasSpaceInStackFor(Item check) => false;

	public override bool HasItem(Item check, bool ignorePrefix = false) => !Inactive;

	public override IEnumerable<Item> GetItems()
	{
		for (int i = 0; i < Items.Length; i++)
		{
			var item = Items[i];
			if (item.IsAir)
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
			return AirItem;

		return lookFor.Clone();
	}
}