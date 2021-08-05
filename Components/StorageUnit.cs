using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
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

		public override ModTileEntity GetTileEntity() => mod.GetTileEntity("TEStorageUnit");

		public override void MouseOver(int i, int j)
		{
			Main.LocalPlayer.noThrow = 2;
		}

		public override int ItemType(int frameX, int frameY)
		{
			int style = frameY / 36;
			int type;
			switch (style)
			{
				case 1:
					type = ModContent.ItemType<StorageUnitDemonite>();
					break;
				case 2:
					type = ModContent.ItemType<StorageUnitCrimtane>();
					break;
				case 3:
					type = ModContent.ItemType<StorageUnitHellstone>();
					break;
				case 4:
					type = ModContent.ItemType<StorageUnitHallowed>();
					break;
				case 5:
					type = ModContent.ItemType<StorageUnitBlueChlorophyte>();
					break;
				case 6:
					type = ModContent.ItemType<StorageUnitLuminite>();
					break;
				case 7:
					type = ModContent.ItemType<StorageUnitTerra>();
					break;
				case 8:
					type = ModContent.ItemType<StorageUnitTiny>();
					break;
				default:
					type = ModContent.ItemType<Items.StorageUnit>();
					break;
			}

			return type;
		}

		public override bool CanKillTile(int i, int j, ref bool blockDamage) => Main.tile[i, j].frameX / 36 % 3 == 0;

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (Main.tile[i, j].frameX / 36 % 3 != 0)
				fail = true;
		}

		public override bool NewRightClick(int i, int j)
		{
			if (Main.tile[i, j].frameX % 36 == 18)
				i--;
			if (Main.tile[i, j].frameY % 36 == 18)
				j--;
			if (TryUpgrade(i, j))
				return true;
			var storageUnit = (TEStorageUnit) TileEntity.ByPosition[new Point16(i, j)];
			Main.LocalPlayer.tileInteractionHappened = true;
			string activeString = storageUnit.Inactive ? "Inactive" : "Active";
			string fullnessString = storageUnit.NumItems + " / " + storageUnit.Capacity + " Items";
			Main.NewText(activeString + ", " + fullnessString);
			return base.NewRightClick(i, j);
		}

		private bool TryUpgrade(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Item item = player.inventory[player.selectedItem];
			int style = Main.tile[i, j].frameY / 36;
			bool success = false;
			switch (style)
			{
				case 0 when item.type == ModContent.ItemType<UpgradeDemonite>():
					SetStyle(i, j, 1);
					success = true;
					break;
				case 0 when item.type == ModContent.ItemType<UpgradeCrimtane>():
					SetStyle(i, j, 2);
					success = true;
					break;
				case 1 when item.type == ModContent.ItemType<UpgradeHellstone>():
					SetStyle(i, j, 3);
					success = true;
					break;
				case 2 when item.type == ModContent.ItemType<UpgradeHellstone>():
					SetStyle(i, j, 3);
					success = true;
					break;
				case 3 when item.type == ModContent.ItemType<UpgradeHallowed>():
					SetStyle(i, j, 4);
					success = true;
					break;
				case 4 when item.type == ModContent.ItemType<UpgradeBlueChlorophyte>():
					SetStyle(i, j, 5);
					success = true;
					break;
				case 5 when item.type == ModContent.ItemType<UpgradeLuminite>():
					SetStyle(i, j, 6);
					success = true;
					break;
				case 6 when item.type == ModContent.ItemType<UpgradeTerra>():
					SetStyle(i, j, 7);
					success = true;
					break;
			}

			if (success)
			{
				var storageUnit = (TEStorageUnit) TileEntity.ByPosition[new Point16(i, j)];
				storageUnit.UpdateTileFrame();
				NetMessage.SendTileRange(Main.myPlayer, i, j, 2, 2);
				TEStorageHeart heart = storageUnit.GetHeart();
				if (heart != null)
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
			}

			return success;
		}

		private void SetStyle(int i, int j, int style)
		{
			Main.tile[i, j].frameY = (short) (36 * style);
			Main.tile[i + 1, j].frameY = (short) (36 * style);
			Main.tile[i, j + 1].frameY = (short) (36 * style + 18);
			Main.tile[i + 1, j + 1].frameY = (short) (36 * style + 18);
		}

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Tile tile = Main.tile[i, j];
			Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
			Vector2 drawPos = zero + 16f * new Vector2(i, j) - Main.screenPosition;
			var frame = new Rectangle(tile.frameX, tile.frameY, 16, 16);
			Color lightColor = Lighting.GetColor(i, j, Color.White);
			Color color = Color.Lerp(Color.White, lightColor, 0.5f);
			spriteBatch.Draw(mod.GetTexture("Components/StorageUnit_Glow"), drawPos, frame, color);
		}
	}
}
