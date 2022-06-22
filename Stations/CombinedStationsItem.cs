﻿using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
	[Autoload(false)]
	public abstract class CombinedStationsItem<TTile> : ModItem where TTile : ModTile
	{
		public abstract string ItemName { get; }
		public abstract string ItemDescription { get; }

		public virtual int SacrificeCount => 1;

		public abstract int Rarity { get; }

		public sealed override void SetStaticDefaults()
		{
			DisplayName.SetDefault(ItemName);
			Tooltip.SetDefault(ItemDescription);

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = SacrificeCount;
		}

		public abstract void GetItemDimensions(out int width, out int height);

		public virtual void SafeSetDefaults()
		{
		}

		public sealed override void SetDefaults()
		{
			SafeSetDefaults();
			Item.DamageType = DamageClass.Default;
			Item.damage = 0;
			Item.knockBack = 0f;
			GetItemDimensions(out Item.width, out Item.height);
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.createTile = ModContent.TileType<TTile>();
			Item.consumable = true;
			Item.useTurn = true;
			Item.maxStack = 99;
			Item.rare = Rarity;
		}

		protected int BasePriceFromItems(params (int type, int stack)[] typeStacks){
			int price = 0;

			foreach ((int type, int stack) in typeStacks)
			{
				Item tItem = ContentSamples.ItemsByType[type];
				price += tItem.value * stack;
			}

			return price;
		}
	}
}
