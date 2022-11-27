using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public const int SAVE_VERSION = 1;

		[Obsolete("Use the Location property instead", true)]
		public Point16 location;
		[CloneByReference]
		internal Dictionary<string, Point16> locationsByWorld = new();

		public Point16 Location {
			get => locationsByWorld.TryGetValue(Main.worldName, out var pos) ? pos : Point16.NegativeOne;
			set => locationsByWorld[Main.worldName] = value;
		}

		public override void SetStaticDefaults()
		{
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
			Point16 location = Location;
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
			locationsByWorld ??= new();

			Point16 location = Location;

			//Legacy data
			tag["X"] = location.X;
			tag["Y"] = location.Y;

			tag["version"] = SAVE_VERSION;

			tag["locations"] = locationsByWorld
				.Select(kvp => new TagCompound() {
					["world"] = kvp.Key,
					["X"] = kvp.Value.X,
					["Y"] = kvp.Value.Y
				})
				.ToList();
		}

		public override void LoadData(TagCompound tag)
		{
			if (tag.GetInt("version") < SAVE_VERSION || tag.GetList<TagCompound>("locations") is not List<TagCompound> locations) {
				//Default to the last known location
				Location = new Point16(tag.GetShort("X"), tag.GetShort("Y"));
			} else {
				locationsByWorld = locations.ToDictionary(t => t.GetString("world"), t => new Point16(t.GetShort("X"), t.GetShort("Y")));
			}
		}

		public override void NetSend(BinaryWriter writer)
		{
			Point16 location = Location;
			writer.Write(location.X);
			writer.Write(location.Y);
		}

		public override void NetReceive(BinaryReader reader)
		{
			Location = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}
	}
}
