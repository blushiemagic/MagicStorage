using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MagicStorage.Components
{
	public class StorageUnit : StorageComponent
	{
		public override void ModifyObjectData()
		{
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleMultiplier = 6;
			TileObjectData.newTile.StyleWrapLimit = 6;
		}

		public override TEStorageUnit GetTileEntity() => ModContent.GetInstance<TEStorageUnit>();

		public override void MouseOver(int i, int j)
		{
			Main.LocalPlayer.noThrow = 2;
		}

		public override int ItemType(int frameX, int frameY)
		{
			int style = frameY / 36;
			int type = style switch {
				1 => ModContent.ItemType<StorageUnitDemonite>(),
				2 => ModContent.ItemType<StorageUnitCrimtane>(),
				3 => ModContent.ItemType<StorageUnitHellstone>(),
				4 => ModContent.ItemType<StorageUnitHallowed>(),
				5 => ModContent.ItemType<StorageUnitBlueChlorophyte>(),
				6 => ModContent.ItemType<StorageUnitLuminite>(),
				7 => ModContent.ItemType<StorageUnitTerra>(),
				8 => ModContent.ItemType<StorageUnitTiny>(),
				_ => ModContent.ItemType<Items.StorageUnit>()
			};
			return type;
		}

		public override bool CanKillTile(int i, int j, ref bool blockDamage)
		{
			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;

			if (!TileEntity.ByPosition.ContainsKey(new Point16(i, j)))
				return true;

			return Main.tile[i, j].TileFrameX / 36 % 3 == 0;
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;

			if (TileEntity.ByPosition.ContainsKey(new Point16(i, j)) && Main.tile[i, j].TileFrameX / 36 % 3 != 0)
				fail = true;
		}

		public override bool RightClick(int i, int j)
		{
			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;
			if (TryUpgrade(i, j))
				return true;

			if (!TileEntity.ByPosition.TryGetValue(new Point16(i, j), out var te) || te is not TEStorageUnit storageUnit)
				return false;

			Main.LocalPlayer.tileInteractionHappened = true;
			string activeString = storageUnit.Inactive ? Language.GetTextValue("Mods.MagicStorage.Inactive") : Language.GetTextValue("Mods.MagicStorage.Active");
			string fullnessString = Language.GetTextValue("Mods.MagicStorage.Capacity", storageUnit.NumItems, storageUnit.Capacity);
			Main.NewText(activeString + ", " + fullnessString);
			return base.RightClick(i, j);
		}

		private static bool TryUpgrade(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Item item = player.HeldItem;
			int style = Main.tile[i, j].TileFrameY / 36;
			bool success = false;
			if (style == 0 && item.type == ModContent.ItemType<UpgradeDemonite>())
			{
				SetStyle(i, j, 1);
				success = true;
			}
			else if (style == 0 && item.type == ModContent.ItemType<UpgradeCrimtane>())
			{
				SetStyle(i, j, 2);
				success = true;
			}
			else if ((style == 1 || style == 2) && item.type == ModContent.ItemType<UpgradeHellstone>())
			{
				SetStyle(i, j, 3);
				success = true;
			}
			else if (style == 3 && item.type == ModContent.ItemType<UpgradeHallowed>())
			{
				SetStyle(i, j, 4);
				success = true;
			}
			else if (style == 4 && item.type == ModContent.ItemType<UpgradeBlueChlorophyte>())
			{
				SetStyle(i, j, 5);
				success = true;
			}
			else if (style == 5 && item.type == ModContent.ItemType<UpgradeLuminite>())
			{
				SetStyle(i, j, 6);
				success = true;
			}
			else if (style == 6 && item.type == ModContent.ItemType<UpgradeTerra>())
			{
				SetStyle(i, j, 7);
				success = true;
			}

			if (success)
			{
				if (!TileEntity.ByPosition.TryGetValue(new Point16(i, j), out var te) || te is not TEStorageUnit storageUnit)
					return false;

				storageUnit.UpdateTileFrame();
				NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 2);
				TEStorageHeart heart = storageUnit.GetHeart();
				if (heart is not null)
				{
					if (Main.netMode == NetmodeID.SinglePlayer)
						heart.ResetCompactStage();
					else if (Main.netMode == NetmodeID.MultiplayerClient)
						NetHelper.SendResetCompactStage(heart.ID);
				}

				item.stack--;
				if (item.stack <= 0)
					item.SetDefaults();
				if (player.selectedItem == 58)
					Main.mouseItem = item.Clone();

				return true;
			}

			return false;
		}

		private static void SetStyle(int i, int j, int style)
		{
			Main.tile[i, j].TileFrameY = (short) (36 * style);
			Main.tile[i + 1, j].TileFrameY = (short) (36 * style);
			Main.tile[i, j + 1].TileFrameY = (short) (36 * style + 18);
			Main.tile[i + 1, j + 1].TileFrameY = (short) (36 * style + 18);
		}

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Tile tile = Main.tile[i, j];
			Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
			Vector2 drawPos = zero + 16f * new Vector2(i, j) - Main.screenPosition;
			Rectangle frame = new(tile.TileFrameX, tile.TileFrameY, 16, 16);
			Color lightColor = Lighting.GetColor(i, j, Color.White);
			Color color = Color.Lerp(Color.White, lightColor, 0.5f);
			spriteBatch.Draw(Mod.Assets.Request<Texture2D>("Components/StorageUnit_Glow").Value, drawPos, frame, color);
		}
	}
}
