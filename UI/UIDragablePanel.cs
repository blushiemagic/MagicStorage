using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI {
	public class UIDragablePanel : UIPanel {
		public const float cornerPadding = 12;

		// Stores the offset from the top left of the UIPanel while dragging.
		private Vector2 Offset { get; set; }

		public bool Dragging { get; set; }

		public readonly bool StopItemUse;

		public int UIDelay = -1;

		public event Action OnMenuClose, OnMenuReset;
		public UIPanel header;

		public readonly Dictionary<string, UIPanelTab> menus;

		public UIPanel viewArea;

		public event Action OnRecalculate;

		public UIDragablePanel(bool stopItemUse, IEnumerable<(string key, LocalizedText text)> menuOptions) {
			StopItemUse = stopItemUse;

			SetPadding(0);

			header = new UIPanel();
			header.SetPadding(0);
			header.Width.Set(0, 1f);
			header.Height.Set(30, 0f);
			header.BackgroundColor.A = 255;
			header.OnLeftMouseDown += Header_MouseDown;
			header.OnLeftMouseUp += Header_MouseUp;
			Append(header);

			var closeButton = new UITextPanel<char>('X');
			closeButton.SetPadding(7);
			closeButton.Width.Set(40, 0);
			closeButton.Left.Set(-40, 1);
			closeButton.BackgroundColor.A = 255;
			closeButton.OnLeftClick += (evt, element) => {
				SoundEngine.PlaySound(SoundID.MenuTick);
				OnMenuClose?.Invoke();
			};
			header.Append(closeButton);

			var resetButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Reset"));
			resetButton.SetPadding(7);
			resetButton.Width.Set(100, 0);
			resetButton.Left.Set(-45 - 100, 1);
			resetButton.BackgroundColor.A = 255;
			resetButton.OnLeftClick += (evt, element) => {
				SoundEngine.PlaySound(SoundID.MenuOpen);
				OnMenuReset?.Invoke();
			};
			resetButton.OnUpdate += element => {
				if (element.IsMouseHovering)
					MagicUI.mouseText = Language.GetTextValue("Mods.MagicStorage.HoverText.Reset");
			};
			resetButton.OnMouseOut += (evt, element) => {
				MagicUI.mouseText = "";
			};
			header.Append(resetButton);

			viewArea = new();
			viewArea.Top.Set(38, 0);
			viewArea.Width.Set(0, 1f);
			viewArea.Height.Set(-38, 1f);
			viewArea.BackgroundColor = Color.Transparent;
			viewArea.BorderColor = Color.Transparent;
			Append(viewArea);

			menus = new();

			float left = 0;

			foreach ((string key, LocalizedText text) in menuOptions) {
				UIPanelTab menu;
				menus.Add(key, menu = new(key, text));
				menu.SetPadding(7);
				menu.Left.Set(left, 0f);
				menu.BackgroundColor.A = 255;
				menu.Recalculate();

				left += menu.GetDimensions().Width + 10;

				header.Append(menu);
			}
		}

		public void SetActivePage(string page) {
			foreach (var tab in menus.Values)
				tab.TextColor = Color.White;

			if (menus.TryGetValue(page, out var menuText))
				menuText.TextColor = Color.Yellow;
		}

		public void HideTab(string page) {
			if (menus.TryGetValue(page, out var tab)) {
				tab.Remove();
				RecalculateTabPositions();
			}
		}

		public void ShowTab(string page) {
			if (menus.TryGetValue(page, out var tab) && tab.Parent is null) {
				tab.Remove();
				header.Append(tab);

				RecalculateTabPositions();
			}
		}

		private void RecalculateTabPositions() {
			float left = 0;

			foreach (var tab in menus.Values) {
				if (tab.Parent is null)
					continue;

				tab.Left.Set(left, 0f);
				tab.Recalculate();

				left += tab.GetDimensions().Width + 10;
			}
		}

		public override void Recalculate(){
			base.Recalculate();

			OnRecalculate?.Invoke();
		}

		private void Header_MouseDown(UIMouseEvent evt, UIElement element) {
			base.LeftMouseDown(evt);

			DragStart(evt);
		}

		private void Header_MouseUp(UIMouseEvent evt, UIElement element) {
			base.LeftMouseUp(evt);

			DragEnd(evt);
		}

		private void DragStart(UIMouseEvent evt) {
			Offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
			Dragging = true;
		}

		private void DragEnd(UIMouseEvent evt) {
			//A child element forced this to not move
			if(!Dragging)
				return;

			Vector2 end = evt.MousePosition;
			Dragging = false;

			Left.Set(end.X - Offset.X, 0f);
			Top.Set(end.Y - Offset.Y, 0f);

			Recalculate();
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime); // don't remove.

			if (!MagicStorageConfig.CanMoveUIPanels)
				Dragging = false;

			if (UIDelay > 0)
				UIDelay--;

			// clicks on this UIElement dont cause the player to use current items. 
			if (ContainsPoint(Main.MouseScreen) && StopItemUse)
				Main.LocalPlayer.mouseInterface = true;

			if (Dragging) {
				Left.Set(Main.mouseX - Offset.X, 0f); // Main.MouseScreen.X and Main.mouseX are the same.
				Top.Set(Main.mouseY - Offset.Y, 0f);
				Recalculate();
			}

			// Here we check if the UIDragablePanel is outside the Parent UIElement rectangle. 
			// By doing this and some simple math, we can snap the panel back on screen if the user resizes his window or otherwise changes resolution.
			var parentSpace = Parent.GetDimensions().ToRectangle();

			if (!GetDimensions().ToRectangle().Intersects(parentSpace)) {
				// TODO: account for negative Pixels and > 0 Percent
				Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
				Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);

				// Recalculate forces the UI system to do the positioning math again.
				Recalculate();
			}
		}
	}
}
