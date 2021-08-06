using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace MagicStorage.UI
{
	public class UISlotZone : UIElement
	{
		public delegate Item GetItem(int slot, ref int context);

		public delegate void HoverItemSlot(int slot, ref int hoverSlot);

		private const int padding = 4;

		private readonly GetItem getItem;
		private readonly float inventoryScale;
		private readonly HoverItemSlot onHover;
		private int hoverSlot = -1;
		private int numColumns = 10;
		private int numRows = 4;

		public UISlotZone(HoverItemSlot onHover, GetItem getItem, float scale)
		{
			this.onHover = onHover;
			this.getItem = getItem;
			inventoryScale = scale;
		}

		public void SetDimensions(int columns, int rows)
		{
			numColumns = columns;
			numRows = rows;
		}

		private static readonly Asset<Texture2D> inventoryBack = TextureAssets.InventoryBack;

		public override void Update(GameTime gameTime)
		{
			hoverSlot = -1;
			Vector2 origin = InterfaceHelper.GetFullRectangle(this).TopLeft();
			MouseState curMouse = StorageGUI.curMouse;
			if (curMouse.X <= origin.X || curMouse.Y <= origin.Y)
				return;
			Texture2D texture = inventoryBack.Value;
			int slotWidth = (int) (texture.Width * inventoryScale * Main.UIScale);
			int slotHeight = (int) (texture.Height * inventoryScale * Main.UIScale);
			int slotX = (curMouse.X - (int) origin.X) / (slotWidth + padding);
			int slotY = (curMouse.Y - (int) origin.Y) / (slotHeight + padding);
			if (slotX < 0 || slotX >= numColumns || slotY < 0 || slotY >= numRows)
				return;
			Vector2 slotPos = origin + new Vector2(slotX * (slotWidth + padding * Main.UIScale), slotY * (slotHeight + padding * Main.UIScale));
			if (curMouse.X > slotPos.X && curMouse.X < slotPos.X + slotWidth && curMouse.Y > slotPos.Y && curMouse.Y < slotPos.Y + slotHeight)
				onHover(slotX + numColumns * slotY, ref hoverSlot);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Texture2D texture = inventoryBack.Value;
			float slotWidth = texture.Width * inventoryScale;
			float slotHeight = texture.Height * inventoryScale;
			Vector2 origin = GetDimensions().Position();
			float oldScale = Main.inventoryScale;
			Main.inventoryScale = inventoryScale;
			Item[] temp = new Item[11];
			for (int k = 0; k < numColumns * numRows; k++)
			{
				int context = 0;
				Item item = getItem(k, ref context);
				Vector2 drawPos = origin + new Vector2((slotWidth + padding) * (k % numColumns), (slotHeight + padding) * (k / numColumns));
				temp[10] = item;
				ItemSlot.Draw(Main.spriteBatch, temp, context, 10, drawPos);
			}

			Main.inventoryScale = oldScale;
		}

		public void DrawText()
		{
			if (hoverSlot >= 0)
			{
				int context = 0;
				Item hoverItem = getItem(hoverSlot, ref context);
				if (!hoverItem.IsAir)
				{
					Main.HoverItem = hoverItem.Clone();
					Main.instance.MouseText(string.Empty);
				}
			}
		}
	}
}
