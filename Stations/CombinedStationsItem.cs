using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Stations
{
    [Autoload(false)]
    public abstract class CombinedStationsItem<TTile> : ModItem where TTile : ModTile
    {
        public abstract string ItemName { get; }
        public abstract string ItemDescription { get; }

        public virtual Dictionary<GameCulture, string> ItemNameLocalized { get; }
        public virtual Dictionary<GameCulture, string> ItemDescriptionLocalized { get; }

        public virtual int SacrificeCount => 1;

        public abstract int Rarity { get; }

        public sealed override void SetStaticDefaults()
        {
            DisplayName.SetDefault(ItemName);
            Tooltip.SetDefault(ItemDescription);

            foreach (var name in ItemNameLocalized)
            {
                DisplayName.AddTranslation(name.Key, name.Value);
            }
            foreach (var description in ItemDescriptionLocalized)
            {
                Tooltip.AddTranslation(description.Key, description.Value);
            }

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

        protected int BasePriceFromItems(params (int type, int stack)[] typeStacks)
        {
            int price = 0;

            for (int i = 0; i < typeStacks.Length; i++)
            {
                Item tItem = new Item(typeStacks[i].type);
                price += tItem.value * typeStacks[i].stack;
            }

            return price;
        }
    }
}
