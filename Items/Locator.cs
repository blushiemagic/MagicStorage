using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Items
{
	public class Locator : ModItem
	{
		public Point16 location = Point16.NegativeOne;

		public override void SetStaticDefaults()
		{
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian), "Локатор");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish), "Lokalizator");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French), "Localisateur");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish), "Locador");
			DisplayName.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "定位器");

			Tooltip.SetDefault("<right> Storage Heart to store location" + "\n<right> Remote Storage Access to set it");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Russian),
				"<right> по Cердцу Хранилища чтобы запомнить его местоположение" +
				"\n<right> на Модуль Удаленного Доступа к Хранилищу чтобы привязать его к Сердцу Хранилища");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Polish),
				"<right> na serce jednostki magazynującej, aby zapisać jej lokalizację" + "\n<right> na bezprzewodowe okno dostępu aby je ustawić");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.French),
				"<right> le Cœur de Stockage pour enregistrer son emplacement" + "\n<right> le Stockage Éloigné pour le mettre en place");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Spanish),
				"<right> el Corazón de Almacenamiento para registrar su ubicación" +
				"\n<right> el Acceso de Almacenamiento Remoto para establecerlo" +
				"\n<right> Stockage Éloigné pour le mettre en place");
			Tooltip.AddTranslation(GameCulture.FromCultureName(GameCulture.CultureName.Chinese), "<right>存储核心可储存其定位点" + "\n<right>远程存储装置以设置其定位点");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 5;
		}

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(gold: 1);
		}

		public override void ModifyTooltips(List<TooltipLine> lines)
		{
			bool isSet = location.X >= 0 && location.Y >= 0;
			for (int k = 0; k < lines.Count; k++)
				if (isSet && lines[k].Mod == "Terraria" && lines[k].Name == "Tooltip0")
				{
					lines[k].Text = Language.GetTextValue("Mods.MagicStorage.SetTo", location.X, location.Y);
				}
				else if (!isSet && lines[k].Mod == "Terraria" && lines[k].Name == "Tooltip1")
				{
					lines.RemoveAt(k);
					k--;
				}
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.MeteoriteBar, 10);
			recipe.AddIngredient(ItemID.Amber, 2);
			recipe.AddTile(TileID.Anvils);
			recipe.Register();
		}

		public override void SaveData(TagCompound tag)
		{
			tag.Set("X", location.X);
			tag.Set("Y", location.Y);
		}

		public override void LoadData(TagCompound tag)
		{
			location = new Point16(tag.GetShort("X"), tag.GetShort("Y"));
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(location.X);
			writer.Write(location.Y);
		}

		public override void NetReceive(BinaryReader reader)
		{
			location = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
